"""Renders the package logos in the Legend style (dark window, cyan ring, white sliders).
Run: python make_assets.py (needs Pillow). Output PNGs sit next to this script."""
from pathlib import Path
from PIL import Image, ImageDraw

BG = (9, 10, 12, 255)
CYAN = (0, 229, 255, 255)
PURPLE = (124, 77, 255, 255)
WHITE = (242, 246, 249, 255)
OUT = Path(__file__).parent


def glyph(size, transparent=False):
    s = size * 4
    im = Image.new("RGBA", (s, s), (0, 0, 0, 0) if transparent else BG)
    d = ImageDraw.Draw(im)
    pad = s * 0.12
    w = max(4, int(s * 0.06))
    d.ellipse([pad, pad, s - pad, s - pad], outline=CYAN, width=w)
    # three slider bars with knobs
    x0, x1 = s * 0.32, s * 0.68
    for i, k in enumerate((0.42, 0.62, 0.50)):
        y = s * (0.38 + i * 0.12)
        d.line([x0, y, x1, y], fill=WHITE, width=max(2, int(s * 0.03)))
        kx = x0 + (x1 - x0) * k
        r = s * 0.035
        d.ellipse([kx - r, y - r, kx + r, y + r], fill=CYAN if i != 1 else PURPLE)
    return im.resize((size, size), Image.LANCZOS)


def wide(w, h):
    im = Image.new("RGBA", (w, h), BG)
    g = glyph(int(h * 0.6), transparent=True)
    im.alpha_composite(g, ((w - g.width) // 2, (h - g.height) // 2))
    d = ImageDraw.Draw(im)
    for x in range(w):  # thin cyan to purple top edge
        t = x / max(1, w - 1)
        c = tuple(int(CYAN[i] * (1 - t) + PURPLE[i] * t) for i in range(3)) + (255,)
        d.line([x, 0, x, max(1, h // 75)], fill=c)
    return im


glyph(50).save(OUT / "StoreLogo.png")
glyph(150).save(OUT / "Square150x150Logo.png")
glyph(44, transparent=True).save(OUT / "Square44x44Logo.png")
wide(310, 150).save(OUT / "Wide310x150Logo.png")
wide(620, 300).save(OUT / "SplashScreen.png")
glyph(64, transparent=True).save(OUT.parent / "GameBar" / "Icon.png")
