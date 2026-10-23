using ControlsHub.Protocol;
using Xunit;

namespace ControlsHub.Protocol.Tests;

public class RoundTripTests
{
    private static T RoundTrip<T>(T msg) where T : HubMessage
    {
        var line = HubProtocol.Serialize(msg);
        Assert.DoesNotContain('\n', line);
        return Assert.IsType<T>(HubProtocol.Parse(line));
    }

    [Fact]
    public void Hello_round_trips()
    {
        var h = RoundTrip(new HelloMessage { App = "alien", AppVersion = "1.3.0" });
        Assert.Equal("alien", h.App);
        Assert.Equal("1.3.0", h.AppVersion);
        Assert.Equal(1, h.V);
    }

    [Fact]
    public void Hello_matches_documented_wire_format()
    {
        Assert.Equal("{\"t\":\"hello\",\"client\":\"ControlsHub\",\"v\":1}",
            HubProtocol.Serialize(new HelloMessage { Client = "ControlsHub" }));
    }

    [Fact]
    public void State_round_trips_all_value_kinds()
    {
        var s = new StateMessage();
        s.Values["preset"] = "performance";
        s.Values["gmode"] = true;
        s.Values["key_light"] = 40L;
        s.Values["cpu_temp"] = 71.5;
        s.Values["outputs"] = new JsonRaw("[{\"id\":\"a\",\"name\":\"Speakers\"}]");
        s.Values["output"] = null;
        var back = RoundTrip(s);
        Assert.Equal("performance", back.Values["preset"]);
        Assert.Equal(true, back.Values["gmode"]);
        Assert.Equal(40L, back.Values["key_light"]);
        Assert.Equal(71.5, back.Values["cpu_temp"]);
        Assert.Equal(new JsonRaw("[{\"id\":\"a\",\"name\":\"Speakers\"}]"), back.Values["outputs"]);
        Assert.Null(back.Values["output"]);
    }

    [Fact]
    public void Set_and_ack_round_trip()
    {
        var set = RoundTrip(new SetMessage { Key = "preset", Value = "quiet", Quiet = true, Id = 7 });
        Assert.Equal("preset", set.Key);
        Assert.Equal("quiet", set.Value);
        Assert.True(set.Quiet);
        Assert.Equal(7, set.Id);

        var ack = RoundTrip(new AckMessage { Id = 7, Ok = false, Error = "nope" });
        Assert.Equal(7, ack.Id);
        Assert.False(ack.Ok);
        Assert.Equal("nope", ack.Error);
    }

    [Fact]
    public void Nav_event_and_control_round_trip()
    {
        var n = RoundTrip(new NavEventMessage { Btn = "cross", Down = false });
        Assert.Equal("cross", n.Btn);
        Assert.False(n.Down);
        Assert.True(RoundTrip(new NavControlMessage { On = true }).On);
    }

    [Fact]
    public void Simple_messages_round_trip()
    {
        RoundTrip(new SubMessage());
        RoundTrip(new OpenMessage());
        Assert.True(RoundTrip(new WatchMessage { Sensors = true }).Sensors);
    }
}

public class RejectionTests
{
    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("{\"x\":1}")]
    [InlineData("{\"t\":\"explode\"}")]
    [InlineData("{\"t\":\"set\",\"value\":1}")]
    [InlineData("{\"t\":\"nav\",\"btn\":\"start\"}")]
    [InlineData("{\"t\":\"nav\"}")]
    public void Bad_lines_throw(string line)
    {
        Assert.Throws<ProtocolException>(() => HubProtocol.Parse(line));
    }

    [Fact]
    public void Overlong_line_is_rejected()
    {
        var line = "{\"t\":\"state\",\"x\":\"" + new string('a', HubProtocol.MaxLineLength) + "\"}";
        Assert.Throws<ProtocolException>(() => HubProtocol.Parse(line));
    }

    [Fact]
    public void Unknown_key_is_rejected_on_set()
    {
        var set = (SetMessage)HubProtocol.Parse("{\"t\":\"set\",\"key\":\"fan_curve\",\"value\":3,\"id\":1}");
        Assert.False(KeySchema.TryValidateSet("alien", set, out var error));
        Assert.Contains("unknown key", error);
    }

    [Fact]
    public void Read_only_key_is_rejected_on_set()
    {
        var set = new SetMessage { Key = "cpu_temp", Value = 50.0 };
        Assert.False(KeySchema.TryValidateSet("alien", set, out var error));
        Assert.Contains("read only", error);
    }

    [Fact]
    public void Unknown_keys_are_dropped_from_state()
    {
        var s = (StateMessage)HubProtocol.Parse("{\"t\":\"state\",\"preset\":\"quiet\",\"evil\":1}");
        var rejected = new List<string>();
        var filtered = KeySchema.Filter("dummy", s, rejected);
        Assert.Single(filtered.Values);
        Assert.Equal(new[] { "evil" }, rejected);
    }

    [Fact]
    public void Bad_enum_value_is_rejected()
    {
        Assert.False(KeySchema.TryNormalize("alien", "preset", "turbo", out _, out _));
        Assert.False(KeySchema.TryNormalize("alien", "preset", 3L, out _, out _));
    }

    [Fact]
    public void Unknown_app_rejects_everything()
    {
        Assert.False(KeySchema.TryNormalize("toaster", "preset", "quiet", out _, out _));
    }
}

public class ClampTests
{
    [Theory]
    [InlineData(150L, 100L)]
    [InlineData(-5L, 0L)]
    [InlineData(40L, 40L)]
    [InlineData(55.5, 56L)]
    public void Key_light_is_clamped_and_rounded(object input, long expected)
    {
        Assert.True(KeySchema.TryNormalize("alien", "key_light", input, out var v, out _));
        Assert.Equal(expected, v);
    }

    [Fact]
    public void Trigger_cap_is_clamped()
    {
        var set = new SetMessage { Key = "trigger_cap", Value = 900L };
        Assert.True(KeySchema.TryValidateSet("dualsense", set, out _));
        Assert.Equal(100L, set.Value);
    }

    [Fact]
    public void NaN_is_rejected()
    {
        Assert.False(KeySchema.TryNormalize("alien", "key_light", double.NaN, out _, out _));
    }

    [Fact]
    public void Number_key_keeps_fraction_inside_range()
    {
        Assert.True(KeySchema.TryNormalize("alien", "cpu_temp", 200.0, out var v, out _));
        Assert.Equal(150.0, v);
    }
}
