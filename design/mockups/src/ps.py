from legend import *
import math

LA = "/workspace/projects/96da831d-cbeb-4679-8a00-90a88452ffe5/repo/crates/dsz/assets/legend"
PSC = {"tri": "#3FD3B4", "cir": "#F0506E", "crs": "#7FA6F0", "sq": "#E08AD0"}
RED = "#FF4D5E"


def face(d, cx, cy, kind, r=13, style="legend", sw=1.8):
    """PlayStation face button: round cap, shape inside."""
    if style == "legend":
        d.circle(cx, cy, r, "#0E1013", ACCENT, sw=1.4)
        c = TEXT
    else:
        d.circle(cx, cy, r, "#15181C", "#2A3038", sw=1.2)
        c = PSC[kind]
    s = r * 0.46
    if kind == "crs":
        d.add(f'<path d="M{cx-s} {cy-s}L{cx+s} {cy+s}M{cx+s} {cy-s}L{cx-s} {cy+s}" stroke="{c}" stroke-width="{sw}" stroke-linecap="round"/>')
    elif kind == "cir":
        d.circle(cx, cy, s * 1.05, "none", c, sw=sw)
    elif kind == "sq":
        d.rect(cx - s * 0.92, cy - s * 0.92, s * 1.84, s * 1.84, "none", c, r=1, sw=sw)
    elif kind == "tri":
        h = s * 1.9
        d.add(f'<path d="M{cx} {cy-h*0.58}L{cx+s*1.1} {cy+h*0.42}L{cx-s*1.1} {cy+h*0.42}Z" fill="none" stroke="{c}" stroke-width="{sw}" stroke-linejoin="round"/>')


def shoulder(d, cx, cy, t, style="legend", w=34, h=22):
    st = ACCENT if style == "legend" else "#2A3038"
    d.rect(cx - w / 2, cy - h / 2, w, h, "#0E1013" if style == "legend" else "#15181C", st, r=h / 2 if t[1] == "1" else 6, sw=1.4 if style == "legend" else 1.2)
    d.mono(cx, cy + 4, t, 10.5, TEXT, 0.8, "middle", 600)


def dpad(d, cx, cy, style="legend", s=12):
    st = ACCENT if style == "legend" else "#2A3038"
    p = (f"M{cx-s*0.33} {cy-s}h{s*0.66}v{s*0.67}h{s*0.67}v{s*0.66}h-{s*0.67}v{s*0.67}h-{s*0.66}v-{s*0.67}"
         f"h-{s*0.67}v-{s*0.66}h{s*0.67}Z")
    d.add(f'<path d="{p}" fill="#0E1013" stroke="{st}" stroke-width="1.3" stroke-linejoin="round"/>')
    d.add(f'<path d="M{cx-3} {cy-s*0.55}L{cx} {cy-s*0.85}L{cx+3} {cy-s*0.55}Z" fill="{TEXT}"/>')


def pill(d, cx, cy, s, style="legend"):
    st = ACCENT if style == "legend" else "#2A3038"
    d.rect(cx - 8, cy - 12, 16, 24, "#0E1013", st, r=8, sw=1.3)
    for i in range(3):
        d.line(cx - 3.5, cy - 5 + i * 5, cx + 3.5, cy - 5 + i * 5, TEXT, 1.4, extra='stroke-linecap="round"')


def hint(d, x, y, kind, label, style="legend"):
    face(d, x, y, kind, 11, style, 1.6)
    d.text(x + 18, y + 5, label, 13, TEXT)


# ---------- image A: prompt sheet ----------
W, H = 1180, 700
d = Doc(W, H)
d.rect(0, 0, W, H, "#0B0C0F")
d.hex_pattern(0, 0, W, 90)
d.rect(40, 0, W - 80, 2, d.grad(ACCENT, PURPLE), r=1)
d.caps(48, 50, "PLAYSTATION PROMPTS", 18, TEXT, 4)
d.text(48, 76, "Drawn in the app theme, never Sony's art. Cross confirms, Circle goes back.", 14, MUTED)

