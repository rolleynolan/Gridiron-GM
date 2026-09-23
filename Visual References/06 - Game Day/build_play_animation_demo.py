from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


SOURCE = Path(r"C:\Users\norma\.codex\generated_images\01a089ed-69fe-7472-91b5-d0b237fc5099\exec-efe962fe-42b0-4094-aea6-7da3d9e1e0f6.png")
OUT_DIR = Path(r"C:\Users\norma\Desktop\GridironGM\Visual References\06 - Game Day")
SHEET_OUT = OUT_DIR / "receiver-run-cycle-v1.png"
GIF_OUT = OUT_DIR / "live-play-animation-demo-v1.gif"
STORY_OUT = OUT_DIR / "live-play-animation-storyboard-v1.png"


def remove_magenta(im: Image.Image) -> Image.Image:
    rgba = im.convert("RGBA")
    pixels = []
    for r, g, b, _ in rgba.getdata():
        is_key = r > 205 and b > 160 and g < 85 and (r + b) > (g * 4)
        pixels.append((r, g, b, 0 if is_key else 255))
    rgba.putdata(pixels)
    return rgba


def normalize_frame(cell: Image.Image, tint_defense: bool = False) -> Image.Image:
    bbox = cell.getbbox()
    sprite = cell.crop(bbox) if bbox else cell
    target_h = 44
    target_w = max(1, round(sprite.width * target_h / sprite.height))
    sprite = sprite.resize((target_w, target_h), Image.Resampling.NEAREST)
    if tint_defense:
        data = []
        for r, g, b, a in sprite.getdata():
            if a == 0:
                data.append((0, 0, 0, 0))
                continue
            # Recolor dark uniform material navy/royal blue and gold accents white.
            if r < 100 and g < 100 and b < 100:
                data.append((22, 62, 142, a))
            elif r > 145 and g > 95 and b < 95:
                data.append((225, 236, 250, a))
            else:
                data.append((r, g, b, a))
        sprite.putdata(data)
    canvas = Image.new("RGBA", (64, 56), (0, 0, 0, 0))
    canvas.alpha_composite(sprite, ((64 - sprite.width) // 2, 54 - sprite.height))
    return canvas


def build_cycles():
    sheet = remove_magenta(Image.open(SOURCE))
    w, h = sheet.size
    offense = []
    defense = []
    for i in range(8):
        left = round(i * w / 8)
        right = round((i + 1) * w / 8)
        cell = sheet.crop((left, 0, right, h))
        offense.append(normalize_frame(cell))
        defense.append(normalize_frame(cell, tint_defense=True))

    export = Image.new("RGBA", (64 * 8, 56), (0, 0, 0, 0))
    for i, frame in enumerate(offense):
        export.alpha_composite(frame, (64 * i, 0))
    export.save(SHEET_OUT)
    return offense, defense


def draw_field(draw: ImageDraw.ImageDraw):
    draw.rectangle((0, 0, 319, 159), fill=(25, 102, 46))
    for x in range(18, 320, 30):
        draw.line((x, 0, x, 159), fill=(213, 226, 197), width=1)
        for y in range(10, 151, 10):
            draw.line((x - 2, y, x + 2, y), fill=(213, 226, 197), width=1)
    for x in range(3, 320, 6):
        draw.point((x, 52), fill=(192, 211, 177))
        draw.point((x, 107), fill=(192, 211, 177))
    draw.line((92, 0, 92, 159), fill=(35, 185, 232), width=2)
    draw.line((198, 0, 198, 159), fill=(241, 190, 43), width=2)


def paste_grounded(base: Image.Image, sprite: Image.Image, x: int, y: int):
    base.alpha_composite(sprite, (x - 32, y - 54))


def make_demo(offense, defense):
    frames = []
    total = 32
    font = ImageFont.load_default()
    for t in range(total):
        base = Image.new("RGBA", (320, 180), (5, 18, 31, 255))
        field = Image.new("RGBA", (320, 160), (0, 0, 0, 0))
        draw = ImageDraw.Draw(field)
        draw_field(draw)

        # Receiver releases horizontally, catches, then turns upfield.
        if t < 18:
            rx = 101 + t * 5
            ry = 91
            receiver = offense[t % 8]
        else:
            rx = 186 + (t - 18) * 2
            ry = 91 - (t - 18) * 4
            receiver = offense[t % 8]

        # Defender trails with a reaction delay and closes after the catch.
        if t < 4:
            dx, dy = 110, 71
        elif t < 18:
            dx, dy = 110 + (t - 4) * 4, 71
        else:
            dx = 166 + (t - 18) * 3
            dy = 71 - (t - 18) * 3
        defender = defense[(t - 4) % 8]

        # Quarterback drop and compact blocking pocket.
        qx = 77 - min(t, 6) * 2
        qy = 92
        paste_grounded(field, offense[(t // 2) % 8], qx, qy)
        for ly in (65, 78, 104, 117):
            paste_grounded(field, offense[0].resize((38, 34), Image.Resampling.NEAREST), 91, ly)
            paste_grounded(field, defense[0].resize((38, 34), Image.Resampling.NEAREST), 102, ly)

        paste_grounded(field, defender, dx, dy)
        paste_grounded(field, receiver, rx, ry)

        # Ball follows an arc from the quarterback to the receiver during frames 9-17.
        if 9 <= t <= 17:
            p = (t - 9) / 8
            bx = round(qx + 10 + (rx - qx - 12) * p)
            by = round(qy - 34 + (ry - qy + 28) * p - 18 * (4 * p * (1 - p)))
            draw.rectangle((bx, by, bx + 3, by + 1), fill=(120, 62, 25))
            draw.point((bx + 1, by), fill=(242, 220, 174))
        elif t >= 18:
            draw.rectangle((rx + 5, ry - 31, rx + 8, ry - 30), fill=(120, 62, 25))

        base.alpha_composite(field, (0, 0))
        panel = ImageDraw.Draw(base)
        phase = "SNAP / RELEASE" if t < 9 else "BALL IN FLIGHT" if t < 18 else "CATCH / TURN UPFIELD"
        panel.rectangle((0, 160, 319, 179), fill=(5, 18, 31))
        panel.text((7, 165), "PLAYBACK PROTOTYPE", font=font, fill=(36, 216, 230))
        panel.text((196, 165), phase, font=font, fill=(242, 190, 55))
        scaled = base.resize((960, 540), Image.Resampling.NEAREST)
        frames.append(scaled.convert("P", palette=Image.Palette.ADAPTIVE, colors=128))

    frames[0].save(
        GIF_OUT,
        save_all=True,
        append_images=frames[1:],
        duration=90,
        loop=0,
        disposal=2,
        optimize=False,
    )
    storyboard = Image.new("RGB", (480 * 2, 270 * 2), (5, 18, 31))
    for slot, source_index in enumerate((0, 9, 18, 27)):
        still = frames[source_index].convert("RGB").resize((480, 270), Image.Resampling.NEAREST)
        storyboard.paste(still, ((slot % 2) * 480, (slot // 2) * 270))
    storyboard.save(STORY_OUT)


if __name__ == "__main__":
    offense_cycle, defense_cycle = build_cycles()
    make_demo(offense_cycle, defense_cycle)
    print(SHEET_OUT)
    print(GIF_OUT)
    print(STORY_OUT)
