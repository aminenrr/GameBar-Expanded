"""Legend look SVG kit for the DualSense Controls Advanced window mockups."""
import base64, re, os, html

HERE = "/workspace/work/gamebar"
ICONS = os.path.join(HERE, "icons")
ASSETS = "/workspace/projects/96da831d-cbeb-4679-8a00-90a88452ffe5/repo/crates/dsz/assets/legend"

BG = "#090A0C"; CARD = "#0E1013"; SIDE = "#0B0D10"; LINE = "#1C2127"; LINE2 = "#16191E"
TEXT = "#F2F6F9"; MUTED = "#A9B4BF"; FAINT = "#6E7A86"; ACCENT = "#00E5FF"; ACC2 = "#00D8F0"
PURPLE = "#7C4DFF"; ORANGE = "#FF8A3D"; GOOD = "#40DC78"; BAD = "#FF4D5E"; SELBG = "#0A1A1F"

H = "Barlow"; B = "Inter"; M = "Cascadia Mono"

_icon_cache = {}


def esc(t):
    return html.escape(str(t), quote=True)


def _icon_body(name):
    if name not in _icon_cache:
        s = open(os.path.join(ICONS, name + ".svg")).read()
        s = re.sub(r"<!--.*?-->", "", s, flags=re.S)
        body = re.search(r"<svg[^>]*>(.*)</svg>", s, re.S).group(1)
        body = re.sub(r'<path stroke="none"[^>]*/>', "", body)
        _icon_cache[name] = body
    return _icon_cache[name]


def b64(path):
    return base64.b64encode(open(path, "rb").read()).decode()