for col, (style, title, sub) in enumerate([("legend", "LEGEND", "cyan ring, white shape: matches the app"),
                                            ("ps", "PLAYSTATION COLORS", "dark cap, shapes in their own colors")]):
    x0 = 48 + col * 560
    d.card(x0, 104, 530, 250)
    d.caps(x0 + 20, 134, title, 12, TEXT, 2.6)
    d.text(x0 + 20, 156, sub, 12.5, FAINT)
    names = [("crs", "Select"), ("cir", "Back"), ("sq", "Quick toggle"), ("tri", "Save for game")]
    for i, (k, n) in enumerate(names):
        cx = x0 + 70 + i * 124
        face(d, cx, 210, k, 22, style, 2.4)
        d.text(cx, 254, n, 12, MUTED, B, 400, "middle")
    others = [("L1", "Prev tab"), ("R1", "Next tab"), ("DPAD", "Move"), ("OPT", "Open app"), ("PS", "Open overlay")]
    for i, (k, n) in enumerate(others):
        cx = x0 + 60 + i * 102
        if k in ("L1", "R1"):
            shoulder(d, cx, 300, k, style)
        elif k == "DPAD":
            dpad(d, cx, 300, style)
        elif k == "OPT":
            pill(d, cx, 300, 1, style)
        else:
            d.circle(cx, 300, 14, "#0E1013", ACCENT if style == "legend" else "#2A3038", sw=1.4)
            d.tinted_logo(f"{LA}/ps-logo.png", cx - 8, 292, 16, 16, TEXT)
        d.text(cx, 334, n, 12, MUTED, B, 400, "middle")

# in context: widget top and footer, Legend style
y0 = 390
d.caps(48, y0, "IN THE WIDGET", 12, FAINT, 2.6)
WX, WW = 48, 530
for col, style in enumerate(["legend", "ps"]):
    x = WX + col * 560
    d.rect(x, y0 + 16, WW, 270, BG, LINE, r=8)
    d.rect(x + 12, y0 + 16, WW - 24, 2, d.grad(ACCENT, PURPLE), r=1)
    ty = y0 + 36
    shoulder(d, x + 34, ty + 13, "L1", style)
    tw = (WW - 100) / 3
    for i, t in enumerate(["ALIEN", "DOLBY", "DUALSENSE"]):
        tx = x + 56 + i * tw
        if i == 0:
            d.rect(tx + 4, ty, tw - 8, 26, SELBG, ACCENT, r=4)
        d.caps(tx + tw / 2, ty + 17.5, t, 11, TEXT if i == 0 else FAINT, 2.4, "middle")
    shoulder(d, x + WW - 34, ty + 13, "R1", style)
    # strip placeholder lines
    d.rect(x + 16, ty + 40, WW - 32, 28, "#0B0D10", LINE, r=5)
    for i, (t, c) in enumerate([("BALANCED", ACCENT), ("ATMOS GAME", ACCENT), ("DS 80%", ACCENT)]):
        px = x + 16 + i * (WW - 32) / 3
        d.circle(px + 16, ty + 54, 3.2, c)
        d.mono(px + 26, ty + 58, t, 10, MUTED, 1.4)
    # preset row teaser
    bw = (WW - 32 - 24) / 5
    for i, (t, c) in enumerate([("Battery", "#FFB020"), ("Quiet", "#40DC78"), ("Balanced", ACCENT), ("Perform.", RED), ("Custom", "#9B7BFF")]):
        bx = x + 16 + i * (bw + 6)
        on = i == 2
        d.rect(bx, ty + 84, bw, 50, SELBG if on else CARD, c if on else LINE, r=5)
        if i == 3:
            d.rect(bx - 3, ty + 81, bw + 6, 56, "none", "#FFFFFF", r=7, sw=2)
        d.circle(bx + bw / 2, ty + 101, 5, c)
        d.text(bx + bw / 2, ty + 124, t, 11, TEXT if on or i == 3 else MUTED, B, 400, "middle")
    fy = y0 + 250
    d.line(x, fy - 18, x + WW, fy - 18, LINE)
    hint(d, x + 30, fy, "tri", "Save for this game", style)
    hint(d, x + 210, fy, "sq", "Next preset", style)
    hint(d, x + 340, fy, "cir", "Back", style)
    hint(d, x + 430, fy, "crs", "Select", style)
