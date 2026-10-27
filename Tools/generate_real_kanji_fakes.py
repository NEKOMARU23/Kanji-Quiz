"""
本物の漢字さがしの問題データ（本物の漢字と偽物の候補）の JSON を作るスクリプト。

使い方:
    python -I Tools/generate_real_kanji_fakes.py <IDS.TXT> <kanjidic2.xml.gz> <KanjiVG のフォルダ> <漢字セットのフォルダ> <出力フォルダ>

例:
    python -I Tools/generate_real_kanji_fakes.py IDS.TXT kanjidic2.xml.gz Assets/StreamingAssets/KanjiVG Assets/TechC/KanjiQuiz/Data/KanjiSets Assets/TechC/KanjiQuiz/Data/RealKanji

入手先:
    IDS.TXT          https://babelstone.co.uk/CJK/IDS.TXT（BabelStone IDS、Andrew West。許可もクレジットもなしで使ってよい）
    kanjidic2.xml.gz https://www.edrdg.org/kanjidic/kanjidic2.xml.gz（EDRDG、CC BY-SA 4.0）

偽物の作り方（Docs/GameSpec.md 2.1）:
    本物の漢字の部首（KanjiVG の kvg:radical="general"、なければ "tradit"）を同じ位置に残し、部首以外の部分を、
    出題範囲（その難易度と、それより下の難易度）にある漢字の部品か、漢字1字に入れ替える（donor_parts）。
    BabelStone IDS で Unicode の漢字すべての部品の組み合わせと照らし合わせ、実在しないものだけを偽物にする。
    偽物の読みは、借りた部品の音読みにする。

部品を置く枠（place_part）:
    本物の部首以外の部分の外枠を基本にする。ただし外枠は部首の線と重なる場所まで広がっていることがある
    （楯の「盾」の左はらいが木へんの下まで伸びているなど）。本物は形でよけているが、借りた部品を外枠いっぱいに
    広げると部首とぶつかる。そこで、部首の側から枠を少しずつ狭め、部首と部品の線の重なりが本物と同じくらいに
    なる枠を探す。どこまで狭めても重なる候補は使わない。

JSON に入れるのは、部首のグループの id と、部品を借りる漢字・グループの id と、部品を置く枠（box）だけ。
座標はゲームが SVG から組み立て、借りた部分を box に合わせて縮める・伸ばす。
groups が空のときは、部品を借りる漢字の画をすべて使う（漢字1字をまるごと借りる）。
差分を見やすくするため、1問を1行に書く。

必要なパッケージ: Pillow、numpy（線の重なりを調べるのに使う）
"""
import gzip
import json
import os
import random
import re
import sys
import unicodedata
import xml.etree.ElementTree as ET

import numpy
from PIL import Image, ImageDraw

KVG = "{http://kanjivg.tagaini.net}"
SVG = "{http://www.w3.org/2000/svg}"

SOURCES = [
    "KanjiVG (Ulrich Apel, CC BY-SA 3.0)",
    "KANJIDIC2 (Electronic Dictionary Research and Development Group, CC BY-SA 4.0)",
    "BabelStone IDS (Andrew West)",
]

# 難易度ごとの漢字セット。部品はその難易度と、それより下の難易度の漢字から借りる
LEVELS = ["Elementary", "Joyo", "Hyogai"]

# 1つの漢字について残す偽物の候補の数（ゲームでは、この中から毎回3つを選ぶ）
MAX_FAKES = 12
MIN_FAKES = 3

# 借りた部分と元の部分の縦横比の違いの上限。これを超えると、縮める・伸ばすと字形が崩れる
MAX_ASPECT_RATIO = 1.5

