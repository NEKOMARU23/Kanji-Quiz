"""
背景画像の候補（学校モチーフ）を作る。採用する案が決まるまで、Assets には入れない。

  A: ノート（横罫と赤い余白線）
  B: 漢字練習帳（薄い緑の方眼と、田の字のあき）
  C: 原稿用紙（オレンジ色のマス目）
  D: テスト用紙（外枠、赤ペンの丸、とじ穴）

製品版でも使う自作の画像（コードで描く）。Pillow と NumPy を使う。
文字は濃いグレー（#2B2B2B）で重なるので、線は薄くして、字が読みにくくならないようにする。

    python Tools/generate_school_background_candidates.py [出力先]

出力先を省くと Tools/out/ に書く（.gitignore の対象外にしないこと。採用したら generate_placeholder_ui.py の school_print に移す）。
"""

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

WIDTH, HEIGHT = 1600, 900


def paper(rng, base, mottle=3.0, grain=5.0):
    """紙の下地。ごく弱いむらと、細かいざらつき"""
    img = np.ones((HEIGHT, WIDTH, 3), dtype=np.float32) * np.array(base, dtype=np.float32)
    coarse = rng.random((HEIGHT // 90 + 2, WIDTH // 90 + 2)).astype(np.float32)
    coarse = np.asarray(Image.fromarray((coarse * 255).astype(np.uint8)).resize((WIDTH, HEIGHT), Image.BICUBIC)) / 255.0
    img += ((coarse - 0.5) * mottle)[..., None]
    img += ((rng.random((HEIGHT, WIDTH)) - 0.5) * grain)[..., None]
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB")


def note():
    """A: ノート。薄い水色の横罫、左に赤い余白線、ヘッダーの罫は少し濃い"""
    rng = np.random.default_rng(11)
    page = paper(rng, (251, 250, 244))
    draw = ImageDraw.Draw(page)
    step = 44
    top = 150  # 上の題名・進捗のバーには罫を引かない（文字と重なるため）
    for y in range(top, HEIGHT - 40, step):
        draw.line((0, y, WIDTH, y), fill=(190, 214, 230), width=2)
    # 余白線（二重）。題名（x=50〜）や左下の説明文より左に寄せる
    draw.line((34, 0, 34, HEIGHT), fill=(228, 130, 130), width=3)
    draw.line((41, 0, 41, HEIGHT), fill=(228, 130, 130), width=1)
    # とじ穴の跡は、余白線のさらに左（画面の端）
    for y in (HEIGHT // 4, HEIGHT // 2, HEIGHT * 3 // 4):
        draw.ellipse((-14, y - 16, 14, y + 16), fill=(208, 204, 196), outline=(190, 186, 178))
    return page


def practice_book():
    """B: 漢字練習帳。薄い緑の方眼。大きなマスの中に十字の補助線"""
    rng = np.random.default_rng(12)
    page = paper(rng, (252, 251, 245))
    draw = ImageDraw.Draw(page)
    cell = 100
    ox, oy = 50, 50
    line = (190, 218, 190)
    guide = (214, 232, 214)
    for x in range(ox, WIDTH, cell):
        draw.line((x, 0, x, HEIGHT), fill=line, width=2)
    for y in range(oy, HEIGHT, cell):
        draw.line((0, y, WIDTH, y), fill=line, width=2)
    # 補助線（点線の十字）
    for x in range(ox, WIDTH, cell):
        for y in range(oy, HEIGHT, cell):
            cx, cy = x + cell // 2, y + cell // 2
            for t in range(-cell // 2 + 6, cell // 2 - 4, 10):
                draw.line((cx + t, cy, cx + t + 4, cy), fill=guide, width=1)
                draw.line((cx, cy + t, cx, cy + t + 4), fill=guide, width=1)
    return page


def manuscript():
    """C: 原稿用紙。オレンジ色の枠と、中央の綴じ目（魚尾）"""
    rng = np.random.default_rng(13)
    page = paper(rng, (252, 250, 243))
    draw = ImageDraw.Draw(page)
    color = (232, 170, 120)
    cell = 60
    cols = (WIDTH - 200) // cell
    rows = (HEIGHT - 120) // cell
    left = (WIDTH - cols * cell) // 2
    top = (HEIGHT - rows * cell) // 2
    for c in range(cols + 1):
        draw.line((left + c * cell, top, left + c * cell, top + rows * cell), fill=color, width=2)
    for r in range(rows + 1):
        draw.line((left, top + r * cell, left + cols * cell, top + r * cell), fill=color, width=2)
    # 外枠は太く
    draw.rectangle((left - 3, top - 3, left + cols * cell + 3, top + rows * cell + 3), outline=color, width=3)
    # 中央の綴じ目
    mid = WIDTH // 2
    draw.line((mid, 0, mid, top), fill=color, width=2)
    draw.line((mid, top + rows * cell, mid, HEIGHT), fill=color, width=2)
    return page


def test_sheet():
    """D: テスト用紙。外枠、右上の赤ペンの丸、左にとじ穴、薄い罫線"""
    rng = np.random.default_rng(14)
    page = paper(rng, (249, 249, 247), mottle=2.0, grain=4.0)
    draw = ImageDraw.Draw(page)
    ink = (120, 120, 126)
    # 外枠
    draw.rectangle((28, 28, WIDTH - 28, HEIGHT - 28), outline=ink, width=2)
    # 角の採点の丸（赤ペン）。題名やボタンと重ならない右上の隅に置く
    draw.ellipse((WIDTH - 96, 44, WIDTH - 52, 92), outline=(236, 170, 170), width=2)
    # 薄い補助罫（解答欄のイメージ）。上のバーの下から
    for y in range(150, HEIGHT - 50, 70):
        draw.line((60, y, WIDTH - 60, y), fill=(230, 232, 236), width=1)
    # とじ穴
    for y in (HEIGHT // 3, HEIGHT * 2 // 3):
        draw.ellipse((6, y - 12, 30, y + 12), fill=(200, 200, 198), outline=(180, 180, 178))
    return page


CANDIDATES = {
    "bg_candidate_a_note.png": note,
    "bg_candidate_b_practice.png": practice_book,
    "bg_candidate_c_manuscript.png": manuscript,
    "bg_candidate_d_test.png": test_sheet,
}

if __name__ == "__main__":
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent / "out"
    out.mkdir(parents=True, exist_ok=True)
    for name, make in CANDIDATES.items():
        make().save(out / name)
        print(f"wrote {out / name}")
