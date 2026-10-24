"""
書き順クイズの出題範囲（漢字セット）の JSON を、KANJIDIC2 から作るスクリプト。

使い方:
    python -I Tools/generate_kanji_sets.py <kanjidic2.xml.gz> <KanjiVG のフォルダ> <出力フォルダ>

例:
    python -I Tools/generate_kanji_sets.py kanjidic2.xml.gz Assets/StreamingAssets/KanjiVG Assets/TechC/KanjiQuiz/Data/KanjiSets

KANJIDIC2 は https://www.edrdg.org/kanjidic/kanjidic2.xml.gz から入手する
（Electronic Dictionary Research and Development Group、CC BY-SA 4.0）。

作る漢字セット:
    Elementary : 小学校で学ぶ漢字（学年別漢字配当表、KANJIDIC2 の grade 1〜6）
    Joyo       : 常用漢字のうち、小学校では学ばない漢字（grade 8）
    Hyogai     : 常用漢字ではない漢字のうち、人名用漢字（grade 9）と、
                 新聞での使用頻度の順位（freq）がある漢字。
                 常用漢字の旧字体にあたる人名用漢字（grade 10、例：顯・眞）は、
                 常用漢字と見分けにくいので入れない
どのセットも、KanjiVG に書き順データ（SVG）がある漢字だけを入れる。
EXCLUDED に挙げた漢字は入れない。
"""
import gzip
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

SOURCE = "KANJIDIC2 (Electronic Dictionary Research and Development Group, CC BY-SA 4.0)"

# 出題範囲から外す漢字と、その理由
EXCLUDED = {
    "𠮟": "フォント（Klee One）に字形がなく、出題する漢字を表示できないため（𠮟）",
}


def load_characters(path):
    with gzip.open(path, "rb") as f:
        data = f.read()
    # DTD は使わないので取り除いてから読む（外部実体を処理しない）
    data = re.sub(rb"<!DOCTYPE.*?\]>", b"", data, flags=re.S)
    root = ET.fromstring(data)
    version = root.find("header").findtext("database_version")
    for ch in root.iter("character"):
        misc = ch.find("misc")
        grade = misc.findtext("grade")
        yield ch.findtext("literal"), int(grade) if grade else None, misc.findtext("freq") is not None, version


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    src, svg_dir, out_dir = sys.argv[1:4]
    svgs = set(os.listdir(svg_dir))

    sets = {
        "Elementary": {"title": "小学漢字", "kanji": []},
        "Joyo": {"title": "常用漢字", "kanji": []},
        "Hyogai": {"title": "常用漢字以外", "kanji": []},
    }
    version = ""
    for literal, grade, has_freq, version in load_characters(src):
        if f"{ord(literal):05x}.svg" not in svgs or literal in EXCLUDED:
            continue
        if grade is not None and 1 <= grade <= 6:
            sets["Elementary"]["kanji"].append(literal)
        elif grade == 8:
            sets["Joyo"]["kanji"].append(literal)
        elif grade == 9 or (grade is None and has_freq):
            sets["Hyogai"]["kanji"].append(literal)

    os.makedirs(out_dir, exist_ok=True)
    for key, value in sets.items():
        data = {
            "id": key,
            "title": value["title"],
            "source": f"{SOURCE}, database version {version}",
            "kanji": "".join(value["kanji"]),
        }
        path = os.path.join(out_dir, f"KanjiSet_{key}.json")
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
            f.write("\n")
        print(f"{path}: {len(value['kanji'])} 字")


if __name__ == "__main__":
    main()
