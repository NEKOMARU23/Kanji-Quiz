"""
漢字ごとの使用例（熟語）の JSON を、JMdict から作るスクリプト。
本物の漢字さがしの振り返りで使う。漢字セットの漢字すべてについて作るので、ほかのモードや漢字帳でも使える。

使い方:
    python -I Tools/generate_kanji_words.py <JMdict_e.gz> <漢字セットのフォルダ> <出力フォルダ>

例:
    python -I Tools/generate_kanji_words.py JMdict_e.gz Assets/TechC/KanjiQuiz/Data/KanjiSets Assets/TechC/KanjiQuiz/Data/KanjiWords

JMdict_e.gz は http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz から入手する
（Electronic Dictionary Research and Development Group、CC BY-SA 4.0）。

熟語の選び方:
    - 2〜4字の、漢字だけでできた言葉。熟語が3つに満たなければ、1字で使う言葉（鞄（かばん）など）も使う
    - ほかの漢字が、その漢字の難易度以下の漢字セットにある（簡単なら小学漢字だけでできた言葉）。読めない字を含めないため
    - よく使われる言葉（JMdict の ke_pri）を、よく使われる順に3つまで。
      1つもなければ、よく使われる印のない2字の言葉を2つまで。それもなければ熟語なし
    - 当て字・まれな書き方・まれな読み、下品・差別的・古い言葉は使わない
    - 選んだ言葉を含む長い言葉（海外 と 海外旅行 など）は選ばない
"""
import gzip
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

SOURCE = "JMdict (Electronic Dictionary Research and Development Group, CC BY-SA 4.0)"

LEVELS = ["Elementary", "Joyo", "Hyogai"]

MAX_WORDS = 3
MAX_UNCOMMON_WORDS = 2

# 使わない書き方・読み・言葉の種類（JMdict の ke_inf・re_inf・misc）
BAD_KANJI_INFO = {"ateji", "iK", "io", "oK", "rK", "sK"}
BAD_READING_INFO = {"gikun", "ik", "ok", "rk", "sk"}
BAD_MISC = {"vulg", "X", "derog", "sens", "arch", "obs", "obsc"}

KANJI = re.compile(r"^[㐀-鿿\U00020000-\U0003ffff]{1,4}$")
ENTITY = re.compile(r"&([A-Za-z0-9-]+);")
XML_ENTITIES = {"amp", "lt", "gt", "quot", "apos"}


def priority(pri):
    """よく使われる度合い。小さいほどよく使われる。印がなければ None。"""
    nf = [int(p[2:]) for p in pri if p.startswith("nf")]
    if nf:
        return min(nf)
    if pri & {"news1", "ichi1", "spec1", "gai1"}:
        return 25
    if pri & {"news2", "ichi2", "spec2", "gai2"}:
        return 50
    return None


def load_words(path):
    """(言葉, 読み, よく使われる度合い) を返す。"""
    with gzip.open(path, "rb") as f:
        data = f.read()
    # DTD は使わないので取り除いてから読む（外部実体を処理しない）。
    # 品詞などは DTD の実体（&n; など）で書かれているので、実体の名前に置き換える
    data = re.sub(rb"<!DOCTYPE.*?\]>", b"", data, flags=re.S)
    data = ENTITY.sub(lambda m: m.group(0) if m.group(1) in XML_ENTITIES else m.group(1), data.decode("utf-8"))
    root = ET.fromstring(data)
    for entry in root.iter("entry"):
        misc = {m.text for sense in entry.iter("sense") for m in sense.iter("misc")}
        if misc & BAD_MISC:
            continue
        readings = []
        for r in entry.iter("r_ele"):
            if r.find("re_nokanji") is not None or {i.text for i in r.iter("re_inf")} & BAD_READING_INFO:
                continue
            readings.append((r.findtext("reb"), {x.text for x in r.iter("re_restr")}))
        for k in entry.iter("k_ele"):
            word = k.findtext("keb")
            if not KANJI.match(word) or {i.text for i in k.iter("ke_inf")} & BAD_KANJI_INFO:
                continue
            reading = next((reb for reb, restr in readings if not restr or word in restr), None)
            if reading:
                yield word, reading, priority({p.text for p in k.iter("ke_pri")})


def choose(candidates):
    chosen = []
    for word, reading, _ in sorted(candidates, key=lambda c: (c[2], len(c[0]), c[0])):
        if any(c["word"] in word or word in c["word"] for c in chosen):
            continue
        chosen.append({"word": word, "reading": reading})
        if len(chosen) == MAX_WORDS:
            break
    return chosen


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    src, sets_dir, out_dir = sys.argv[1:4]

    kanji_level = {}
    for level, key in enumerate(LEVELS):
        with open(os.path.join(sets_dir, f"KanjiSet_{key}.json"), encoding="utf-8") as f:
            for ch in json.load(f)["kanji"]:
                kanji_level[ch] = level

    single = {ch: [] for ch in kanji_level}
    common = {ch: [] for ch in kanji_level}
    uncommon = {ch: [] for ch in kanji_level}
    seen = set()
    for word, reading, pri in load_words(src):
        if word in seen or any(ch not in kanji_level for ch in word):
            continue
        seen.add(word)
        if len(word) == 1:
            if pri is not None:
                single[word].append((word, reading, pri))
            continue
        for ch in set(word):
            if any(kanji_level[other] > kanji_level[ch] for other in word):
                continue
            if pri is not None:
                common[ch].append((word, reading, pri))
            elif len(word) == 2:
                uncommon[ch].append((word, reading, 100))

    os.makedirs(out_dir, exist_ok=True)
    for level, key in enumerate(LEVELS):
        entries = []
        missing = 0
        for ch, lv in kanji_level.items():
            if lv != level:
                continue
            words = choose(common[ch])
            if len(words) < MAX_WORDS and single[ch]:
                words.append(choose(single[ch])[0])
            if not words:
                # 印のない言葉には見慣れないものも多いので、よく使われる言葉が1つもないときだけ、少しだけ使う
                words = choose(uncommon[ch])[:MAX_UNCOMMON_WORDS]
            if not words:
                missing += 1
            entries.append({"kanji": ch, "words": words})
        path = os.path.join(out_dir, f"KanjiWords_{key}.json")
        # 差分を見やすくするため、1字を1行に書く
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write("{\n")
            f.write(f'"id": {json.dumps(key)},\n')
            f.write(f'"source": {json.dumps(SOURCE)},\n')
            f.write('"entries": [\n')
            f.write(",\n".join(json.dumps(e, ensure_ascii=False, separators=(",", ":")) for e in entries))
            f.write("\n]\n}\n")
        print(f"{path}: {len(entries)} 字（熟語なし {missing} 字）")


if __name__ == "__main__":
    main()