# ---- 部品を置く枠（place_part） ----
# 線の太さ（KanjiVG の座標で）。ゲームの KanjiGlyphRenderer の太さ 0.2（字の1辺 3）に合わせる
STROKE_WIDTH = 0.2 / 3 * 109
# 外枠の幅・高さの下限（KanjiVG の座標で）。ゲームの FakeKanjiComposer.MinExtent（0.3）に合わせる
MIN_EXTENT = 0.3 / 3 * 109
# 借りた部品の線が、部首の線（本物の部品が触れているところを除く）とこれ以上重なれば「ぶつかっている」とする
# （面積。KanjiVG の座標で）。本物の部品も部首に重なっている字（省の「少」など）があるので、その場所は除いて比べる
OVERLAP_TOLERANCE = 8.0
# 部首以外の部分の外枠の幅・高さの下限（KanjiVG の座標で）。これより小さいと、借りた部品がつぶれて読めない（太の点など）
MIN_REST_SIZE = 20.0
# 部品を置く枠の、1画あたりの面積の下限（KanjiVG の座標で）。小さな枠に画数の多い部品を入れると線が詰まって読めない
# （加の「口」の大きさ、32×43 なら5画まで）
MIN_AREA_PER_STROKE = 250.0
# 枠を狭める1回の量と上限（枠の幅・高さに対する割合）
SHRINK_STEP = 0.04
MAX_SHRINK = 0.32
# 部首の位置ごとに、狭める辺（l: 左、r: 右、t: 上、b: 下）。部首のある側の辺を動かす
SHRINK_SIDES = {
    "left": "l",
    "right": "r",
    "top": "t",
    "bottom": "b",
    "nyo": "lb",
    "tare": "lt",
    "kamae": "lrtb",
}

# 部首の位置と、IDS での組み立て方。囲い・垂れ・繞は IDS の書き方が字によって違うのでまとめて扱う
LAYOUTS = {
    "left": ("⿰", True),
    "right": ("⿰", False),
    "top": ("⿱", True),
    "bottom": ("⿱", False),
    "kamae": ("enclose", True),
    "tare": ("enclose", True),
    "nyo": ("enclose", True),
}

IDC_ARITY = {c: 2 for c in "⿰⿱⿴⿵⿶⿷⿸⿹⿺⿻⿼⿽㇯"}
IDC_ARITY.update({"⿲": 3, "⿳": 3, "⿾": 1, "⿿": 1})
ENCLOSE_IDCS = set("⿴⿵⿶⿷⿸⿹⿺⿻⿼⿽㇯")

# 照らし合わせるときに同じ部品とみなす異体字。広めにそろえるほど、実在する漢字を偽物にしにくくなる
VARIANTS = {
    "氵": "水", "扌": "手", "忄": "心", "㣺": "心", "亻": "人", "礻": "示", "衤": "衣",
    "犭": "犬", "灬": "火", "刂": "刀", "⺼": "月", "肉": "月", "艹": "艸", "⺾": "艸",
    "辶": "辵", "⻌": "辵", "⻍": "辵", "⻎": "辵", "每": "毎", "⺮": "竹", "𥫗": "竹",
    "罒": "网", "⺲": "网", "飠": "食", "𩙿": "食", "釒": "金", "糹": "糸", "訁": "言",
    "⻖": "阜", "⻏": "邑", "王": "玉", "𤣩": "玉", "⺩": "玉", "⺗": "心", "⺌": "小",
    "⺍": "小", "⺊": "卜", "⻊": "足", "𧾷": "足", "爫": "爪", "⺧": "牛", "牜": "牛",
    "⺶": "羊", "⺷": "羊", "𦍌": "羊", "⺻": "聿", "⺺": "聿", "耂": "老", "⺹": "老",
    "⻗": "雨", "⺪": "疋", "𤴔": "疋", "⺫": "目", "⻞": "長", "镸": "長", "⺁": "厂",
    "⺅": "人", "⺉": "刀", "⺖": "心", "⺘": "手", "⺡": "水", "⺨": "犬", "⺭": "示",
    "⻂": "衣", "⻗": "雨", "⻟": "食", "⻠": "食", "⻤": "鬼", "⻨": "麦", "⻩": "黄",
    "⻭": "歯", "⻱": "亀", "⺝": "月", "⺜": "日", "⺦": "丬", "丬": "爿",
}


def normalize(ch):
    ch = unicodedata.normalize("NFKC", ch)
    seen = set()
    while ch in VARIANTS and ch not in seen:
        seen.add(ch)
        ch = VARIANTS[ch]
    return ch


# ---- IDS ----

