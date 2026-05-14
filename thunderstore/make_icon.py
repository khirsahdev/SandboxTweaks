"""Generates icon.png (256x256) for the Thunderstore package: three toggle
switches on a dark panel — the 'pick your tweaks' motif."""
from PIL import Image, ImageDraw

S = 256
BG_TOP = (28, 33, 48)
BG_BOT = (16, 19, 30)
GOLD = (255, 199, 51)
GOLD_DIM = (120, 96, 36)
TRACK = (44, 50, 70)
WHITE = (240, 240, 245)

img = Image.new("RGB", (S, S), BG_BOT)
px = img.load()
for y in range(S):  # vertical gradient
    t = y / (S - 1)
    r = int(BG_TOP[0] * (1 - t) + BG_BOT[0] * t)
    g = int(BG_TOP[1] * (1 - t) + BG_BOT[1] * t)
    b = int(BG_TOP[2] * (1 - t) + BG_BOT[2] * t)
    for x in range(S):
        px[x, y] = (r, g, b)

d = ImageDraw.Draw(img)

# gold border frame
d.rounded_rectangle([6, 6, S - 7, S - 7], radius=26, outline=GOLD_DIM, width=3)

# three toggle switches, alternating on/off
rows = [(70, True), (128, False), (186, True)]
track_w, track_h = 132, 40
tx = (S - track_w) // 2
for cy, on in rows:
    y0 = cy - track_h // 2
    y1 = cy + track_h // 2
    # track
    d.rounded_rectangle([tx, y0, tx + track_w, y1], radius=track_h // 2,
                        fill=(GOLD if on else TRACK))
    # knob
    knob_r = track_h // 2 - 5
    knob_cx = (tx + track_w - track_h // 2) if on else (tx + track_h // 2)
    d.ellipse([knob_cx - knob_r, cy - knob_r, knob_cx + knob_r, cy + knob_r],
              fill=WHITE if on else (150, 156, 172))

img.save("icon.png")
print("wrote icon.png", img.size)
