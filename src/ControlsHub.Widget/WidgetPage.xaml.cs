using System;
using System.Collections.ObjectModel;
using Microsoft.Gaming.XboxGameBar;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ControlsHub.Widget
{
    /// <summary>
    /// P0 page: live preset from the broker (pushed, never polled), the last nav events, and a
    /// highlight that the DualSense nav stream moves while the widget does not have focus.
    /// </summary>
    public sealed partial class WidgetPage : Page
    {
        private static readonly string[] Presets = { "battery", "quiet", "balanced", "performance", "custom" };
        private static readonly string[] Tabs = { "ALIEN", "DOLBY", "DUALSENSE" };

        private readonly ObservableCollection<string> _nav = new ObservableCollection<string>();
        private XboxGameBarWidget _widget;
        private int _selected = 2;
        private int _tab;
        private int _changes;

        public WidgetPage()
        {
            InitializeComponent();
            NavList.ItemsSource = _nav;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _widget = e.Parameter as XboxGameBarWidget;
            BrokerConnection.Message += OnBrokerMessage;
            BrokerConnection.ConnectedChanged += OnBrokerConnectedChanged;
            ShowConnection(BrokerConnection.IsConnected);
            object preset;
            lock (BrokerConnection.LastState)
            {
                if (BrokerConnection.LastState.TryGetValue("preset", out preset)) ShowPreset(preset as string);
            }
            UpdateHighlight();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            BrokerConnection.Message -= OnBrokerMessage;
            BrokerConnection.ConnectedChanged -= OnBrokerConnectedChanged;
        }

        private void OnBrokerConnectedChanged(bool connected)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () => ShowConnection(connected));
        }

        private void OnBrokerMessage(ValueSet msg)
        {
            var _ = Dispatcher.RunAsync(CoreDispatcherPriority.High, () => Handle(msg));
        }

        private void Handle(ValueSet msg)
        {
            var t = msg.TryGetValue("t", out var tv) ? tv as string : null;
            switch (t)
            {
                case "state":
                    if (msg.TryGetValue("preset", out var p))
                    {
                        _changes++;
                        ShowPreset(p as string);
                    }
                    break;
                case "pipe":
                    ShowConnection(BrokerConnection.IsConnected);
                    if (!BrokerConnection.PipeConnected) ShowPreset(null);
                    break;
                case "nav":
                    var btn = msg.TryGetValue("btn", out var b) ? b as string : "?";
                    var down = !msg.TryGetValue("down", out var d) || (d is bool db && db);
                    if (down) OnNav(btn);
                    break;
                case "ack":
                    var ok = msg.TryGetValue("ok", out var o) && o is bool ob && ob;
                    var id = msg.TryGetValue("id", out var i) ? i : "?";
                    AckText.Text = ok ? $"set #{id} applied by the app" : $"set #{id} refused";
                    break;
            }
        }

        private void OnNav(string btn)
        {
            _nav.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {btn.ToUpperInvariant()}");
            while (_nav.Count > 10) _nav.RemoveAt(_nav.Count - 1);
            switch (btn)
            {
                case "left":
                case "up":
                    _selected = (_selected + Presets.Length - 1) % Presets.Length;
                    UpdateHighlight();
                    break;
                case "right":
                case "down":
                    _selected = (_selected + 1) % Presets.Length;
                    UpdateHighlight();
                    break;
                case "cross":
                    SendPreset(Presets[_selected]);
                    break;
                case "circle":
                    _nav.Clear();
                    break;
                case "l1":
                    _tab = (_tab + Tabs.Length - 1) % Tabs.Length;
                    TabText.Text = "TAB " + Tabs[_tab];
                    break;
                case "r1":
                    _tab = (_tab + 1) % Tabs.Length;
                    TabText.Text = "TAB " + Tabs[_tab];
                    break;
            }
        }

        private void Chip_Click(object sender, RoutedEventArgs e)
        {
            var preset = (sender as Button)?.Tag as string;
            if (preset == null) return;
            _selected = Array.IndexOf(Presets, preset);
            UpdateHighlight();
            SendPreset(preset);
        }

        private void SendPreset(string preset)
        {
            BrokerConnection.Send(new ValueSet { ["t"] = "set", ["key"] = "preset", ["value"] = preset });
        }

        private void ShowConnection(bool brokerConnected)
        {
            BrokerDot.Fill = brokerConnected ? Brush("LegendGreen") : Brush("LegendText3");
            BrokerText.Text = brokerConnected ? "BROKER" : "NO BROKER";
            if (!brokerConnected)
                LinkText.Text = "broker: not connected (it starts when the widget opens)";
            else if (BrokerConnection.PipeConnected)
                LinkText.Text = $"broker: connected    app: {BrokerConnection.PipeApp} connected";
            else
                LinkText.Text = "broker: connected    app: not running";
        }

        private void ShowPreset(string preset)
        {
            if (string.IsNullOrEmpty(preset))
            {
                PresetText.Text = "NOT RUNNING";
                PresetText.Foreground = Brush("LegendText3");
                PresetMeta.Text = "start tools\\DummyApp";
                return;
            }
            PresetText.Text = preset.ToUpperInvariant();
            PresetText.Foreground = new SolidColorBrush(PresetColor(preset));
            PresetMeta.Text = $"pushed {DateTime.Now:HH:mm:ss.fff}    {_changes} changes this session";
        }

        private void UpdateHighlight()
        {
            for (int i = 0; i < Chips.Children.Count; i++)
            {
                var chip = (Button)Chips.Children[i];
                chip.BorderBrush = i == _selected ? new SolidColorBrush(Colors.White) : Brush("LegendBorderSoft");
            }
        }

        private static Color PresetColor(string preset)
        {
            switch (preset)
            {
                case "battery": return Color.FromArgb(255, 0xFF, 0xB0, 0x20);
                case "quiet": return Color.FromArgb(255, 0x40, 0xDC, 0x78);
                case "balanced": return Color.FromArgb(255, 0x00, 0xE5, 0xFF);
                case "performance": return Color.FromArgb(255, 0xFF, 0x4D, 0x5E);
                case "custom": return Color.FromArgb(255, 0x9B, 0x7B, 0xFF);
                default: return Color.FromArgb(255, 0xA9, 0xB4, 0xBF);
            }
        }

        private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];
    }
}