def parse_ids_sequence(seq):
    """IDS の文字列を木（部品の文字、または (組み立て方, [子]) ）にする。読めなければ None。"""
    tokens = list(seq)
    pos = 0

    def read():
        nonlocal pos
        if pos >= len(tokens):
            raise ValueError
        token = tokens[pos]
        pos += 1
        if token in IDC_ARITY:
            return (token, [read() for _ in range(IDC_ARITY[token])])
        return token

    try:
        tree = read()
    except ValueError:
        return None
    return tree if pos == len(tokens) else None


def load_ids(path):
    ids = {}
    with open(path, encoding="utf-8-sig") as f:
        for line in f:
            if line.startswith("#") or not line.strip():
                continue
            fields = line.rstrip("\r\n").split("\t")
            if len(fields) < 3:
                continue
            trees = []
            for field in fields[2:]:
                m = re.match(r"\^(.*)\$", field)
                if not m:
                    continue
                tree = parse_ids_sequence(m.group(1))
                if tree is not None:
                    trees.append(tree)
            if trees:
                ids[fields[1]] = trees
    return ids


class IdsIndex:
    """部品の組み合わせから、実在する漢字を引けるようにしたもの。"""

    def __init__(self, ids):
        self.ids = ids
        self.expanded = {}
        self.keys = set()
        for ch, trees in ids.items():
            for tree in trees:
                for key in self.tree_keys(tree, ch):
                    self.keys.add(key)

    def expand(self, ch, depth=0):
        """部品をこれ以上分けられないところまで分けた文字列。"""
        if ch in self.expanded:
            return self.expanded[ch]
        trees = self.ids.get(ch)
        if depth > 12 or not trees or trees[0] == ch:
            result = normalize(ch)
        else:
            result = self.flatten(trees[0], depth + 1, expand=True)
        self.expanded[ch] = result
        return result

    def flatten(self, tree, depth=0, expand=False):
        if isinstance(tree, str):
            return self.expand(tree, depth) if expand else normalize(tree)
        idc, children = tree
        idc = "enclose" if idc in ENCLOSE_IDCS else idc
        return idc + "".join(self.flatten(c, depth, expand) for c in children)

    def operand_keys(self, tree):
        return {self.flatten(tree), self.flatten(tree, expand=True)}

    def tree_keys(self, tree, ch):
        if isinstance(tree, str):
            return set()
        idc, children = tree
        if len(children) != 2:
            return set()
        idc = "enclose" if idc in ENCLOSE_IDCS else idc
        first, second = children
        return {(idc, a, b) for a in self.operand_keys(first) for b in self.operand_keys(second)}

    def exists(self, idc, first_tree, second_tree):
        return any(
            (idc, a, b) in self.keys
            for a in self.operand_keys(first_tree)
            for b in self.operand_keys(second_tree)
        )

    def split(self, ch, idc, radical_first):
        """漢字の IDS を部首側と残りに分ける。組み立て方が違えば None。"""
        for tree in self.ids.get(ch, []):
            if isinstance(tree, str):
                continue
            tree_idc, children = tree
            tree_idc = "enclose" if tree_idc in ENCLOSE_IDCS else tree_idc
            if tree_idc != idc or len(children) != 2:
                continue
            return (children[0], children[1]) if radical_first else (children[1], children[0])
        return None


# ---- KanjiVG ----

PATH_TOKEN = re.compile(r"[MmLlHhVvCcSsZz]|-?(?:\d+\.?\d*|\.\d+)(?:e-?\d+)?")
PATH_ARGS = {"M": 2, "L": 2, "H": 1, "V": 1, "C": 6, "S": 4, "Z": 0}


def path_points(d):
    """パスの端点と制御点（外枠を求めるのに使う。制御点を含むので外枠は少し大きめになる）。"""
    points = []
    x = y = 0.0
    cmd = None
    tokens = PATH_TOKEN.findall(d)
    i = 0
    while i < len(tokens):
        if tokens[i].isalpha():
            cmd = tokens[i]
            i += 1
            if cmd in "Zz":
                continue
        upper = cmd.upper()
        n = PATH_ARGS[upper]
        args = [float(t) for t in tokens[i:i + n]]
        i += n
        rel = cmd.islower()
        if upper == "H":
            x = args[0] + (x if rel else 0)
            points.append((x, y))
        elif upper == "V":
            y = args[0] + (y if rel else 0)
            points.append((x, y))
        else:
            pts = [(args[j] + (x if rel else 0), args[j + 1] + (y if rel else 0)) for j in range(0, n, 2)]
            points.extend(pts)
            x, y = pts[-1]
            if upper == "M":
                cmd = "l" if rel else "L"
    return points


