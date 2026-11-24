"""
背景画像の候補（学校モチーフ）を作る。採用する案が決まるまで、Assets には入れない。

  A: ノート（横罫と赤い余白線）
  B: 漢字練習帳（薄い緑の方眼と、田の字のあき）
  C: 原稿用紙（オレンジ色のマス目）
  D: テスト用紙（外枠、赤ペンの丸、とじ穴）
  E: 方眼ノート（薄い水色の5mm方眼）
  F: 学習プリント（上下の色の帯と、点線の罫）
  G: 色画用紙（淡い黄色の厚い紙と、折り紙の角）
  H: 教科書（二重の細枠と、下のページ番号の欄）
  I: 画板（木の板に金具のクリップで留めた紙）
  J: 国語ノート（ピンクの罫と、間の点線）
  K: ドット方眼（薄いグレーの点）
  L: 給食ナプキン（淡い青のギンガムチェック）
  M: 色画用紙・水色
  N: 色画用紙・ミント
  O: ごほうびシール帳（淡い星と花丸）
  P: ホワイトボード（アルミの枠とペン置き）

製品版でも使う自作の画像（コードで描く）。Pillow と NumPy を使う。
文字は濃いグレー（#2B2B2B）で重なるので、線は薄くして、字が読みにくくならないようにする。

    python Tools/generate_school_background_candidates.py [出力先]

出力先を省くと Tools/out/ に書く（.gitignore の対象外にしないこと。採用したら generate_placeholder_ui.py の school_print に移す）。
"""

import math
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


def graph_note():
    """E: 方眼ノート。薄い水色の細かい方眼。5マスごとに少し濃い線"""
    rng = np.random.default_rng(15)
    page = paper(rng, (251, 251, 246))
    draw = ImageDraw.Draw(page)
    cell = 25
    for i, x in enumerate(range(0, WIDTH, cell)):
        draw.line((x, 0, x, HEIGHT), fill=(198, 220, 234) if i % 5 else (170, 200, 222), width=1 if i % 5 else 2)
    for i, y in enumerate(range(0, HEIGHT, cell)):
        draw.line((0, y, WIDTH, y), fill=(198, 220, 234) if i % 5 else (170, 200, 222), width=1 if i % 5 else 2)
    return page


def worksheet():
    """F: 学習プリント。上下の縁に色の帯、中は点線の薄い罫"""
    rng = np.random.default_rng(16)
    page = paper(rng, (252, 251, 247))
    draw = ImageDraw.Draw(page)
    colors = [(240, 150, 120), (245, 200, 100), (140, 195, 150), (120, 170, 220)]
    seg = WIDTH // len(colors)
    for i, c in enumerate(colors):
        draw.rectangle((i * seg, 0, (i + 1) * seg, 12), fill=c)
        draw.rectangle((i * seg, HEIGHT - 12, (i + 1) * seg, HEIGHT), fill=c)
    for y in range(160, HEIGHT - 40, 64):
        for x in range(60, WIDTH - 60, 14):
            draw.line((x, y, x + 6, y), fill=(206, 208, 212), width=1)
    return page


def color_paper():
    """G: 色画用紙。淡い黄色のざらざらした厚い紙。四隅に折り紙のような三角"""
    rng = np.random.default_rng(17)
    page = paper(rng, (252, 241, 205), mottle=5.0, grain=9.0)
    draw = ImageDraw.Draw(page)
    size = 90
    corners = [
        ((0, 0), (size, 0), (0, size), (240, 160, 140)),
        ((WIDTH, 0), (WIDTH - size, 0), (WIDTH, size), (140, 195, 215)),
        ((0, HEIGHT), (size, HEIGHT), (0, HEIGHT - size), (150, 200, 150)),
        ((WIDTH, HEIGHT), (WIDTH - size, HEIGHT), (WIDTH, HEIGHT - size), (235, 190, 110)),
    ]
    for a, b, c, color in corners:
        draw.polygon((a, b, c), fill=color)
    return page


