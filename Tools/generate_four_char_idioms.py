"""
四字熟語づくりの問題データ（Data/FourCharIdiom/FourCharIdiom_<難易度>.json）を作る。

もとは Tools/four_char_idioms_source.tsv（AI（Claude）が書いたもの。手で直してよい）。読みは
Tools/four_char_idiom_candidates.tsv（extract_four_char_idioms.py で JMdict から取り出したもの）から取る。
作る前に次のことを確かめ、まちがいがあれば JSON を書かずに止まる：
  - 難易度が Easy・Normal・Hard のどれかで、同じ熟語が2回出てこない
  - 熟語が候補（JMdict の四字熟語）にある
  - 意味と例文の読みの書き方（{漢字|よみ}、よみはひらがな）。読みのない漢字は、例文の中の熟語そのもののほかには残さない
  - 例文に熟語が入っている。意味・例文とも、表示する文で 40 字以内
  - まぎらわしい字が、位置ごとに3字ずつ（12 字とも別の字）で、常用漢字。熟語の字とは重ならない
  - まぎらわしい字を1つ入れただけで、別の四字熟語（候補にあるもの）にならない
  - 常識の熟語は、2字を入れ替えても別の四字熟語にならない（2マスを空けるため）
  - 困難の熟語は、熟語の字と 12 字のまぎらわしい字を組み合わせて、別の四字熟語（並べ替えを含む）が作れない
ほかの難易度で別の四字熟語が作れるものは、数だけ知らせる（その難易度では、並べ替えるマスがないので困らない）。

使い方:
    python -I Tools/generate_four_char_idioms.py
"""
import json
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TOOLS = ROOT / "Tools"
DATA = ROOT / "Assets" / "TechC" / "KanjiQuiz" / "Data"
OUT_DIR = DATA / "FourCharIdiom"
SOURCE = TOOLS / "four_char_idioms_source.tsv"
CANDIDATES = TOOLS / "four_char_idiom_candidates.tsv"
LEVELS = {"Easy": "四字熟語（簡単）", "Normal": "四字熟語（常識）", "Hard": "四字熟語（困難）"}
KANJI_SETS = ["Elementary", "JoyoCommon", "JoyoRare"]
MAX_LENGTH = 40
CONFUSERS_PER_POSITION = 3
MARK = re.compile(r"\{([^{}|]+)\|([^{}|]+)\}")
HIRAGANA = re.compile(r"^[ぁ-ゖー]+$")
SOURCE_NOTE = "熟語と読み：JMdict (EDRDG, CC BY-SA 4.0)。難易度・意味・例文・まぎらわしい字：AI (Claude) で作成"


def is_kanji(ch):
    code = ord(ch)
    return 0x4E00 <= code <= 0x9FFF or 0x3400 <= code <= 0x4DBF or code >= 0x20000 or ch == "々"


def render(text, use_hiragana):
    return MARK.sub(lambda m: m.group(2) if use_hiragana else m.group(1), text)