class Doc:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.s = []
        self.defs = []
        self._id = 0

    def uid(self, p="i"):
        self._id += 1
        return f"{p}{self._id}"

    def add(self, x):
        self.s.append(x)

    # primitives
    def rect(self, x, y, w, h, fill="none", stroke=None, r=0, sw=1, op=1, extra=""):
        st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
        self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}"{st} opacity="{op}" {extra}/>')

    def line(self, x1, y1, x2, y2, c=LINE, sw=1, op=1, extra=""):
        self.add(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{c}" stroke-width="{sw}" opacity="{op}" {extra}/>')

    def circle(self, cx, cy, r, fill="none", stroke=None, sw=1, op=1, extra=""):
        st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
        self.add(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{fill}"{st} opacity="{op}" {extra}/>')

    def text(self, x, y, t, size=13, c=TEXT, font=B, weight=400, anchor="start", ls=0, op=1):
        self.add(f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" font-weight="{weight}" fill="{c}" '
                 f'text-anchor="{anchor}" letter-spacing="{ls}" opacity="{op}">{esc(t)}</text>')

    def caps(self, x, y, t, size=11, c=TEXT, ls=2.4, anchor="start", weight=600):
        self.text(x, y, t, size, c, H, weight, anchor, ls)

    def mono(self, x, y, t, size=10.5, c=FAINT, ls=1.2, anchor="start", weight=400):
        self.text(x, y, t, size, c, M, weight, anchor, ls)

    def icon(self, name, cx, cy, size=16, c=MUTED, sw=1.8):
        s = size / 24.0
        self.add(f'<g transform="translate({cx - size / 2},{cy - size / 2}) scale({s})" fill="none" stroke="{c}" '
                 f'stroke-width="{sw / s * s}" stroke-linecap="round" stroke-linejoin="round">{_icon_body(name)}</g>')

    def image(self, path, x, y, w, h, op=1):
        self.add(f'<image x="{x}" y="{y}" width="{w}" height="{h}" opacity="{op}" '
                 f'href="data:image/png;base64,{b64(path)}" xlink:href="data:image/png;base64,{b64(path)}"/>')

    def tinted_logo(self, path, x, y, w, h, color):
        fid = self.uid("f")
        self.defs.append(f'<filter id="{fid}"><feFlood flood-color="{color}"/><feComposite in2="SourceAlpha" operator="in"/></filter>')
        self.add(f'<image x="{x}" y="{y}" width="{w}" height="{h}" filter="url(#{fid})" xlink:href="data:image/png;base64,{b64(path)}"/>')

    def svg(self):
        return (f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
                f'width="{self.w}" height="{self.h}" viewBox="0 0 {self.w} {self.h}"><defs>{"".join(self.defs)}</defs>'
                + "".join(self.s) + "</svg>")

    def save_png(self, path, scale=2):
        import cairosvg
        open(path.replace(".png", ".svg"), "w").write(self.svg())
        cairosvg.svg2png(bytestring=self.svg().encode(), write_to=path, scale=scale)

    # ---------- widgets ----------
    def grad(self, c1=ACCENT, c2=PURPLE, horiz=True):
        gid = self.uid("g")
        self.defs.append(f'<linearGradient id="{gid}" x1="0" y1="0" x2="{1 if horiz else 0}" y2="{0 if horiz else 1}">'
                         f'<stop offset="0" stop-color="{c1}"/><stop offset="1" stop-color="{c2}"/></linearGradient>')
        return f"url(#{gid})"

    def fade_grad(self, c, a1=1, a2=0):
        gid = self.uid("g")
        self.defs.append(f'<linearGradient id="{gid}" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="{c}" stop-opacity="{a1}"/>'
                         f'<stop offset="1" stop-color="{c}" stop-opacity="{a2}"/></linearGradient>')
        return f"url(#{gid})"

    def hex_pattern(self, x, y, w, h):
        pid = self.uid("p")
        self.defs.append(
            f'<pattern id="{pid}" width="28" height="48.5" patternUnits="userSpaceOnUse">'
            f'<path d="M14 0 L28 8.08 L28 24.25 L14 32.33 L0 24.25 L0 8.08 Z M14 32.33 L14 48.5" fill="none" stroke="#FFFFFF" stroke-width="0.6"/></pattern>')
        cid = self.uid("c")
        self.defs.append(f'<clipPath id="{cid}"><rect x="{x}" y="{y}" width="{w}" height="{h}" rx="10"/></clipPath>')
        self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="url(#{pid})" opacity="0.035" clip-path="url(#{cid})"/>')

    def card(self, x, y, w, h):
        self.rect(x, y, w, h, CARD, LINE, r=6)

    def section(self, x, y, w, icon, title, note=""):
        """Legend section header: icon, spaced caps title, rule, right note."""
        self.icon(icon, x + 8, y, 15, ACCENT)
        self.caps(x + 24, y + 4.5, title, 12, TEXT, 2.6)
        tw = len(title) * 10.2 + 30
        nw = len(note) * 8.0 + 14 if note else 0
        self.line(x + tw, y, x + w - nw, y, LINE2)
        if note:
            self.mono(x + w, y + 4, note, 10.5, FAINT, 1.4, "end")

    def page_header(self, x, y, w, title, sub, note=""):
        self.caps(x, y + 16, title, 19, TEXT, 3.2)
        rule = self.grad()
        self.rect(x, y + 27, 300, 2, rule, r=1)
        self.text(x, y + 48, sub, 12, MUTED)
        if note:
            self.mono(x + w, y + 16, note, 10.5, FAINT, 1.4, "end")

    def label(self, x, y, t, c=MUTED, size=12.5):
        self.text(x, y, t, size, c, B, 400)

    def toggle(self, x, y, on=True):
        if on:
            self.rect(x, y, 36, 18, "#06262C", ACCENT, r=4)
            self.rect(x + 21, y + 3, 12, 12, ACCENT, r=2)
        else:
            self.rect(x, y, 36, 18, "#14181C", LINE, r=4)
            self.rect(x + 3, y + 3, 12, 12, "#4A535C", r=2)

    def toggle_row(self, x, y, w, t, on=True, sub=None):
        self.label(x, y + 4, t, TEXT if on else MUTED, 12.5)
        if sub:
            self.text(x, y + 19, sub, 10.5, FAINT)
        self.toggle(x + w - 36, y - 9, on)

    def slider(self, x, y, w, frac, c=ACCENT, center=None):
        self.rect(x, y - 1, w, 2, LINE, r=1)
        kx = x + w * frac
        if center is None:
            self.rect(x, y - 1, kx - x, 2, c, r=1)
        else:
            cx = x + w * center
            a, b2 = sorted([cx, kx])
            self.rect(a, y - 1, b2 - a, 2, c, r=1)
            self.line(cx, y - 5, cx, y + 5, c, 1)
        self.rect(kx - 6, y - 9, 12, 18, "#E9EEF2", r=2.5)

    def slider_row(self, x, y, w, t, frac, val, c=ACCENT, center=None, lw=96, vw=46):
        self.label(x, y + 4, t, MUTED, 12.5)
        self.slider(x + lw, y, w - lw - vw - 10, frac, c, center)
        self.mono(x + w, y + 4, val, 11.5, c if c != ACCENT else ACCENT, 0.6, "end", 600)

    def seg(self, x, y, w, opts, sel, h=26, mono=True, accent=ACCENT):
        """Compact segmented pill (like HD HAPTICS | RUMBLE)."""
        self.rect(x, y, w, h, "#0B0D10", LINE, r=4)
        cw = w / len(opts)
        for i, o in enumerate(opts):
            cx = x + i * cw
            if i == sel:
                self.rect(cx + 2, y + 2, cw - 4, h - 4, "#062A31", r=3)
            col = accent if i == sel else FAINT
            if mono:
                self.mono(cx + cw / 2, y + h / 2 + 4, o, 10.5, col, 1.5, "middle", 600 if i == sel else 400)
            else:
                self.text(cx + cw / 2, y + h / 2 + 4.5, o, 12, TEXT if i == sel else MUTED, B, 400, "middle")

    def sel_buttons(self, x, y, w, opts, sel, h=34, gap=6, accent=ACCENT, subs=None):
        """Row of big select buttons with the selected underline glow."""
        bw = (w - gap * (len(opts) - 1)) / len(opts)
        for i, o in enumerate(opts):
            bx = x + i * (bw + gap)
            on = i == sel
            self.rect(bx, y, bw, h, SELBG if on else CARD, accent if on else LINE, r=5)
            ty = y + h / 2 + 4.5 if not subs else y + h / 2 - 1
            self.text(bx + bw / 2, ty, o, 12.5, TEXT if on else MUTED, B, 400, "middle")
            if subs:
                self.mono(bx + bw / 2, y + h / 2 + 13, subs[i], 9.5, FAINT, 0.6, "middle")
            if on:
                self.rect(bx + bw * 0.28, y + h - 3, bw * 0.44, 2, accent, r=1)
                self.rect(bx + bw * 0.28 - 2, y + h - 5, bw * 0.44 + 4, 6, accent, r=3, op=0.18)

    def button(self, x, y, t, icon=None, w=None, h=28, primary=False, danger=False):
        w = w or (len(t) * 7.4 + (34 if icon else 22))
        if primary:
            self.rect(x, y, w, h, "#06303A", ACCENT, r=5)
        else:
            self.rect(x, y, w, h, "#101318", LINE, r=5)
        col = ACCENT if primary else (ORANGE if danger else TEXT)
        if icon:
            self.icon(icon, x + 15, y + h / 2, 14, col)
            self.text(x + 28, y + h / 2 + 4.5, t, 12.5, col if primary or danger else TEXT)
        else:
            self.text(x + w / 2, y + h / 2 + 4.5, t, 12.5, col, B, 500 if primary else 400, "middle")
        return w

    def dropdown(self, x, y, w, prefix, val, h=30):
        self.rect(x, y, w, h, "#0B0D10", LINE, r=5)
        px = x + 12
        if prefix:
            self.mono(px, y + h / 2 + 4, prefix, 10.5, FAINT, 1.6)
            px += len(prefix) * 8.2 + 8
        self.text(px, y + h / 2 + 4.5, val, 12.5, TEXT)
        self.icon("chevron-down", x + w - 16, y + h / 2, 14, MUTED)

    def keycap(self, x, y, t, anchor_right=True):
        w = len(t) * 7.6 + 16
        x0 = x - w if anchor_right else x
        self.rect(x0, y - 11, w, 22, "#0B0D10", LINE, r=4)
        self.mono(x0 + w / 2, y + 4, t, 10.5, TEXT, 1.6, "middle")
        return w

    def chip(self, x, y, t, c=ACCENT, icon=None, dot=False):
        w = len(t) * 8.4 + 18 + (14 if icon or dot else 0)
        self.rect(x, y, w, 20, "#071A1F", c, r=4, op=1)
        tx = x + 9
        if icon:
            self.icon(icon, tx + 5, y + 10, 11, c, 2)
            tx += 14
        if dot:
            self.circle(tx + 4, y + 10, 3.2, c)
            tx += 14
        self.mono(tx, y + 14, t, 11, c, 1.8, "start", 600)
        return w

    def ticks(self, x, y, w, frac, c=ACCENT, n=None, h=4):
        n = n or int(w // 10)
        gap = 3
        tw = (w - gap * (n - 1)) / n
        on = round(frac * n)
        for i in range(n):
            self.rect(x + i * (tw + gap), y, tw, h, c if i < on else "#1C2127", r=0.5)

    def hatched(self, x, y, w, h):
        pid = self.uid("h")
        self.defs.append(f'<pattern id="{pid}" width="5" height="5" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
                         f'<rect width="2" height="5" fill="{ORANGE}" opacity="0.55"/></pattern>')
        self.rect(x, y, w, h, f"url(#{pid})")

    def stick(self, cx, cy, r, dx, dy, inner=0.08):
        self.circle(cx, cy, r, "#0B0D10", LINE, 1)
        self.circle(cx, cy, r - 6, "none", ACCENT, 1, 0.55, 'stroke-dasharray="3 4"')
        self.line(cx - r, cy, cx + r, cy, LINE2)
        self.line(cx, cy - r, cx, cy + r, LINE2)
        if inner:
            self.circle(cx, cy, r * inner + 2, "none", ORANGE, 1.2, 0.9)
        self.circle(cx + dx * (r - 6), cy + dy * (r - 6), 5.5, "#FFFFFF")
        self.circle(cx + dx * (r - 6), cy + dy * (r - 6), 10, ACCENT, op=0.18)