def textbook():
    """H: 教科書。二重の細枠と、下中央のページ番号の欄。淡いクリーム色"""
    rng = np.random.default_rng(18)
    page = paper(rng, (250, 247, 236))
    draw = ImageDraw.Draw(page)
    ink = (176, 168, 150)
    draw.rectangle((24, 24, WIDTH - 24, HEIGHT - 24), outline=ink, width=3)
    draw.rectangle((34, 34, WIDTH - 34, HEIGHT - 34), outline=ink, width=1)
    # 下中央のページ番号の欄（細い線で囲む。文字は入れない）
    draw.rounded_rectangle((WIDTH // 2 - 40, HEIGHT - 70, WIDTH // 2 + 40, HEIGHT - 40), 6, outline=ink, width=1)
    return page


def clipboard():
    """I: 画板。細い木の縁に囲まれた紙と、上中央の金具のクリップ"""
    rng = np.random.default_rng(19)
    board = paper(rng, (186, 140, 90), mottle=10.0, grain=12.0)
    draw = ImageDraw.Draw(board)
    for _ in range(60):
        y = rng.random() * HEIGHT
        draw.line((0, y, WIDTH, y + (rng.random() - 0.5) * 10), fill=(165, 120, 74), width=1)
    inset = 16
    sheet = paper(rng, (251, 250, 245), mottle=2.0, grain=4.0).crop((0, 0, WIDTH - inset * 2, HEIGHT - inset * 2))
    board.paste(sheet, (inset, inset))
    draw = ImageDraw.Draw(board)
    draw.rectangle((inset, inset, WIDTH - inset, HEIGHT - inset), outline=(140, 100, 60), width=2)
    # クリップ（上中央）
    cx = WIDTH // 2
    draw.rounded_rectangle((cx - 90, 0, cx + 90, 38), 8, fill=(170, 172, 178), outline=(120, 122, 128), width=2)
    draw.rounded_rectangle((cx - 60, 10, cx + 60, 26), 5, fill=(205, 207, 212))
    return board


def kokugo_note():
    """J: 国語ノート。ピンクの罫を2本ずつ、その間に点線（字の高さをそろえる補助線）"""
    rng = np.random.default_rng(20)
    page = paper(rng, (252, 250, 245))
    draw = ImageDraw.Draw(page)
    pitch = 76
    for y in range(160, HEIGHT - 30, pitch):
        draw.line((0, y, WIDTH, y), fill=(244, 208, 212), width=2)
        draw.line((0, y + 44, WIDTH, y + 44), fill=(244, 208, 212), width=2)
        for x in range(0, WIDTH, 16):
            draw.line((x, y + 22, x + 8, y + 22), fill=(248, 226, 228), width=1)
    return page


def dot_grid():
    """K: ドット方眼。薄いグレーの点を等間隔に並べる"""
    rng = np.random.default_rng(21)
    page = paper(rng, (252, 252, 249))
    draw = ImageDraw.Draw(page)
    for y in range(25, HEIGHT, 50):
        for x in range(25, WIDTH, 50):
            draw.ellipse((x - 2, y - 2, x + 2, y + 2), fill=(196, 198, 204))
    return page


def gingham():
    """L: 給食ナプキン。淡い青のギンガムチェック"""
    rng = np.random.default_rng(22)
    base = np.asarray(paper(rng, (253, 253, 251))).astype(np.float32)
    period, band = 80, 40
    xs = (np.arange(WIDTH) % period) < band
    ys = (np.arange(HEIGHT) % period) < band
    tint = np.zeros((HEIGHT, WIDTH), dtype=np.float32)
    tint += ys[:, None] * 1.0
    tint += xs[None, :] * 1.0
    # 縦だけ・横だけの帯は薄く、重なる所は少し濃く
    strength = np.array([14.0, 8.0, 0.0], dtype=np.float32)
    base -= (tint[..., None] * strength[None, None, :]) * 0.6
    return Image.fromarray(np.clip(base, 0, 255).astype(np.uint8), "RGB")


def pastel_paper(base, corners, seed):
    rng = np.random.default_rng(seed)
    page = paper(rng, base, mottle=5.0, grain=9.0)
    draw = ImageDraw.Draw(page)
    size = 90
    pts = [
        ((0, 0), (size, 0), (0, size)),
        ((WIDTH, 0), (WIDTH - size, 0), (WIDTH, size)),
        ((0, HEIGHT), (size, HEIGHT), (0, HEIGHT - size)),
        ((WIDTH, HEIGHT), (WIDTH - size, HEIGHT), (WIDTH, HEIGHT - size)),
    ]
    for (a, b, c), color in zip(pts, corners):
        draw.polygon((a, b, c), fill=color)
    return page


def color_paper_blue():
    """M: 色画用紙・水色"""
    return pastel_paper((226, 240, 248), [(250, 200, 190), (250, 226, 150), (170, 214, 170), (200, 190, 235)], 23)


def color_paper_mint():
    """N: 色画用紙・ミント"""
    return pastel_paper((226, 244, 232), [(250, 205, 195), (248, 228, 160), (170, 205, 235), (232, 190, 215)], 24)


def sticker_book():
    """O: ごほうびシール帳。淡い星と花丸のうすい模様を散らす（文字の邪魔にならない淡さ）"""
    rng = np.random.default_rng(25)
    page = paper(rng, (253, 250, 243))
    draw = ImageDraw.Draw(page)
    colors = [(251, 235, 226), (253, 244, 214), (232, 244, 236), (228, 238, 250), (249, 234, 244)]
    for _ in range(46):
        cx, cy = rng.random() * WIDTH, rng.random() * HEIGHT
        # 上の題名・進捗のバーと、下のボタンの列には置かない（文字と重ならないように）
        if cy < 140 or cy > 760:
            continue
        r = 14 + rng.random() * 18
        color = colors[int(rng.random() * len(colors))]
        if rng.random() < 0.6:
            pts = []
            for k in range(10):
                ang = -math.pi / 2 + k * math.pi / 5
                rad = r if k % 2 == 0 else r * 0.45
                pts.append((cx + math.cos(ang) * rad, cy + math.sin(ang) * rad))
            draw.polygon(pts, fill=color)
        else:
            for ring in (r, r * 0.66, r * 0.33):
                draw.ellipse((cx - ring, cy - ring, cx + ring, cy + ring), outline=color, width=3)
    return page


def whiteboard():
    """P: ホワイトボード。つるっとした白と、アルミの細い枠、下にペン置き"""
    rng = np.random.default_rng(26)
    page = paper(rng, (250, 251, 252), mottle=2.0, grain=2.0)
    draw = ImageDraw.Draw(page)
    draw.rectangle((0, 0, WIDTH - 1, HEIGHT - 1), outline=(176, 180, 186), width=14)
    draw.rectangle((14, 14, WIDTH - 15, HEIGHT - 15), outline=(214, 217, 222), width=2)
    # ペン置き（下中央。ボタンの列と重ならない細さ）
    cx = WIDTH // 2
    draw.rounded_rectangle((cx - 180, HEIGHT - 26, cx + 180, HEIGHT - 4), 6, fill=(190, 194, 200), outline=(150, 154, 160), width=1)
    return page


CANDIDATES = {
    "bg_candidate_a_note.png": note,
    "bg_candidate_b_practice.png": practice_book,
    "bg_candidate_c_manuscript.png": manuscript,
    "bg_candidate_d_test.png": test_sheet,
    "bg_candidate_e_graph.png": graph_note,
    "bg_candidate_f_worksheet.png": worksheet,
    "bg_candidate_g_colorpaper.png": color_paper,
    "bg_candidate_h_textbook.png": textbook,
    "bg_candidate_i_clipboard.png": clipboard,
    "bg_candidate_j_kokugo.png": kokugo_note,
    "bg_candidate_k_dot.png": dot_grid,
    "bg_candidate_l_gingham.png": gingham,
    "bg_candidate_m_bluepaper.png": color_paper_blue,
    "bg_candidate_n_mintpaper.png": color_paper_mint,
    "bg_candidate_o_sticker.png": sticker_book,
    "bg_candidate_p_whiteboard.png": whiteboard,
}

if __name__ == "__main__":
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent / "out"
    out.mkdir(parents=True, exist_ok=True)
    for name, make in CANDIDATES.items():
        make().save(out / name)
        print(f"wrote {out / name}")