def check_text(word, label, text, errors, allow_word):
    for m in MARK.finditer(text):
        if not HIRAGANA.match(m.group(2)):
            errors.append(f"{word}: {label}の読み「{m.group(2)}」がひらがなではない")
    plain = MARK.sub("", text)
    if allow_word:
        plain = plain.replace(word, "")
    left = [ch for ch in plain if is_kanji(ch)]
    if left:
        errors.append(f"{word}: {label}に読みのない漢字がある（{''.join(left)}）")
    if "{" in plain or "}" in plain or "|" in plain:
        errors.append(f"{word}: {label}の読みの書き方がまちがっている")
    length = max(len(render(text, False)), len(render(text, True)))
    if length > MAX_LENGTH:
        errors.append(f"{word}: {label}が長い（{length} 字）")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    joyo = set()
    for key in KANJI_SETS:
        joyo.update(json.loads((DATA / "KanjiSets" / f"KanjiSet_{key}.json").read_text(encoding="utf-8"))["kanji"])

    readings = {}
    for line in CANDIDATES.read_text(encoding="utf-8").splitlines():
        cols = line.split("\t")
        if len(cols) >= 2:
            readings[cols[0]] = cols[1]
    known = set(readings)

    errors = []
    entries = {level: [] for level in LEVELS}
    seen = set()
    for n, line in enumerate(SOURCE.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip() or line.startswith("#"):
            continue
        cols = [c.strip() for c in line.split("||")]
        if len(cols) != 5:
            errors.append(f"{n} 行目：列の数が 5 ではない")
            continue
        level, word, meaning, example, confusers = cols
        if level not in LEVELS:
            errors.append(f"{word}: 難易度「{level}」が Easy・Normal・Hard のどれでもない")
            continue
        if word in seen:
            errors.append(f"{word}: 2回出てくる")
            continue
        seen.add(word)
        if len(word) != 4 or word not in readings:
            errors.append(f"{word}: 候補（JMdict の四字熟語）にない")
            continue

        check_text(word, "意味", meaning, errors, allow_word=False)
        check_text(word, "例文", example, errors, allow_word=True)
        if word not in MARK.sub("", example):
            errors.append(f"{word}: 例文に熟語が入っていない")

        groups = confusers.split("/")
        if len(groups) != 4 or any(len(g) != CONFUSERS_PER_POSITION for g in groups):
            errors.append(f"{word}: まぎらわしい字が、位置ごとに {CONFUSERS_PER_POSITION} 字ずつそろっていない（{confusers}）")
            continue
        flat = "".join(groups)
        if len(set(flat)) != len(flat):
            errors.append(f"{word}: まぎらわしい字に同じ字が2回ある（{confusers}）")
        for ch in flat:
            if ch not in joyo:
                errors.append(f"{word}: まぎらわしい字「{ch}」が常用漢字ではない")
            if ch in word:
                errors.append(f"{word}: まぎらわしい字「{ch}」が熟語の字と同じ")
        for pos, group in enumerate(groups):
            for ch in group:
                other = word[:pos] + ch + word[pos + 1:]
                if other in known:
                    errors.append(f"{word}: {pos + 1} 字目に「{ch}」を入れると、別の四字熟語「{other}」になる")

        # 常識では2マスを空けるので、2字を入れ替えた並びが別の熟語だと、正しく並べても不正解になりうる
        if level == "Normal":
            for i in range(4):
                for j in range(i + 1, 4):
                    chars = list(word)
                    chars[i], chars[j] = chars[j], chars[i]
                    swapped = "".join(chars)
                    if swapped != word and swapped in known:
                        errors.append(f"{word}: {i + 1} 字目と {j + 1} 字目を入れ替えると、別の四字熟語「{swapped}」になる（常識では2マスを空けるため）")

        entries[level].append({
            "word": word,
            "reading": readings[word],
            "meaning": meaning,
            "example": example,
            "confusers": groups,
        })

    if errors:
        print("まちがいがあるので、JSON を書きません：")
        for e in errors:
            print("  " + e)
        sys.exit(1)

    # 困難では4字とも並べ、12 字のまぎらわしい字から 8 字を出すので、組み合わせで別の熟語が作れると
    # 正しく並べても不正解になりうる。困難の熟語ならまちがいとして止め、ほかの難易度なら知らせるだけにする
    anagram = []
    for level in entries:
        for e in entries[level]:
            tiles = Counter(e["word"] + "".join(e["confusers"]))
            for other in known:
                if other != e["word"] and not (Counter(other) - tiles):
                    if level == "Hard":
                        errors.append(f"{e['word']}: 候補の字で別の四字熟語「{other}」も作れる（困難では4字とも並べるため）")
                    else:
                        anagram.append(f"{e['word']} → {other}")
    if errors:
        print("まちがいがあるので、JSON を書きません：")
        for e in errors:
            print("  " + e)
        sys.exit(1)
    if anagram:
        print(f"注意：まぎらわしい字との組み合わせで別の四字熟語が作れるもの {len(anagram)} 件（簡単・常識では字を並べ替えないので、出題には困らない）")
        for a in anagram:
            print("  " + a)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for level, title in LEVELS.items():
        if not entries[level]:
            continue
        path = OUT_DIR / f"FourCharIdiom_{level}.json"
        lines = [json.dumps(e, ensure_ascii=False, separators=(",", ":")) for e in entries[level]]
        body = ",\n".join(lines)
        path.write_text(
            "{" + f'"id":"{level}","title":"{title}","source":"{SOURCE_NOTE}","questions":[\n' + body + "\n]}\n",
            encoding="utf-8", newline="\n")
        print(f"{path.relative_to(ROOT)}: {len(entries[level])} 語")


if __name__ == "__main__":
    main()
