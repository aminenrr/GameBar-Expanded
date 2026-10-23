import sys
from legend import *

LOGO = "/workspace/work/alien-control/src/AlienControl/Assets/logo.png"
LA = "/workspace/projects/96da831d-cbeb-4679-8a00-90a88452ffe5/repo/crates/dsz/assets/legend"
W, H = 1280, 760
AMBER = "#FFB020"; GREEN = "#40DC78"; RED = "#FF4D5E"; VIOLET = "#9B7BFF"


def frame(tab):
    d = Doc(W, H)
    g = d.uid("bg")
    d.defs.append(f'<radialGradient id="{g}" cx="0.72" cy="0.45" r="0.8"><stop offset="0" stop-color="#2B3A33"/>'
                  f'<stop offset="0.5" stop-color="#16201E"/><stop offset="1" stop-color="#07090A"/></radialGradient>')
    d.rect(0, 0, W, H, f"url(#{g})")
    for (x, y, r, c) in [(980, 300, 220, "#3D5A4A"), (760, 520, 160, "#2A3B4F"), (1150, 600, 200, "#4A3A2A")]:
        b = d.uid("b")
        d.defs.append(f'<filter id="{b}"><feGaussianBlur stdDeviation="60"/></filter>')
        d.add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{c}" opacity="0.55" filter="url(#{b})"/>')
    d.rect(0, 0, W, H, "#000000", op=0.42)
    # Game Bar home bar: one Controls icon
    bx, by, bw, bh = 40, 22, 600, 52
    d.rect(bx, by, bw, bh, "#1E1F22", "#2C2E33", r=8, op=0.96)
    ix = bx + 34
    for i, ic in enumerate(["brand-xbox", "HUB", "volume", "chart-line", "camera", "device-gamepad-2", "settings"]):
        if ic == "HUB":
            d.rect(ix - 22, by + 6, 44, 40, "#3A3D44", r=6)
            d.rect(ix - 10, by + bh - 7, 20, 3, ACCENT, r=1.5)
            d.icon("adjustments-horizontal", ix, by + bh / 2 - 1, 22, "#FFFFFF", 1.7)
        else:
            d.icon(ic, ix, by + bh / 2, 22, "#E6E6E6", 1.6)
        if i == 0:
            d.line(ix + 32, by + 14, ix + 32, by + bh - 14, "#44474D", 1)
            ix += 20
        ix += 76
    d.icon("microphone", 1018, 48, 20, "#FFFFFF", 1.6)
    d.icon("wifi", 1052, 48, 20, "#FFFFFF", 1.6)
    d.icon("battery-3", 1088, 48, 22, "#FFFFFF", 1.6)
    d.text(1236, 57, "4:50 PM", 26, "#FFFFFF", "Inter", 600, "end")
    d.text(W - 40, H - 22, "Concept: one Game Bar widget for all three apps, controller first", 12, "#8A939C", "Inter", 400, "end")
    # widget chrome
    wx, wy, ww, wh = 40, 92, 470, 650
    d.rect(wx - 1, wy + 6, ww + 2, wh, "#000000", r=10, op=0.5)
    d.rect(wx, wy, ww, wh, "#1E1F22", "#2C2E33", r=8)
    d.text(wx + 18, wy + 26, "Controls", 14, "#F0F0F0", "Inter", 400)
    d.icon("pinned", wx + ww - 52, wy + 21, 16, "#CFCFCF", 1.6)
    d.icon("x", wx + ww - 22, wy + 21, 16, "#CFCFCF", 1.6)
    cx, cy, cw, ch = wx + 1, wy + 42, ww - 2, wh - 43
    d.rect(cx, cy, cw, ch, BG)
    d.rect(cx, cy + ch - 10, cw, 10, BG, r=8)
    d.hex_pattern(cx, cy, cw, 70)
    d.rect(cx + 12, cy, cw - 24, 2, d.grad(ACCENT, PURPLE), r=1)
    P, IW = cx + 16, cw - 32
    # tabs
    ty = cy + 14
    d.keycap(P + 28, ty + 13, "LB")
    tw = (IW - 76) / 3
    for i, t in enumerate(["ALIEN", "DOLBY", "DUALSENSE"]):
        tx = P + 36 + i * tw
        on = i == tab
        if on:
            d.rect(tx + 4, ty, tw - 8, 26, SELBG, ACCENT, r=4)
        d.caps(tx + tw / 2, ty + 17.5, t, 11, TEXT if on else FAINT, 2.4, "middle")
    d.keycap(P + IW, ty + 13, "RB")
    # glance strip: all three apps at once, on every tab
    gy = ty + 36
    d.rect(P, gy, IW, 28, "#0B0D10", LINE, r=5)
    items = [(ACCENT, "BALANCED"), (ACCENT, "ATMOS GAME"), (ACCENT, "DS 80%")]
    sw = IW / 3
    for i, (c, t) in enumerate(items):
        x = P + i * sw
        if i:
            d.line(x, gy + 7, x, gy + 21, LINE)
        d.circle(x + 16, gy + 14, 3.2, c)
        d.mono(x + 26, gy + 18, t, 10, MUTED if i != tab else TEXT, 1.4, "start", 600 if i == tab else 400)
    # game row
    ry = gy + 42
    d.icon("device-gamepad-2", P + 8, ry, 15, FAINT, 1.6)
    d.text(P + 22, ry + 4.5, "Call of Duty: Modern Warfare 4", 12, MUTED)
    d.chip(P + IW - 132, ry - 10, "GAME PROFILE", GREEN, dot=True)
    # footer
    fy = cy + ch - 32
    d.line(cx, fy - 10, cx + cw, fy - 10, LINE)

    def hint(x, letter, col, label):
        d.circle(x, fy + 10, 10, col)
        d.text(x, fy + 14.5, letter, 12, "#FFFFFF", "Inter", 600, "middle")
        d.text(x + 16, fy + 15, label, 12.5, TEXT, B, 400)
    hint(P + 70, "Y", "#B34FD0", "Save for this game")
    hint(P + 262, "B", "#7559D6", "Back")
    hint(P + 344, "A", "#4F6BD6", "Select")
    return d, P, IW, ry + 26, fy - 64


def open_btn(d, P, IW, y, name):
    d.rect(P, y, IW, 40, "#06303A", ACCENT, r=5)
    d.text(P + 16, y + 25, f"Open {name}", 13, TEXT, B, 500)
    d.icon("external-link", P + IW - 20, y + 20, 16, ACCENT, 1.7)


def tiles(d, x, y, w, items, h=70, sel=None):
    n = len(items)
    tw = (w - 6 * (n - 1)) / n
    for i, (ic, t, v, c) in enumerate(items):
        tx = x + i * (tw + 6)
        on = sel == i
        d.rect(tx, y, tw, h, SELBG if on else CARD, ACCENT if on else LINE, r=5)
        d.icon(ic, tx + tw / 2, y + 20, 19, c, 1.6)
        d.text(tx + tw / 2, y + 43, t, 11, TEXT if on else MUTED, B, 400, "middle")
        d.mono(tx + tw / 2, y + 59, v, 9.5, c if c != "#7F8B97" else TEXT, 1.2, "middle", 600)


def big_buttons(d, x, y, w, items, sel, h=66, focus=None):
    n = len(items)
    gap = 6
    bw = (w - gap * (n - 1)) / n
    for i, (t, ic, c) in enumerate(items):
        bx = x + i * (bw + gap)
        on = i == sel
        d.rect(bx, y, bw, h, SELBG if on else CARD, c if on else LINE, r=5)
        if focus == i:
            d.rect(bx - 3, y - 3, bw + 6, h + 6, "none", "#FFFFFF", r=7, sw=2)
        if ic.endswith(".png"):
            d.tinted_logo(ic, bx + bw / 2 - 10, y + 12, 20, 20, c if on else "#7F8B97")
        else:
            d.icon(ic, bx + bw / 2, y + 24, 21, c, 1.7)
        d.text(bx + bw / 2, y + 51, t, 11.5, TEXT if on or focus == i else MUTED, B, 400, "middle")
        if on:
            d.rect(bx + bw * 0.3, y + h - 3, bw * 0.4, 2, c, r=1)


def alien():
    d, P, IW, y, oy = frame(0)
    d.tinted_logo(LOGO, P, y - 2, 13, 19, TEXT)
    d.caps(P + 22, y + 13, "ALIEN CONTROLS", 13, TEXT, 3)
    d.chip(P + 186, y - 1, "m15 R7", ACCENT)
    d.mono(P + IW, y + 12, "AC  ~82 W", 11, ACCENT, 1.2, "end", 600)
    sy = y + 30
    cells = [("CPU", "64", "°C", 0.64), ("GPU", "71", "°C", 0.71), ("CPU FAN", "48", "%", 0.48), ("GPU FAN", "52", "%", 0.52)]
    d.card(P, sy, IW, 62)
    cwid = IW / 4
    for i, (k, v, u, f) in enumerate(cells):
        x = P + i * cwid
        if i:
            d.line(x, sy + 10, x, sy + 52, LINE)
        d.mono(x + 12, sy + 18, k, 9.5, FAINT, 1.8)
        d.text(x + 12, sy + 41, v, 20, TEXT, H, 600)
        d.mono(x + 16 + len(v) * 12.5, sy + 41, u, 10.5, FAINT, 0.6)
        d.ticks(x + 12, sy + 49, cwid - 26, f, ACCENT, n=8, h=3)
    py = sy + 88
    d.section(P, py, IW, "adjustments-alt", "PRESET", "F2")
    big_buttons(d, P, py + 16, IW, [("Battery", "battery-2", AMBER), ("Quiet", "moon", GREEN), ("Balanced", "scale", ACCENT),
                                     ("Perform.", "gauge", RED), ("Custom", "settings", VIOLET)], 2, focus=3)
    d.text(P, py + 104, "Power mode, CPU boost and refresh rate in one press", 11, FAINT)
    qy = py + 136
    d.section(P, qy, IW, "bolt", "QUICK", "")
    tiles(d, P, qy + 16, IW, [("flame", "G-Mode", "OFF  F1", ORANGE), ("device-desktop-analytics", "Refresh", "240 HZ", ACCENT),
                              ("keyboard", "Key light", "60%", ACCENT), ("cpu", "Clean RAM", "15.1 GB", "#7F8B97")])
    open_btn(d, P, IW, oy, "Alien Controls")
    return d


def dolby():
    d, P, IW, y, oy = frame(1)
    d.icon("device-speaker", P + 7, y + 8, 17, TEXT, 1.7)
    d.caps(P + 22, y + 13, "DOLBY CONTROLS", 13, TEXT, 3)
    d.mono(P + IW, y + 12, "CTRL+ALT+O", 10.5, FAINT, 1.2, "end")
    oyy = y + 30
    rows = [("headphones", "PRO X Wireless", "ACTIVE", True, True), ("device-speaker", "Speakers (Realtek)", "", False, True),
            ("ear", "Pixel Buds Pro", "IN CASE  L 80  R 76", False, False)]
    for i, (ic, t, v, on, avail) in enumerate(rows):
        ry = oyy + i * 36
        d.rect(P, ry, IW, 32, SELBG if on else CARD, ACCENT if on else LINE, r=5)
        if on:
            d.rect(P, ry + 8, 3, 16, ACCENT, r=1.5)
        d.icon(ic, P + 20, ry + 16, 16, ACCENT if on else ("#7F8B97" if avail else "#3E4650"), 1.6)
        d.text(P + 38, ry + 20.5, t, 12.5, TEXT if on else (MUTED if avail else FAINT))
        if v:
            d.mono(P + IW - 12, ry + 20, v, 9.5, ACCENT if on else FAINT, 1.2, "end", 600 if on else 400)
    ay = oyy + 126
    d.section(P, ay, IW - 50, "wave-sine", "DOLBY ATMOS", "CTRL+ALT+P")
    d.toggle(P + IW - 36, ay - 9, True)
    big_buttons(d, P, ay + 18, IW, [("Dynamic", "activity", ACCENT), ("Movie", "player-play", ACCENT), ("Music", "music", ACCENT),
                                     ("Game", "device-gamepad-2", ACCENT), ("Voice", "microphone", ACCENT),
                                     ("Custom", "adjustments-horizontal", ACCENT)], 3, h=62, focus=None)
    hy = ay + 112
    d.section(P, hy, IW, "headphones", "PRO X", "PLUGGED IN")
    d.card(P, hy + 16, IW, 70)
    d.slider_row(P + 14, hy + 38, IW - 28, "Volume", 0.72, "72")
    d.slider_row(P + 14, hy + 66, IW - 28, "Mic level", 0.85, "85")
    open_btn(d, P, IW, oy, "Dolby Controls")
    return d


def dualsense():
    d, P, IW, y, oy = frame(2)
    d.tinted_logo(f"{LA}/ps-logo.png", P, y - 1, 17, 17, TEXT)
    d.caps(P + 24, y + 13, "DUALSENSE CONTROLS", 13, TEXT, 3)
    d.chip(P + IW - 58, y - 1, "BT", ACCENT, icon="bluetooth")
    cy0 = y + 28
    d.card(P, cy0, IW, 74)
    d.image(f"{LA}/pads/white.png", P + 10, cy0 + 8, 84, 58)
    stats = [("BATTERY", "80%"), ("LINK", "250 HZ"), ("HAPTIC LAG", "33 MS")]
    sx = P + 106
    sw = (IW - 116) / 3
    for i, (k, v) in enumerate(stats):
        x = sx + i * sw
        d.mono(x, cy0 + 28, k, 9.5, FAINT, 1.6)
        d.text(x, cy0 + 52, v.replace(" HZ", "").replace(" MS", "").replace("%", ""), 19, TEXT, H, 600)
        unit = "%" if "%" in v else v.split(" ")[-1].lower() if " " in v else ""
        num = v.split(" ")[0].replace("%", "")
        d.mono(x + len(num) * 12.5 + 6, cy0 + 52, unit.upper() if unit != "%" else unit, 10, FAINT, 0.6)
        if k == "BATTERY":
            d.ticks(x, cy0 + 60, sw - 18, 0.8, ACCENT, n=6, h=3)
    my = cy0 + 100
    d.section(P, my, IW, "device-gamepad-3", "MODE", "CTRL+ALT+C")
    big_buttons(d, P, my + 16, IW, [("Native", "device-gamepad-2", ACCENT), ("DualSense", f"{LA}/ps-logo.png", ACCENT),
                                     ("Xbox", f"{LA}/xbox-logo.png", GREEN)], 1, h=62)
    gy = my + 104
    d.section(P, gy, IW, "wave-sine", "FEEL", "OVER GAME OUTPUT")
    d.card(P, gy + 16, IW, 98)
    d.slider_row(P + 14, gy + 38, IW - 28, "Haptics", 0.6, "+20", ACCENT, center=0.5)
    d.slider_row(P + 14, gy + 66, IW - 28, "Rumble", 0.42, "-15", ORANGE, center=0.5)
    d.slider_row(P + 14, gy + 94, IW - 28, "Trigger cap", 0.7, "70%")
    open_btn(d, P, IW, oy, "DualSense Controls")
    return d


for n, f in [("1-alien", alien), ("2-dolby", dolby), ("3-dualsense", dualsense)]:
    f().save_png(f"/workspace/work/gamebar/hub-{n}.png", scale=1.5)
print("ok")