def bbox(paths):
    points = [p for path in paths for p in path_points(path.get("d"))]
    xs = [p[0] for p in points]
    ys = [p[1] for p in points]
    return min(xs), min(ys), max(xs), max(ys)


class Part:
    """漢字の一番上の階層の部品（グループ、またはグループに入っていない画）。"""

    def __init__(self, node):
        self.id = node.get("id")
        self.element = node.get(KVG + "element")
        self.position = node.get(KVG + "position")
        self.radical = node.get(KVG + "radical")
        self.split = node.get(KVG + "part") is not None
        self.paths = [node] if node.tag == SVG + "path" else list(node.iter(SVG + "path"))


class KanjiShape:
    def __init__(self, ch, path):
        root = ET.parse(path).getroot()
        code = f"{ord(ch):05x}"
        group = next(g for g in root.iter(SVG + "g") if g.get("id") == f"kvg:{code}")
        self.ch = ch
        self.parts = [Part(node) for node in group if node.tag in (SVG + "g", SVG + "path")]
        self.elements = {normalize(g.get(KVG + "element")) for g in group.iter(SVG + "g") if g.get(KVG + "element")}

    def layout(self, position):
        """position にある部品（書き順の都合で分かれていれば、そのすべて）と、残りの部品に分ける。分けられなければ None。"""
        found = [p for p in self.parts if p.position == position]
        if not found or len({p.element for p in found}) != 1 or (len(found) > 1) != all(p.split for p in found):
            return None
        rest = [p for p in self.parts if p not in found]
        if not rest or any(p.element and p.element == found[0].element for p in rest):
            return None
        return found, rest

    def radical_layout(self):
        """部首と残りの部品に分ける。部首は kvg:radical="general"、なければ "tradit"（康熙字典の部首）。"""
        for kind in ("general", "tradit"):
            radicals = [p for p in self.parts if p.radical == kind]
            if radicals:
                break
        else:
            return None
        if radicals[0].position not in LAYOUTS:
            return None
        result = self.layout(radicals[0].position)
        return result if result and result[0] == radicals else None


def rest_element(rest):
    return rest[0].element if len(rest) == 1 and rest[0].element else None


def stroke_count(parts):
    return sum(len(p.paths) for p in parts)


def sample_path(d):
    """パスを点の列にする。曲線はゲームの StrokePathParser と同じく 30 分割する"""
    tokens = PATH_TOKEN.findall(d)
    points = []
    x = y = 0.0
    start = (0.0, 0.0)
    last_control = None
    cmd = None
    i = 0

    def bezier(p0, p1, p2, p3):
        for k in range(1, 31):
            u = k / 30
            a, b, c, e = (1 - u) ** 3, 3 * (1 - u) ** 2 * u, 3 * (1 - u) * u * u, u ** 3
            points.append((a * p0[0] + b * p1[0] + c * p2[0] + e * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + e * p3[1]))

    while i < len(tokens):
        if tokens[i].isalpha():
            cmd = tokens[i]
            i += 1
            if cmd in "Zz":
                points.append(start)
                x, y = start
                continue
        upper = cmd.upper()
        n = PATH_ARGS[upper]
        args = [float(t) for t in tokens[i:i + n]]
        i += n
        ox, oy = (x, y) if cmd.islower() else (0.0, 0.0)
        if upper == "M":
            x, y = args[0] + ox, args[1] + oy
            start = (x, y)
            points.append((x, y))
            cmd = "l" if cmd.islower() else "L"
        elif upper == "L":
            x, y = args[0] + ox, args[1] + oy
            points.append((x, y))
        elif upper == "H":
            x = args[0] + ox
            points.append((x, y))
        elif upper == "V":
            y = args[0] + oy
            points.append((x, y))
        elif upper == "C":
            p1, p2, p3 = (args[0] + ox, args[1] + oy), (args[2] + ox, args[3] + oy), (args[4] + ox, args[5] + oy)
            bezier((x, y), p1, p2, p3)
            last_control = p2
            x, y = p3
        elif upper == "S":
            p1 = (2 * x - last_control[0], 2 * y - last_control[1]) if last_control else (x, y)
            p2, p3 = (args[0] + ox, args[1] + oy), (args[2] + ox, args[3] + oy)
            bezier((x, y), p1, p2, p3)
            last_control = p2
            x, y = p3
        if upper not in "CS":
            last_control = None
    return points


