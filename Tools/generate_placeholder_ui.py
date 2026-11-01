"""
画像（ハンコの○と×、級のハンコの枠、花丸、プリントの背景、机、プリントの影、黒板のチョークの丸と級の枠、掲示板のコルク、漢字帳のマス）を作る。

製品版でも使う自作の画像（ほかの人の素材を使わず、コードで描く）。Pillow と NumPy を使う。
出力先: Assets/TechC/KanjiQuiz/Art/UI/

    python Tools/generate_placeholder_ui.py
"""

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUTPUT_DIR = Path(__file__).resolve().parent.parent / "Assets" / "TechC" / "KanjiQuiz" / "Art" / "UI"

# 朱肉の色（ScreenDesign.md の強調色 #C84B31 より少し赤い）
STAMP_COLOR = (204, 45, 38)
# 不正解のハンコの色（青いインク）
WRONG_COLOR = (45, 80, 190)
# 花丸の色（赤ペン）
PEN_COLOR = (222, 40, 40)

# 線を滑らかにするため、大きく描いてから縮める
SUPERSAMPLE = 4


def grain_alpha(alpha, rng, amount):
    """ハンコのかすれ。ところどころ色が抜けたようにする"""
    size = alpha.shape
    noise = rng.random(size)
    # 細かい点のかすれと、少し大きなむら
    coarse = Image.fromarray((rng.random((size[0] // 16, size[1] // 16)) * 255).astype(np.uint8))
    coarse = np.asarray(coarse.resize((size[1], size[0]), Image.BICUBIC)) / 255.0
    keep = (noise > amount * 0.6) & (coarse > amount * 0.5)
    return np.where(keep, alpha, alpha * 0.35)


def rough_edge(mask_image, rng, strength):
    """輪郭を少しでこぼこにする"""
    blurred = mask_image.filter(ImageFilter.GaussianBlur(strength))
    arr = np.asarray(blurred).astype(np.float32) / 255.0
    noise = rng.random(arr.shape) * 0.35
    return np.clip((arr - 0.5 + noise - 0.17) * 6 + 0.5, 0, 1)


def to_rgba(alpha, color):
    h, w = alpha.shape
    rgba = np.zeros((h, w, 4), dtype=np.uint8)
    rgba[..., 0] = color[0]
    rgba[..., 1] = color[1]
    rgba[..., 2] = color[2]
    rgba[..., 3] = (np.clip(alpha, 0, 1) * 255).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def save(image, name, size):
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    path = OUTPUT_DIR / name
    image.resize((size, size), Image.LANCZOS).save(path)
    print(f"wrote {path}")


def stamp_circle(size=256):
    """正解のハンコ（朱色の○）"""
    rng = np.random.default_rng(1)
    s = size * SUPERSAMPLE
    mask = Image.new("L", (s, s), 0)
    draw = ImageDraw.Draw(mask)
    margin = s * 0.06
    width = int(s * 0.11)
    draw.ellipse((margin, margin, s - margin, s - margin), outline=255, width=width)
    alpha = rough_edge(mask, rng, s * 0.004)
    alpha = grain_alpha(alpha, rng, 0.45)
    save(to_rgba(alpha, STAMP_COLOR), "icon_stamp_correct.png", size)


def stamp_cross(size=256):
    """不正解のハンコ（青い×）"""
    rng = np.random.default_rng(3)
    s = size * SUPERSAMPLE
    mask = Image.new("L", (s, s), 0)
    draw = ImageDraw.Draw(mask)
    margin = s * 0.16
    width = int(s * 0.12)
    draw.line((margin, margin, s - margin, s - margin), fill=255, width=width)
    draw.line((s - margin, margin, margin, s - margin), fill=255, width=width)
    alpha = rough_edge(mask, rng, s * 0.004)
    alpha = grain_alpha(alpha, rng, 0.45)
    save(to_rgba(alpha, WRONG_COLOR), "icon_stamp_incorrect.png", size)


def stamp_frame(size=256):
    """級のハンコの枠（朱色の角の丸い四角）。中の文字は TextMeshPro で書く"""
    rng = np.random.default_rng(2)
    s = size * SUPERSAMPLE
    mask = Image.new("L", (s, s), 0)
    draw = ImageDraw.Draw(mask)
    margin = s * 0.05
    draw.rounded_rectangle((margin, margin, s - margin, s - margin), radius=s * 0.12, outline=255, width=int(s * 0.07))
    inner = s * 0.15
    draw.rounded_rectangle((inner, inner, s - inner, s - inner), radius=s * 0.06, outline=255, width=int(s * 0.015))
    alpha = rough_edge(mask, rng, s * 0.003)
    alpha = grain_alpha(alpha, rng, 0.4)
    save(to_rgba(alpha, STAMP_COLOR), "frame_stamp_rank.png", size)


def hanamaru(size=512):
    """全問正解の花丸（真ん中のうず巻きと、まわりの花びら）"""
    s = size * SUPERSAMPLE
    image = Image.new("L", (s, s), 0)
    draw = ImageDraw.Draw(image)
    center = s / 2

    points = []
    # 真ん中のうず巻き（2周半）
    turns = 2.5
    for i in range(400):
        t = i / 399
        angle = -math.pi / 2 + t * turns * 2 * math.pi
        r = s * (0.03 + 0.2 * t)
        points.append((center + r * math.cos(angle), center + r * math.sin(angle)))
    # まわりの花びら（外側に丸くふくらむ 7 枚。くびれのところで少し内側に入る）
    petals = 7
    base = s * 0.3
    bulge = s * 0.13
    start_angle = -math.pi / 2 + turns * 2 * math.pi
    for i in range(1400):
        t = i / 1399
        angle = start_angle + t * 2 * math.pi * 1.04
        r = base + bulge * abs(math.sin(t * petals * math.pi))
        points.append((center + r * math.cos(angle), center + r * math.sin(angle)))

    # ペンの線（太さを少し揺らす）
    for i, (x, y) in enumerate(points):
        width = s * (0.022 + 0.004 * math.sin(i * 0.05))
        draw.ellipse((x - width, y - width, x + width, y + width), fill=255)

    alpha = np.asarray(image).astype(np.float32) / 255.0
    save(to_rgba(alpha, PEN_COLOR), "icon_hanamaru.png", size)


def school_print(width=1600, height=900):
    """背景（学校で配られるプリント。少しくすんだ再生紙に、繊維の粒と印刷の外枠）"""
    rng = np.random.default_rng(4)
    img = np.ones((height, width, 3), dtype=np.float32) * np.array([246, 245, 239], dtype=np.float32)
    # 紙のむら（ごく弱く）と、細かいざらつき
    coarse = rng.random((height // 80 + 2, width // 80 + 2)).astype(np.float32)
    coarse = np.asarray(Image.fromarray((coarse * 255).astype(np.uint8)).resize((width, height), Image.BICUBIC)) / 255.0
    img += ((coarse - 0.5) * 3)[..., None]
    img += ((rng.random((height, width)) - 0.5) * 5)[..., None]

    page = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")
    draw = ImageDraw.Draw(page)
    # 再生紙に混じる繊維のかけら（小さな点と短い線）
    for _ in range(650):
        x, y = rng.random() * width, rng.random() * height
        shade = int(185 + rng.random() * 45)
        if rng.random() < 0.7:
            r = 0.6 + rng.random() * 0.9
            draw.ellipse((x - r, y - r, x + r, y + r), fill=(shade, shade - 6, shade - 14))
        else:
            angle = rng.random() * math.pi
            length = 3 + rng.random() * 7
            draw.line((x, y, x + math.cos(angle) * length, y + math.sin(angle) * length), fill=(shade, shade - 6, shade - 14), width=1)
    # 印刷の外枠（プリントの周りの線）
    margin = 18
    draw.rectangle((margin, margin, width - margin, height - margin), outline=(120, 120, 120), width=2)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    path = OUTPUT_DIR / "bg_print.png"
    page.save(path)
    print(f"wrote {path}")


def desk(width=1600, height=900):
    """シーンを切り替えるときに見える机（木目）"""
    rng = np.random.default_rng(7)
    page = Image.new("RGB", (width, height), (185, 138, 87))
    draw = ImageDraw.Draw(page)
    for _ in range(45):
        y0 = rng.random() * height
        amp = 6 + rng.random() * 20
        freq = 0.002 + rng.random() * 0.004
        phase = rng.random() * 6
        points = [(x, y0 + math.sin(x * freq + phase) * amp) for x in range(0, width + 16, 16)]
        tone = int(rng.random() * 20)
        draw.line(points, fill=(150 - tone, 104 - tone, 62 - tone // 2), width=int(2 + rng.random() * 4))
    arr = np.asarray(page.filter(ImageFilter.GaussianBlur(2.2))).astype(np.float32)
    rows = np.asarray(Image.fromarray((rng.random((height // 50 + 2, 1)) * 255).astype(np.uint8)).resize((width, height), Image.BICUBIC)).astype(np.float32) / 255.0
    arr += ((rows - 0.5) * 18)[..., None]
    arr += ((rng.random((height, width)) - 0.5) * 10)[..., None]
    # 天板の継ぎ目
    seam = int(height * 0.62)
    arr[seam:seam + 3] *= 0.7

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    path = OUTPUT_DIR / "bg_desk.png"
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB").save(path)
    print(f"wrote {path}")


def sheet_shadow(size=128, border=48):
    """プリントの影（9スライスで伸ばす。外側の border px がぼかした部分）"""
    s = size * SUPERSAMPLE
    mask = Image.new("L", (s, s), 0)
    draw = ImageDraw.Draw(mask)
    inset = border * SUPERSAMPLE * 0.55
    draw.rectangle((inset, inset, s - inset, s - inset), fill=255)
    alpha = np.asarray(mask.filter(ImageFilter.GaussianBlur(border * SUPERSAMPLE * 0.28))).astype(np.float32) / 255.0
    save(to_rgba(alpha * 0.55, (0, 0, 0)), "fx_sheet_shadow.png", size)


def chalk_circle(width=512, height=256):
    """黒板で選んだものを囲むチョークの丸（白。Image の色で黄色にする）"""
    rng = np.random.default_rng(5)
    w, h = width * SUPERSAMPLE, height * SUPERSAMPLE
    mask = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(mask)
    line = int(h * 0.05)
    # 少しずらして2周なぞったように描く
    draw.ellipse((w * 0.03, h * 0.08, w * 0.97, h * 0.92), outline=255, width=line)
    draw.arc((w * 0.05, h * 0.04, w * 0.99, h * 0.88), start=200, end=330, fill=255, width=int(line * 0.8))
    alpha = rough_edge(mask, rng, h * 0.004)
    alpha = grain_alpha(alpha, rng, 0.55)
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    path = OUTPUT_DIR / "fx_chalk_circle.png"
    to_rgba(alpha, (255, 255, 255)).resize((width, height), Image.LANCZOS).save(path)
    print(f"wrote {path}")


def chalk_stamp_frame(size=256):
    """黒板に書く級の枠（白。Image の色でピンクのチョークにする）"""
    rng = np.random.default_rng(6)
    s = size * SUPERSAMPLE
    mask = Image.new("L", (s, s), 0)
    draw = ImageDraw.Draw(mask)
    margin = s * 0.06
    draw.rounded_rectangle((margin, margin, s - margin, s - margin), radius=s * 0.1, outline=255, width=int(s * 0.06))
    alpha = rough_edge(mask, rng, s * 0.004)
    alpha = grain_alpha(alpha, rng, 0.55)
    save(to_rgba(alpha, (255, 255, 255)), "frame_chalk_stamp.png", size)


def cork_board(width=1600, height=880):
    """クレジットの掲示板のコルク（細かい粒と、ところどころの濃い点）"""
    rng = np.random.default_rng(8)
    arr = np.empty((height, width, 3), np.float32)
    arr[...] = (181, 138, 90)
    # 粒の濃淡（細かいものと、少し大きなまだら）
    fine = (rng.random((height, width)) - 0.5) * 34
    blotch = np.asarray(Image.fromarray((rng.random((height // 24 + 2, width // 24 + 2)) * 255).astype(np.uint8))
                        .resize((width, height), Image.BICUBIC)).astype(np.float32) / 255.0
    arr += (fine + (blotch - 0.5) * 26)[..., None]
    # 濃い粒
    dots = Image.new("L", (width, height), 0)
    draw = ImageDraw.Draw(dots)
    for _ in range(width * height // 260):
        x, y = rng.random() * width, rng.random() * height
        r = 0.6 + rng.random() * 1.4
        draw.ellipse((x - r, y - r, x + r, y + r), fill=int(120 + rng.random() * 135))
    dark = np.asarray(dots.filter(ImageFilter.GaussianBlur(0.6))).astype(np.float32) / 255.0
    arr *= (1.0 - dark * 0.45)[..., None]

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    path = OUTPUT_DIR / "bg_cork.png"
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB").save(path)
    print(f"wrote {path}")


def masu_cell(size=128):
    """漢字帳のマス（白。Image の色で線の色にする）。外の枠と、真ん中の薄い点線の十字"""
    s = size * SUPERSAMPLE
    alpha = np.zeros((s, s), np.float32)
    line = int(s * 0.022)
    alpha[:line, :] = alpha[-line:, :] = alpha[:, :line] = alpha[:, -line:] = 1.0
    dash = s // 16
    thin = max(1, int(s * 0.012))
    mid = s // 2
    for start in range(0, s, dash * 2):
        alpha[mid - thin:mid + thin, start:start + dash] = np.maximum(alpha[mid - thin:mid + thin, start:start + dash], 0.35)
        alpha[start:start + dash, mid - thin:mid + thin] = np.maximum(alpha[start:start + dash, mid - thin:mid + thin], 0.35)
    save(to_rgba(alpha, (255, 255, 255)), "frame_masu.png", size)


def select_frame(size=128):
    """漢字帳で選んだマスを囲む太い枠（白。Image の色で朱色にする）"""
    s = size * SUPERSAMPLE
    alpha = np.zeros((s, s), np.float32)
    line = int(s * 0.07)
    alpha[:line, :] = alpha[-line:, :] = alpha[:, :line] = alpha[:, -line:] = 1.0
    save(to_rgba(alpha, (255, 255, 255)), "frame_select.png", size)


if __name__ == "__main__":
    stamp_circle()
    stamp_cross()
    stamp_frame()
    hanamaru()
    school_print()
    desk()
    sheet_shadow()
    chalk_circle()
    chalk_stamp_frame()
    cork_board()
    masu_cell()
    select_frame()
