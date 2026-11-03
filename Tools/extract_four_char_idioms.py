"""
四字熟語づくりの問題データ（Data/FourCharIdiom）のもとになる、四字熟語の候補を JMdict から取り出す。

取り出した候補から、AI（Claude）が知られている度合いで難易度を分け、読み・意味・例文・まぎらわしい字をつけて
Data/FourCharIdiom の JSON を作る。できた JSON は Tools/check_four_char_idioms.py で確かめる。

使い方:
    python -I Tools/extract_four_char_idioms.py <JMdict_e.gz> <漢字セットのフォルダ> <出力ファイル（.tsv）>

例:
    python -I Tools/extract_four_char_idioms.py JMdict_e.gz Assets/TechC/KanjiQuiz/Data/KanjiSets yoji_candidates.tsv

JMdict_e.gz は http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz から入手する
（Electronic Dictionary Research and Development Group、CC BY-SA 4.0）。

候補の選び方:
    - JMdict で四字熟語の印（misc の yoji）がある、漢字4字（々を含む）の言葉
    - どの字も常用漢字（Elementary・JoyoCommon・JoyoRare の漢字セット）にある。人名用漢字は使わない
    - 当て字・まれな書き方・まれな読み、下品・差別的・古い言葉は使わない
    - 子ども向けでない言葉は、generate_kanji_words.py の EXCLUDED_WORDS と同じ考えで、あとで AI が外す

出力（タブ区切り、1行に1語）:
    言葉、読み、よく使う印（1/0）、使っている漢字のいちばん上の漢字セット、英語の意味（1つ目）
"""
import gzip
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

KANJI_LEVELS = ["Elementary", "JoyoCommon", "JoyoRare"]
BAD_KANJI_INFO = {"ateji", "iK", "io", "oK", "rK", "sK"}
BAD_READING_INFO = {"gikun", "ik", "ok", "rk", "sk"}
BAD_MISC = {"vulg", "X", "derog", "sens", "arch", "obs", "obsc"}
ENTITY = re.compile(r"&([A-Za-z0-9-]+);")
XML_ENTITIES = {"amp", "lt", "gt", "quot", "apos"}


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    src, sets_dir, out_path = sys.argv[1:4]

    kanji_level = {}
    for level, key in enumerate(KANJI_LEVELS):
        with open(os.path.join(sets_dir, f"KanjiSet_{key}.json"), encoding="utf-8") as f:
            for ch in json.load(f)["kanji"]:
                kanji_level[ch] = level

    with gzip.open(src, "rb") as f:
        data = f.read()
    # DTD は使わないので取り除いてから読む（外部実体を処理しない）。品詞などの実体は名前に置き換える
    data = re.sub(rb"<!DOCTYPE.*?\]>", b"", data, flags=re.S)
    data = ENTITY.sub(lambda m: m.group(0) if m.group(1) in XML_ENTITIES else m.group(1), data.decode("utf-8"))
    root = ET.fromstring(data)

    rows = []
    seen = set()
    for entry in root.iter("entry"):
        misc = {m.text for sense in entry.iter("sense") for m in sense.iter("misc")}
        if "yoji" not in misc or misc & BAD_MISC:
            continue
        readings = []
        for r in entry.iter("r_ele"):
            if r.find("re_nokanji") is not None or {i.text for i in r.iter("re_inf")} & BAD_READING_INFO:
                continue
            readings.append((r.findtext("reb"), {x.text for x in r.iter("re_restr")}))
        gloss = entry.find("sense").findtext("gloss") or ""
        for k in entry.iter("k_ele"):
            word = k.findtext("keb")
            if word in seen or len(word) != 4 or {i.text for i in k.iter("ke_inf")} & BAD_KANJI_INFO:
                continue
            if not all(ch in kanji_level or ch == "々" for ch in word) or word[0] == "々":
                continue
            reading = next((reb for reb, restr in readings if not restr or word in restr), None)
            if not reading:
                continue
            seen.add(word)
            common = 1 if k.find("ke_pri") is not None else 0
            top = max(kanji_level[ch] for ch in word if ch != "々")
            rows.append((word, reading, common, KANJI_LEVELS[top], gloss.replace("\t", " ")))
            break

    rows.sort(key=lambda r: (-r[2], KANJI_LEVELS.index(r[3]), r[0]))
    with open(out_path, "w", encoding="utf-8", newline="\n") as f:
        for row in rows:
            f.write("\t".join(str(v) for v in row) + "\n")
    print(f"{out_path}: {len(rows)} 語（よく使う印 {sum(r[2] for r in rows)} 語）")


if __name__ == "__main__":
    main()