def sampled(paths):
    return [sample_path(p.get("d")) for p in paths]


def box_of(strokes):
    """画をすべて囲む枠 (x, y, 幅, 高さ)。幅・高さが MIN_EXTENT より小さければ、中心を変えずに広げる"""
    xs = [p[0] for s in strokes for p in s]
    ys = [p[1] for s in strokes for p in s]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    w, h = max(max(xs) - min(xs), MIN_EXTENT), max(max(ys) - min(ys), MIN_EXTENT)
    return cx - w / 2, cy - h / 2, w, h


def fit(strokes, source, target):
    """画を source の枠から target の枠へ、縦横それぞれの比率で写す（ゲームの FakeKanjiComposer と同じ）"""
    sx, sy, sw, sh = source
    tx, ty, tw, th = target
    return [[(tx + (x - sx) / sw * tw, ty + (y - sy) / sh * th) for x, y in s] for s in strokes]


def stroke_mask(strokes, scale=2):
    """線を太さつきで描いた白黒の画像（numpy の真偽値の配列）。字の外に少しはみ出しても描けるように余白をとる"""
    margin = 10
    size = (109 + margin * 2) * scale
    image = Image.new("1", (size, size), 0)
    draw = ImageDraw.Draw(image)
    width = max(1, round(STROKE_WIDTH * scale))
    for stroke in strokes:
        points = [((x + margin) * scale, (y + margin) * scale) for x, y in stroke]
        draw.line(points if len(points) > 1 else points * 2, fill=1, width=width, joint="curve")
        for px, py in (points[0], points[-1]):
            draw.ellipse([px - width / 2, py - width / 2, px + width / 2, py + width / 2], fill=1)
    return numpy.array(image, dtype=bool)


def overlap_area(mask_a, mask_b, scale=2):
    """2つの線の画像が重なる面積（KanjiVG の座標で）"""
    return float((mask_a & mask_b).sum()) / (scale * scale)


def shrink(box, sides, amount):
    """枠の、指定した辺を内側へ amount（幅・高さに対する割合）だけ動かす"""
    x, y, w, h = box
    dx, dy = w * amount, h * amount
    if "l" in sides:
        x, w = x + dx, w - dx
    if "r" in sides:
        w -= dx
    if "t" in sides:
        y, h = y + dy, h - dy
    if "b" in sides:
        h -= dy
    return x, y, w, h


def place_part(radical_mask, part_strokes, rest_box, position):
    """
    借りた部品を置く枠を探す。本物の残りの部分の外枠から始め、部首の側から少しずつ狭める
    部品の線が部首の線（radical_mask。本物の部品が触れているところは除いたもの）にほぼ重ならなくなった枠を返す。見つからなければ None
    """
    source = box_of(part_strokes)
    source_aspect = source[2] / source[3]
    steps = round(MAX_SHRINK / SHRINK_STEP)
    for k in range(steps + 1):
        box = shrink(rest_box, SHRINK_SIDES[position], k * SHRINK_STEP)
        box_aspect = box[2] / box[3]
        # 狭めすぎて部品がつぶれるなら、それ以上は狭めない
        if max(box_aspect, source_aspect) / min(box_aspect, source_aspect) > MAX_ASPECT_RATIO:
            return None
        if len(part_strokes) * MIN_AREA_PER_STROKE > box[2] * box[3]:
            return None
        fitted = fit(part_strokes, source, box)
        if overlap_area(radical_mask, stroke_mask(fitted)) <= OVERLAP_TOLERANCE:
            return box
    return None


def aspect(paths):
    x0, y0, x1, y1 = bbox(paths)
    return max(x1 - x0, 1.0) / max(y1 - y0, 1.0)


