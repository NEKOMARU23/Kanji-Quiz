"""
読み当ての漢字帳に出す「意味と使い方（例文）」のデータを作り、確かめる。

元は Tools/reading_quiz_meanings_source.tsv（AI（Claude）が書く。1行に1語、タブ区切り：語・意味・例文）。
ここから Data/ReadingQuiz/ReadingQuizMeanings.json を作る。読み当ての問題データ（ReadingQuiz_*.json）とは別のファイルなので、
generate_reading_quiz.py で問題データを作り直しても消えない。

書き方（本物の漢字さがしの意味と例文と同じ。Docs/GameSpec.md 「意味と例文のデータ」）：
  - 語の漢字以外の漢字には {漢字|よみ} の形で読みをつける。語そのものは、例文に読みなしで入れる
  - 読みはひらがな。意味と例文は、表示する文で 40 字以内
  - 読みが複数ある語で、読みによって意味が変わるものは、意味を「（へた）…／（したて）…」の形で並べる（長さは 60 字まで）

確かめること：問題データにある語か／重複／書き方／読みなしの漢字／長さ／例文に語が入っているか。
足りない語（まだ書いていない語）は、数だけ出す（--require-all で、足りなければ失敗にする）。

    python -I Tools/generate_reading_quiz_meanings.py [--require-all] [--next N]

--next N：まだ書いていない語を、簡単→困難→常識（よく使う語が先）の順に N 語出す（次に書く語の一覧）。
"""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "Assets" / "TechC" / "KanjiQuiz" / "Data" / "ReadingQuiz"
SOURCE = ROOT / "Tools" / "reading_quiz_meanings_source.tsv"
OUTPUT = DATA / "ReadingQuizMeanings.json"
LEVELS = ["Easy", "Hard", "Normal"]
MAX_LENGTH = 40
MAX_LENGTH_MULTI = 60
MARK = re.compile(r"\{([^{}|]+)\|([^{}|]+)\}")
HIRAGANA = re.compile(r"^[ぁ-ゖー]+$")


def is_kanji(ch):
    code = ord(ch)
    return 0x4E00 <= code <= 0x9FFF or 0x3400 <= code <= 0x4DBF or code >= 0x20000 or ch == "々"


def render(text):
    return MARK.sub(lambda m: m.group(1), text)


def load_questions():
    """問題データの語を、簡単→困難→常識の順に（常識はよく使う語が先）読む。{語: 読みの数}と並び"""
    order, readings = [], {}
    for level in LEVELS:
        data = json.loads((DATA / f"ReadingQuiz_{level}.json").read_text(encoding="utf-8"))
        qs = data["questions"]
        if level == "Normal":
            qs = sorted(qs, key=lambda q: not q.get("common", False))
        for q in qs:
            if q["word"] not in readings:
                readings[q["word"]] = len(q["readings"])
                order.append(q["word"])
    return order, readings


def check_text(where, label, text, word, limit, errors):
    if not text:
        errors.append(f"{where}: {label}がない")
        return
    if text.count("{") != len(MARK.findall(text)) or text.count("}") != text.count("{"):
        errors.append(f"{where}: {label}の読みの書き方が正しくない：{text}")
        return
    for marked, reading in MARK.findall(text):
        if not HIRAGANA.match(reading):
            errors.append(f"{where}: {label}の「{marked}」の読みがひらがなでない：{reading}")
        if not any(is_kanji(ch) for ch in marked):
            errors.append(f"{where}: {label}で、漢字でないところに読みがついている：{marked}")
        if word in marked:
            errors.append(f"{where}: {label}で、語そのものに読みがついている：{marked}")
    unmarked = sorted({ch for ch in MARK.sub("", text) if is_kanji(ch) and ch not in word})
    if unmarked:
        errors.append(f"{where}: {label}に読みのない漢字がある：{''.join(unmarked)}")
    if len(render(text)) > limit:
        errors.append(f"{where}: {label}が長すぎる（{len(render(text))}字）：{render(text)}")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    require_all = "--require-all" in sys.argv
    next_count = int(sys.argv[sys.argv.index("--next") + 1]) if "--next" in sys.argv else 0

    order, readings = load_questions()
    errors, entries, seen = [], [], set()
    lines = SOURCE.read_text(encoding="utf-8").splitlines() if SOURCE.exists() else []
    for number, line in enumerate(lines, 1):
        if not line.strip() or line.startswith("#"):
            continue
        cols = line.split("\t")
        where = f"{number}行目"
        if len(cols) != 3:
            errors.append(f"{where}: タブ区切りの3列（語・意味・例文）になっていない")
            continue
        word, meaning, example = (c.strip() for c in cols)
        where = f"{number}行目 {word}"
        if word not in readings:
            errors.append(f"{where}: 読み当ての問題データにない語")
            continue
        if word in seen:
            errors.append(f"{where}: 2回出てくる")
            continue
        seen.add(word)
        check_text(where, "意味", meaning, word, MAX_LENGTH_MULTI if readings[word] > 1 else MAX_LENGTH, errors)
        check_text(where, "例文", example, word, MAX_LENGTH, errors)
        if example and word not in MARK.sub("", example):
            errors.append(f"{where}: 例文に語が（読みなしで）入っていない")
        entries.append({"word": word, "meaning": meaning, "example": example})

    missing = [w for w in order if w not in seen]
    print(f"書いた語：{len(seen)} / {len(order)}　足りない語：{len(missing)}　問題：{len(errors)} 件")
    for error in errors[:60]:
        print("NG", error)
    if len(errors) > 60:
        print(f"... ほか {len(errors) - 60} 件")
    if next_count:
        print("次に書く語：" + "、".join(missing[:next_count]))
    if errors or (require_all and missing):
        return 1

    # 1語を1行に書く（差分を見やすくするため）
    rows = ",\n".join(json.dumps(e, ensure_ascii=False, separators=(",", ":")) for e in entries)
    OUTPUT.write_text('{\n"id":"ReadingQuizMeanings",\n"entries":[\n' + rows + "\n]\n}\n", encoding="utf-8")
    print(f"書き出した：{OUTPUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
