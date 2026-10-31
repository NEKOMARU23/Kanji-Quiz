"""
本物の漢字さがしの振り返りに出す「意味と例文」のデータ（Data/KanjiMeanings）を確かめる。

データは AI（Claude）で作ったもの。手で直したら、このスクリプトで確かめ直す。
確かめること：
  - 出題する漢字（Data/RealKanji）がすべてそろっているか（--allow-missing で、足りない字は数だけ出す）
  - 例文に、その漢字が読みなしで入っているか
  - 読みをつけていない漢字がないか（その漢字だけは読みなしでよい）
  - 読みがひらがなだけか
  - 使った漢字が、その漢字の難易度以下の漢字セットにあるか（簡単なら小学漢字だけ）
  - 長さ（意味・例文とも、表示する文で 40 字以内）

    python -I Tools/check_kanji_meanings.py [--allow-missing]
"""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "Assets" / "TechC" / "KanjiQuiz" / "Data"
LEVELS = ["Elementary", "JoyoCommon", "JoyoRare"]
MAX_LENGTH = 40
MARK = re.compile(r"\{([^{}|]+)\|([^{}|]+)\}")
HIRAGANA = re.compile(r"^[ぁ-ゖー]+$")


def is_kanji(ch):
    code = ord(ch)
    return 0x4E00 <= code <= 0x9FFF or 0x3400 <= code <= 0x4DBF or code >= 0x20000 or ch == "々"


def render(text, use_hiragana):
    return MARK.sub(lambda m: m.group(2) if use_hiragana else m.group(1), text)


def load_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    allow_missing = "--allow-missing" in sys.argv
    # 々（くりかえしの記号）はどの難易度でも使ってよい
    allowed = {"々"}
    errors = []
    total_missing = 0

    for level in LEVELS:
        # その難易度以下の漢字セットにある漢字だけを、意味と例文に使ってよい
        allowed |= set(load_json(DATA / "KanjiSets" / f"KanjiSet_{level}.json")["kanji"])
        questions = [q["kanji"] for q in load_json(DATA / "RealKanji" / f"RealKanji_{level}.json")["questions"]]

        path = DATA / "KanjiMeanings" / f"KanjiMeanings_{level}.json"
        entries = load_json(path)["entries"] if path.exists() else []
        seen = {}
        for entry in entries:
            kanji, meaning, example = entry["kanji"], entry.get("meaning", ""), entry.get("example", "")
            where = f"{level} {kanji}"
            if kanji in seen:
                errors.append(f"{where}: 2回出てくる")
            seen[kanji] = entry

            for label, text in (("意味", meaning), ("例文", example)):
                if not text:
                    errors.append(f"{where}: {label}がない")
                    continue
                if text.count("{") != len(MARK.findall(text)):
                    errors.append(f"{where}: {label}の読みの書き方が正しくない：{text}")
                for word, reading in MARK.findall(text):
                    if not HIRAGANA.match(reading):
                        errors.append(f"{where}: {label}の「{word}」の読みがひらがなでない：{reading}")
                    if kanji in word:
                        errors.append(f"{where}: {label}の「{word}」に、出題する字が読みつきで入っている")
                plain_unmarked = MARK.sub("", text)
                unmarked = sorted({ch for ch in plain_unmarked if is_kanji(ch) and ch != kanji})
                if unmarked:
                    errors.append(f"{where}: {label}に読みのない漢字がある：{''.join(unmarked)}")
                outside = sorted({ch for ch in render(text, False) if is_kanji(ch) and ch != kanji and ch not in allowed})
                if outside:
                    errors.append(f"{where}: {label}に難易度より上の漢字がある：{''.join(outside)}")
                if len(render(text, False)) > MAX_LENGTH:
                    errors.append(f"{where}: {label}が長すぎる（{len(render(text, False))}字）")
            if example and kanji not in MARK.sub("", example):
                errors.append(f"{where}: 例文に出題する字が（読みなしで）入っていない")

        extra = [k for k in seen if k not in questions]
        if extra:
            errors.append(f"{level}: 出題しない字が入っている：{''.join(extra)}")
        missing = [k for k in questions if k not in seen]
        total_missing += len(missing)
        print(f"{level}: {len(seen)} / {len(questions)} 字")
        if missing and not allow_missing:
            errors.append(f"{level}: 足りない字が {len(missing)} 字ある（例：{''.join(missing[:20])}）")

    for error in errors:
        print("NG", error)
    print(f"足りない字：{total_missing}　問題：{len(errors)} 件")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