# ---- KANJIDIC2 ----

def load_readings(path):
    with gzip.open(path, "rb") as f:
        data = f.read()
    # DTD は使わないので取り除いてから読む（外部実体を処理しない）
    data = re.sub(rb"<!DOCTYPE.*?\]>", b"", data, flags=re.S)
    root = ET.fromstring(data)
    readings = {}
    for ch in root.iter("character"):
        on = [r.text for r in ch.iter("reading") if r.get("r_type") == "ja_on"]
        kun = [r.text for r in ch.iter("reading") if r.get("r_type") == "ja_kun"]
        readings[ch.findtext("literal")] = (on, kun)
    return readings


def main_reading(readings, ch):
    """音読みを1つ（カタカナ）。音読みがなければ訓読み（ひらがな、送り仮名を除く）。"""
    on, kun = readings.get(ch, ([], []))
    if on:
        return on[0].strip("-")
    if kun:
        return kun[0].split(".")[0].strip("-")
    return None


def fake_reading(readings, part, donor):
    """借りた部品の音読み。部品が漢字でなければ、部品を借りた漢字の音読み。"""
    for ch in (part, donor):
        if ch and readings.get(ch, ([], []))[0]:
            return readings[ch][0][0].strip("-")
    return main_reading(readings, donor)


# ---- 偽物を作る ----

def donor_parts(shape, position, shapes, kanji_level, level, components):
    """入れ替えに使える部分。(部品の漢字, 借りる漢字, グループの id（丸ごと借りるときは空）, 画のパス) を返す。

    借りる部品は、出題範囲（その難易度以下）にある漢字に限る。見慣れた部品だけで偽物を作り、読みも自然にするため。
    - ほかの漢字の同じ場所にある部品（例：持の右側の「寺」）。元の場所の形に近いので、縮めても字形が崩れにくい
    - 漢字1字をまるごと（例：「寺」）。囲い・垂れ・繞の内側は、こちらでも形が合いやすい。
      ほかの漢字で部品として使われている漢字（components）だけにする。偏・旁・繞・垂れ・構えの部首を持つ漢字を
      丸ごと入れると（氵＋活など）部首が2つあるように見えて不自然なので、部首が上か下にある漢字（寺など）までにする
    """
    # 部首と同じ部品を含む漢字からは借りない（艹＋苗のように部首が2つ重なるため）
    radical = normalize(shape.radical_layout()[0][0].element or "")
    donors = [(d, s) for d, s in shapes.items()
              if d != shape.ch and kanji_level[d] <= level and radical not in s.elements and normalize(d) != radical]
    for donor, donor_shape in donors:
        donor_layout = donor_shape.layout(position)
        if donor_layout is not None:
            part = rest_element(donor_layout[1])
            if part in kanji_level and kanji_level[part] <= level:
                yield part, donor, [p.id for p in donor_layout[1]], [path for p in donor_layout[1] for path in p.paths]
    for donor, donor_shape in donors:
        if donor not in components:
            continue
        donor_radical = donor_shape.radical_layout()
        if donor_radical and donor_radical[0][0].position not in ("top", "bottom"):
            continue
        yield donor, donor, [], [path for p in donor_shape.parts for path in p.paths]


