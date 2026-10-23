from legend import *

LA = "/workspace/projects/96da831d-cbeb-4679-8a00-90a88452ffe5/repo/crates/dsz/assets/legend"
W, H = 1100, 600
AMBER = "#FFB020"; GREEN = "#40DC78"; RED = "#FF4D5E"; VIOLET = "#9B7BFF"
d = Doc(W, H)
d.rect(0, 0, W, H, "#0B0C0F")
d.hex_pattern(0, 0, W, 90)
d.rect(40, 0, W - 80, 2, d.grad(ACCENT, PURPLE), r=1)
d.caps(48, 50, "GLANCE STRIP", 18, TEXT, 4)
d.text(48, 76, "Sits under the tabs on every tab. Each part is tied to one app. Press it to jump to that tab.", 14, MUTED)

SW, SH = 620, 46


def strip(x, y, parts, active=None):
    d.rect(x, y, SW, SH, "#0B0D10", LINE, r=7)
    pw = SW / 3
    for i, p in enumerate(parts):
        px = x + i * pw
        if i:
            d.line(px, y + 10, px, y + SH - 10, LINE)
        if active == i:
            d.rect(px + 4, y + 4, pw - 8, SH - 8, SELBG, ACCENT, r=5)
        kind, ic, label, col, extra = p
        dim = kind == "off"
        c = "#3E4650" if dim else col
        if ic.endswith(".png"):
            d.tinted_logo(ic, px + 14, y + 14, 18, 18, c)
        else:
            d.icon(ic, px + 23, y + 23, 18, c, 1.8)
        d.mono(px + 42, y + 28, label, 12, FAINT if dim else TEXT, 1.4, "start", 600)
        if extra == "bar":
            d.ticks(px + 150, y + 21, 40, 0.8 if col == ACCENT else 0.15, col, n=5, h=4)
        elif extra == "bolt":
            d.icon("bolt", px + pw - 18, y + 23, 14, ACCENT, 1.8)


def note(x, y, txt, col=MUTED):
    d.text(x, y, txt, 13, col)


rows = [
    ("Normal",
     [("on", "scale", "BALANCED", ACCENT, None), ("on", "wave-sine", "ATMOS  GAME", ACCENT, None),
      ("on", f"{LA}/ps-logo.png", "DS  80%", ACCENT, "bar")], 0,
     "Preset in its own color, Atmos profile, controller battery"),
    ("Changes live",
     [("on", "gauge", "PERFORM.", RED, None), ("on", "wave-sine", "ATMOS  MOVIE", ACCENT, None),
      ("on", f"{LA}/ps-logo.png", "DS  ~64%", ACCENT, "bolt")], None,
     "F2 or a game profile flips it red; charging shows a bolt"),
    ("Warnings",
     [("on", "flame", "G-MODE", ORANGE, None), ("off", "wave-sine", "ATMOS  OFF", ACCENT, None),
      ("on", f"{LA}/ps-logo.png", "DS  12%", ORANGE, "bar")], None,
     "G-Mode on, Atmos off goes dim, low battery turns orange"),
    ("App closed",
     [("on", "moon", "QUIET", GREEN, None), ("on", "wave-sine", "ATMOS  GAME", ACCENT, None),
      ("off", f"{LA}/ps-logo.png", "NOT RUNNING", ACCENT, None)], None,
     "A closed app goes grey; its tab shows a Start button"),
]
y = 120
for title, parts, act, txt in rows:
    d.mono(48, y + 28, title.upper(), 11, FAINT, 2)
    strip(200, y, parts, act)
    note(200, y + SH + 22, txt)
    y += 120

# side callout: focus behaviour
x0 = 850
d.card(x0, 120, 210, 210)
d.caps(x0 + 16, 148, "CONTROLLER", 11, TEXT, 2.4)
for i, (k, c, t) in enumerate([("D", "#3E4650", ["Up from the content", "to the strip"]),
                               ("A", "#4F6BD6", ["Jump to that tab"]),
                               ("X", "#2E86DE", ["Quick toggle: next", "preset, Atmos on/off"])]):
    yy = 180 + i * 46
    d.circle(x0 + 28, yy, 11, c)
    d.text(x0 + 28, yy + 4.5, k if k != "D" else "\u2191", 12, "#FFFFFF", "Inter", 600, "middle")
    for j, line in enumerate(t):
        d.text(x0 + 48, yy + 4 + j * 16, line.strip(), 12.5, MUTED)
d.card(x0, 350, 210, 120)
d.caps(x0 + 16, 378, "COST", 11, TEXT, 2.4)
for j, line in enumerate(["Updates only when an app", "pushes a change.", "Nothing polls while", "Game Bar is closed."]):
    d.text(x0 + 16, 404 + j * 17, line, 12.5, MUTED)
d.save_png("/workspace/work/gamebar/glance-strip.png", scale=1.5)
print("ok")