d.save_png("/workspace/work/gamebar/ps-prompts.png", scale=1.5)

# ---------- image B: save sheet + toast ----------
W, H = 1280, 720
H = 720
d = Doc(W, H)
g = d.uid("bg")
d.defs.append(f'<radialGradient id="{g}" cx="0.6" cy="0.5" r="0.8"><stop offset="0" stop-color="#3A4236"/>'
              f'<stop offset="0.6" stop-color="#1A1F1C"/><stop offset="1" stop-color="#08090A"/></radialGradient>')
d.rect(0, 0, W, H, f"url(#{g})")
for (x, y, r, c) in [(900, 380, 260, "#55603F"), (1100, 600, 180, "#5A3E2A")]:
    b = d.uid("b")
    d.defs.append(f'<filter id="{b}"><feGaussianBlur stdDeviation="70"/></filter>')
    d.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{c}" opacity="0.6" filter="url(#{b})"/>')
d.rect(0, 0, W, H, "#000000", op=0.3)

# step labels
d.caps(60, 40, "1  ONCE, IN GAME", 12, TEXT, 2.6)
d.caps(700, 40, "2  EVERY LAUNCH AFTER THAT", 12, TEXT, 2.6)

# widget with save sheet
wx, wy, ww, wh = 60, 60, 470, 620
d.rect(wx, wy, ww, wh, "#1E1F22", "#2C2E33", r=8)
d.text(wx + 18, wy + 26, "Controls", 14, "#F0F0F0", "Inter", 400)
cx, cy, cw, ch = wx + 1, wy + 42, ww - 2, wh - 43
d.rect(cx, cy, cw, ch, BG)
d.rect(cx + 12, cy, cw - 24, 2, d.grad(ACCENT, PURPLE), r=1)
d.rect(cx, cy, cw, ch, "#000000", op=0.55)  # dimmed widget behind
# sheet
sx, sy, sw_, sh = cx + 18, cy + 70, cw - 36, 420
d.rect(sx, sy, sw_, sh, CARD, ACCENT, r=8)
d.rect(sx + 12, sy, sw_ - 24, 2, d.grad(ACCENT, PURPLE), r=1)
d.icon("device-gamepad-2", sx + 26, sy + 34, 18, ACCENT, 1.7)
d.caps(sx + 44, sy + 39, "SAVE FOR THIS GAME", 12, TEXT, 2.6)
d.text(sx + 20, sy + 68, "Call of Duty: Modern Warfare 4", 15, TEXT, B, 500)
d.mono(sx + 20, sy + 88, "cod.exe", 10.5, FAINT, 1.2)
d.rect(sx + sw_ - 100, sy + 52, 80, 24, "#0B0D10", LINE, r=4)
d.icon("pencil", sx + sw_ - 84, sy + 64, 13, ACCENT, 1.7)
d.text(sx + sw_ - 72, sy + 68.5, "Rename", 11, MUTED)
rows = [("Alien preset", "PERFORMANCE", RED, "gauge"), ("Dolby Atmos", "GAME", ACCENT, "wave-sine"),
        ("DualSense mode", "NATIVE", ACCENT, "device-gamepad-2")]
for i, (k, v, c, ic) in enumerate(rows):
    ry = sy + 108 + i * 50
    on = True
    d.rect(sx + 14, ry, sw_ - 28, 42, SELBG if i == 0 else "#0B0D10", "#FFFFFF" if i == 0 else LINE, r=5, sw=2 if i == 0 else 1)
    d.rect(sx + 26, ry + 13, 16, 16, ACCENT, r=3)
    d.add(f'<path d="M{sx+30} {ry+21}l3 3l6 -7" stroke="#06232A" stroke-width="2" fill="none" stroke-linecap="round"/>')
    d.icon(ic, sx + 62, ry + 21, 16, c, 1.7)
    d.text(sx + 80, ry + 26, k, 13, TEXT)
    d.mono(sx + sw_ - 30, ry + 25, v, 10.5, c, 1.4, "end", 600)
d.text(sx + 20, sy + 280, "Uncheck anything you want left alone.", 12, FAINT)
d.toggle_row(sx + 20, sy + 302, sw_ - 40, "Restore when the game closes", True)
d.rect(sx + 14, sy + sh - 56, (sw_ - 34) / 2, 40, "#06303A", ACCENT, r=5)
face(d, sx + 40, sy + sh - 36, "crs", 11, "legend", 1.6)
d.text(sx + 58, sy + sh - 31, "Save", 13, TEXT, B, 500)
bx2 = sx + 20 + (sw_ - 34) / 2
d.rect(bx2, sy + sh - 56, (sw_ - 34) / 2, 40, "#0B0D10", LINE, r=5)
face(d, bx2 + 26, sy + sh - 36, "cir", 11, "legend", 1.6)
d.text(bx2 + 44, sy + sh - 31, "Cancel", 13, TEXT)


# toast pills (DualSense Controls pill style)
def toast(cxm, y, title, sub, chips, ic="device-gamepad-2", col=ACCENT, w=500):
    x = cxm - w / 2
    d.rect(x, y + 4, w, 64, "#000000", r=32, op=0.4)
    d.rect(x, y, w, 64, "#111317", "#2A3038", r=32)
    d.circle(x + 32, y + 32, 20, col, op=0.18)
    d.icon(ic, x + 32, y + 32, 20, col, 1.8)
    d.text(x + 62, y + 27, title, 14.5, TEXT, B, 600)
    if sub:
        d.text(x + 62, y + 47, sub, 12, MUTED)
    cxp = x + 62
    if chips:
        for t, c in chips:
            tw = len(t) * 7.4 + 22
            d.rect(cxp, y + 36, tw, 18, "#0B0D10", c, r=9)
            d.mono(cxp + tw / 2, y + 49, t, 9.5, c, 1.2, "middle", 600)
            cxp += tw + 6


toast(960, 70, "Call of Duty: Modern Warfare 4", None,
      [("PERFORMANCE", RED), ("ATMOS GAME", ACCENT), ("NATIVE", ACCENT) ], w=460)
d.text(960, 162, "Drops in from the top when the game takes focus, holds 2 s, slides away", 12.5, MUTED, B, 400, "middle")
toast(960, 210, "Back to your setup", "Balanced, Atmos Music, DualSense mode", None, "arrow-back-up", "#7F8B97", w=440)
d.text(960, 302, "When you quit the game", 12.5, MUTED, B, 400, "middle")
# glance strip showing the profile chip
d.rect(740, 340, 440, 120, CARD, LINE, r=8)
d.caps(760, 368, "IN THE OVERLAY", 11, FAINT, 2.4)
d.icon("device-gamepad-2", 768, 398, 15, FAINT, 1.6)
d.text(782, 402.5, "Call of Duty: Modern Warfare 4", 12, MUTED)
d.chip(1030, 388, "GAME PROFILE", "#40DC78", dot=True)
d.text(760, 438, "Change anything, press Triangle again to update it", 12, FAINT)
# classic Windows notification option
kx, ky = 740, 500
d.rect(kx, ky, 440, 150, "#202020", "#353535", r=8)
d.icon("adjustments-horizontal", kx + 22, ky + 24, 14, ACCENT, 1.8)
d.text(kx + 38, ky + 28, "Controls Hub", 12, "#C8C8C8", "Segoe UI")
d.icon("x", kx + 418, ky + 24, 12, "#9A9A9A", 1.6)
d.text(kx + 20, ky + 64, "Call of Duty: Modern Warfare 4", 15, "#FFFFFF", "Segoe UI", 600)
d.text(kx + 20, ky + 90, "Performance preset, Dolby Atmos Game,", 13, "#D0D0D0", "Segoe UI")
d.text(kx + 20, ky + 110, "DualSense in Native mode", 13, "#D0D0D0", "Segoe UI")
d.text(960, ky + 172, "Classic style: a normal Windows notification, set in Controls Hub", 12.5, MUTED, B, 400, "middle")
d.save_png("/workspace/work/gamebar/game-profile.png", scale=1.5)
print("ok")