def make_question(shape, level, shapes, kanji_level, components, ids, readings):
    layout = shape.radical_layout()
    if layout is None:
        return None, "部首と残りの部分に分けられない"
    radicals, rest = layout
    position = radicals[0].position
    idc, radical_first = LAYOUTS[position]
    own = ids.split(shape.ch, idc, radical_first)
    radical_tree = own[0] if own else radicals[0].element
    if radical_tree is None:
        return None, "部首の文字がわからない"
    reading = main_reading(readings, shape.ch)
    if reading is None:
        return None, "読みがない"

    own_rest = {ids.flatten(own[1], expand=True)} if own else set()
    if rest_element(rest):
        own_rest.add(ids.expand(rest_element(rest)))
    rest_paths = [p for part in rest for p in part.paths]
    target_aspect = aspect(rest_paths)
    target_strokes = len(rest_paths)
    radical_key = ids.expand(radicals[0].element) if radicals[0].element else None

    candidates = {}
    for part, donor, groups, paths in donor_parts(shape, position, shapes, kanji_level, level, components):
        key = ids.expand(part)
        if part in candidates or key in own_rest or key == radical_key:
            continue
        if ids.exists(idc, *((radical_tree, part) if radical_first else (part, radical_tree))):
            continue
        donor_aspect = aspect(paths)
        if max(donor_aspect, target_aspect) / min(donor_aspect, target_aspect) > MAX_ASPECT_RATIO:
            continue
        # 元の部分より画が多すぎると、縮めたときに線が詰まって読めなくなる
        if len(paths) > max(target_strokes * 2, target_strokes + 4):
            continue
        candidates[part] = ({
            "part": part,
            "donor": donor,
            "groups": groups,
            "reading": fake_reading(readings, part, donor),
        }, paths)

    if len(candidates) < MIN_FAKES:
        return None, "偽物を3つ作れない"
    shuffled = list(candidates.values())
    random.Random(ord(shape.ch)).shuffle(shuffled)

    rest_strokes = sampled(rest_paths)
    xs = [p[0] for s in rest_strokes for p in s]
    ys = [p[1] for s in rest_strokes for p in s]
    if max(xs) - min(xs) < MIN_REST_SIZE or max(ys) - min(ys) < MIN_REST_SIZE:
        return None, "部首以外の部分が小さすぎる"

    # 部首とぶつからない枠が見つかった候補だけを、MAX_FAKES 個まで使う
    # 本物の部品が触れている部首の線は、偽物の部品が触れてもよい
    radical_mask = stroke_mask(sampled([p for part in radicals for p in part.paths])) & ~stroke_mask(rest_strokes)
    rest_box = box_of(rest_strokes)
    fakes = []
    for fake, paths in shuffled:
        box = place_part(radical_mask, sampled(paths), rest_box, position)
        if box is None:
            continue
        fake["box"] = [round(v, 2) for v in box]
        fakes.append(fake)
        if len(fakes) >= MAX_FAKES:
            break

    if len(fakes) < MIN_FAKES:
        return None, "部首とぶつからない偽物を3つ作れない"
    return {
        "kanji": shape.ch,
        "reading": reading,
        "radical": radicals[0].element or "",
        "position": position,
        "radicalGroups": [p.id for p in radicals],
        "fakes": fakes,
    }, None


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ids_path, kanjidic_path, svg_dir, sets_dir, out_dir = sys.argv[1:6]

    kanji_level = {}
    for level, key in enumerate(LEVELS):
        with open(os.path.join(sets_dir, f"KanjiSet_{key}.json"), encoding="utf-8") as f:
            for ch in json.load(f)["kanji"]:
                kanji_level[ch] = level

    shapes = {ch: KanjiShape(ch, os.path.join(svg_dir, f"{ord(ch):05x}.svg")) for ch in kanji_level}
    # ほかの漢字で部品として使われている漢字（持の「寺」など）
    components = set()
    for shape in shapes.values():
        for position in LAYOUTS:
            layout = shape.layout(position)
            if layout and rest_element(layout[1]) in kanji_level:
                components.add(rest_element(layout[1]))
    ids = IdsIndex(load_ids(ids_path))
    readings = load_readings(kanjidic_path)

    os.makedirs(out_dir, exist_ok=True)
    for level, key in enumerate(LEVELS):
        questions = []
        reasons = {}
        for ch, shape in shapes.items():
            if kanji_level[ch] != level:
                continue
            question, reason = make_question(shape, level, shapes, kanji_level, components, ids, readings)
            if question:
                questions.append(question)
            else:
                reasons[reason] = reasons.get(reason, 0) + 1
        path = os.path.join(out_dir, f"RealKanji_{key}.json")
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write("{\n")
            f.write(f'"id": {json.dumps(key)},\n')
            f.write(f'"sources": {json.dumps(SOURCES, ensure_ascii=False)},\n')
            f.write('"questions": [\n')
            f.write(",\n".join(json.dumps(q, ensure_ascii=False, separators=(",", ":")) for q in questions))
            f.write("\n]\n}\n")
        print(f"{path}: {len(questions)} 字（除外 {sum(reasons.values())} 字: {reasons}）")


if __name__ == "__main__":
    main()
