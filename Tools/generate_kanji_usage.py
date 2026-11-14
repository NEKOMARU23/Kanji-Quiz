"""
使い分けモードの問題データ（Data/KanjiUsage/KanjiUsage_<難易度>.json）を、元データの TSV から確かめて作る。

元データ（Tools/kanji_usage_source.tsv）は AI（Claude）が書いたもの。手で直してよい（直したらこのスクリプトで作り直す）。
言葉の組は Tools/kanji_usage_candidates.tsv（extract_kanji_usage_groups.py で JMdict から取り出したもの）から取る。

使い方:
    python -I Tools/generate_kanji_usage.py [--check]

    --check をつけると、確かめるだけで JSON は書かない。

元データの形（1行に1問。列は「 || 」で区切る。# で始まる行と空行は読まない）:
    難易度 || 読み || 正解の言葉 || 文 || まちがいの言葉（/ 区切り） || 意味（「言葉=意味」を ／ で区切る）

    - 難易度は Easy・Normal・Hard（Docs/GameSpec.md 2.1）
        Easy   ＝ 小学漢字だけの言葉で、意味がはっきり違う組。まちがいは 2 つ（選択肢 3）
        Normal ＝ 常用漢字の言葉の組。まちがいは 3 つ（選択肢 4）
        Hard   ＝ 意味の近い組。まちがいは 3 つ（選択肢 4）。よく使う言葉がちょうど3つの組
                 （保証・保障・補償など）だけは、まちがい 2 つ（選択肢 3）でよい（2026-11-14 の決定）
    - 文の空いたところは「＿」（全角の下線）で1か所だけ書く。正解の言葉は、送り仮名も含めてそのままの形で入る
    - 文と意味の漢字には {漢字|よみ} で読みをつける（ひらがなで表示の設定のため）
    - 意味は、正解とまちがいの言葉すべてに書く

確かめること:
    - 正解とまちがいの言葉が、どれも同じ読みの、よく使う言葉（候補の一覧で頻度が「-」でないもの）か
    - その読みが、言葉のふつうの読み（辞書の最初の読み）か（細胞＝さいほう、羽＝は のような読みは使わない）
    - 言葉が重ならないか、まちがいの数が難易度に合っているか
    - 送り仮名の違いだけの言葉（表す・表わす）が選択肢に並んでいないか（どちらも正しいため）
    - 簡単は、正解もまちがいも小学漢字だけの言葉か
    - 文に「＿」が1つだけあるか。文・意味の漢字に読みがついているか。読みがひらがなだけか
    - 送り仮名のある言葉の空いたところのすぐ後ろに、活用の続き（て・た・ます など）を書いていないか
    - 文・意味に使った漢字が、その難易度以下の漢字セットにあるか
    - 長さ（文 40 字・意味 30 字まで。表示する形で数える）
    - 同じ難易度で正解の言葉が重ならないか（苦手な問題を正解の言葉で覚えるため）
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "Assets" / "TechC" / "KanjiQuiz" / "Data"
SOURCE_PATH = ROOT / "Tools" / "kanji_usage_source.tsv"
CANDIDATES_PATH = ROOT / "Tools" / "kanji_usage_candidates.tsv"
OUT_DIR = DATA / "KanjiUsage"

SOURCE = ("Claude（Anthropic の AI）で作り、Tools/generate_kanji_usage.py で確かめたもの。"
          "言葉の組は JMdict (Electronic Dictionary Research and Development Group, CC BY-SA 4.0)")
DIFFICULTIES = ["Easy", "Normal", "Hard"]
WRONG_COUNT = {"Easy": 2, "Normal": 3, "Hard": 3}
# 文と意味に使ってよい漢字セット（その難易度以下）
LEVELS = {"Easy": ["Elementary"], "Normal": ["Elementary", "JoyoCommon"],
          "Hard": ["Elementary", "JoyoCommon", "JoyoRare"]}
# 辞書の最初の読みではないが、ふつうに使う読み（2026-11-14 に確かめた）
#   描く＝かく：常用漢字表に「かく」の読みがある。済み＝すみ：「確認済み」などで使う
READING_EXCEPTIONS = {("描く", "かく"), ("済み", "すみ")}
MAX_SENTENCE = 40
MAX_MEANING = 30
BLANK = "＿"
# 送り仮名のある言葉の後ろに続くと、言葉がそのままの形で入らない文字（治め「て」いた、暑「かっ」た など）
CONJUGATION_TAILS = set("てたでだなまれせかきくけさしず")

MARK = re.compile(r"\{([^{}|]+)\|([^{}|]+)\}")
HIRAGANA = re.compile(r"^[ぁ-ゖー]+$")


def is_kanji(ch):
    code = ord(ch)
    return 0x4E00 <= code <= 0x9FFF or 0x3400 <= code <= 0x4DBF or code >= 0x20000 or ch == "々"


def stem(word):
    """送り仮名を除いた漢字の部分（表す・表わす → 表）。"""
    return "".join(ch for ch in word if is_kanji(ch))


def render(text, use_hiragana=False):
    return MARK.sub(lambda m: m.group(2) if use_hiragana else m.group(1), text)


def load_candidates():
    """読み → {言葉: (漢字セットの印 E/C/R, よく使うか, その読みがふつうの読みか)}。"""
    groups = {}
    for line in CANDIDATES_PATH.read_text(encoding="utf-8").splitlines():
        if line.startswith("#") or line.startswith("reading\t") or not line.strip():
            continue
        reading, _, _, _, words = line.split("\t")
        groups[reading] = {}
        for item in words.split(" / "):
            word, level, freq, main = item.split("|")[:4]
            groups[reading][word] = (level, freq != "-", main == "1")
    return groups


def check_text(where, label, text, allowed, max_length, errors):
    """読みのつけ方・使ってよい漢字・長さを確かめる。"""
    if not text:
        errors.append(f"{where}: {label}がない")
        return
    if text.count("{") != len(MARK.findall(text)) or text.count("}") != text.count("{"):
        errors.append(f"{where}: {label}の読みの書き方が正しくない：{text}")
    for word, reading in MARK.findall(text):
        if not HIRAGANA.match(reading):
            errors.append(f"{where}: {label}の「{word}」の読みがひらがなでない：{reading}")
        if not any(is_kanji(ch) for ch in word):
            errors.append(f"{where}: {label}の「{word}」に漢字がないのに読みがついている")
    unmarked = sorted({ch for ch in MARK.sub("", text) if is_kanji(ch)})
    if unmarked:
        errors.append(f"{where}: {label}に読みのない漢字がある：{''.join(unmarked)}")
    outside = sorted({ch for ch in render(text) if is_kanji(ch) and ch not in allowed})
    if outside:
        errors.append(f"{where}: {label}に難易度より上の漢字がある：{''.join(outside)}")
    if len(render(text)) > max_length:
        errors.append(f"{where}: {label}が長すぎる（{len(render(text))}字）")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    check_only = "--check" in sys.argv
    groups = load_candidates()
    sets = {key: set(json.loads((DATA / "KanjiSets" / f"KanjiSet_{key}.json").read_text(encoding="utf-8"))["kanji"])
            for key in ["Elementary", "JoyoCommon", "JoyoRare"]}

    questions = {name: [] for name in DIFFICULTIES}
    errors = []
    for number, line in enumerate(SOURCE_PATH.read_text(encoding="utf-8").splitlines(), start=1):
        if not line.strip() or line.startswith("#"):
            continue
        cols = [c.strip() for c in line.split("||")]
        where = f"{number}行目"
        if len(cols) != 6:
            errors.append(f"{where}: 列が6つでない（{len(cols)}）")
            continue
        difficulty, reading, answer, sentence, wrong_text, meaning_text = cols
        where = f"{number}行目 {difficulty} {answer}"
        if difficulty not in DIFFICULTIES:
            errors.append(f"{where}: 難易度が正しくない")
            continue
        wrong = [w.strip() for w in wrong_text.split("/") if w.strip()]

        # 言葉の組：同じ読みの、よく使う言葉か
        group = groups.get(reading)
        words = [answer] + wrong
        if group is None:
            errors.append(f"{where}: 読み「{reading}」が候補の一覧にない")
        else:
            for word in words:
                if word not in group:
                    errors.append(f"{where}: 「{word}」は読み「{reading}」の言葉にない")
                elif not group[word][1]:
                    errors.append(f"{where}: 「{word}」はよく使う言葉ではない")
                elif not group[word][2] and (word, reading) not in READING_EXCEPTIONS:
                    errors.append(f"{where}: 「{word}」をふつうは「{reading}」と読まない（辞書の2番目以降の読み）")
                elif difficulty == "Easy" and group[word][0] != "E":
                    errors.append(f"{where}: 簡単なのに「{word}」が小学漢字だけの言葉でない")
        if len(set(words)) != len(words):
            errors.append(f"{where}: 言葉が重なっている")
        # 送り仮名の違いだけの言葉（表す・表わす）は同じ言葉なので、選択肢に並べない
        if len({stem(w) for w in words}) != len(words):
            errors.append(f"{where}: 送り仮名の違いだけの言葉が並んでいる")
        # 困難は、よく使う言葉がちょうど3つの組（保証・保障・補償など）だけ、まちがい2つ（3択）でよい（2026-11-14）。
        # 送り仮名の違いだけの言葉は1つと数える
        # ふつうでない読みで組に入った言葉（細胞＝さいほう など）も数えない
        common_count = len({stem(w) for w, (_, common, main) in (group or {}).items() if common and main})
        three_word_hard = difficulty == "Hard" and len(wrong) == 2 and common_count == 3
        if len(wrong) != WRONG_COUNT[difficulty] and not three_word_hard:
            errors.append(f"{where}: まちがいの数が {len(wrong)}（{WRONG_COUNT[difficulty]} つ要る）")

        allowed = set().union(*(sets[key] for key in LEVELS[difficulty])) | {"々"}
        # 文：空いたところは1か所。文の中に正解の言葉を書かない
        if sentence.count(BLANK) != 1:
            errors.append(f"{where}: 文の「{BLANK}」が1つでない")
        # 送り仮名のある言葉は、そのままの形で入る。空いたところのすぐ後ろに活用の続き（治め「て」いた）を書かない
        elif answer[-1:] and HIRAGANA.match(answer[-1]):
            after = render(sentence).split(BLANK, 1)[1][:1]
            if after and after in CONJUGATION_TAILS:
                errors.append(f"{where}: 空いたところの後ろが活用の続きになっている（「{answer}」がそのまま入らない）")
        check_text(where, "文", sentence, allowed, MAX_SENTENCE - len(answer), errors)

        # 意味：言葉ごとに1つ
        meanings = {}
        for item in [m.strip() for m in meaning_text.split("／") if m.strip()]:
            if "=" not in item:
                errors.append(f"{where}: 意味の書き方が正しくない：{item}")
                continue
            word, meaning = (x.strip() for x in item.split("=", 1))
            meanings[word] = meaning
        for word in words:
            if word not in meanings:
                errors.append(f"{where}: 「{word}」の意味がない")
            else:
                check_text(where, f"「{word}」の意味", meanings[word], allowed, MAX_MEANING, errors)
        extra = [w for w in meanings if w not in words]
        if extra:
            errors.append(f"{where}: 選択肢にない言葉の意味がある：{'・'.join(extra)}")

        questions[difficulty].append({
            "reading": reading,
            "answer": answer,
            "sentence": sentence,
            "wrong": wrong,
            "meanings": [meanings.get(w, "") for w in words],
        })

    for name in DIFFICULTIES:
        answers = [q["answer"] for q in questions[name]]
        repeated = sorted({a for a in answers if answers.count(a) > 1})
        if repeated:
            errors.append(f"{name}: 正解の言葉が重なっている：{'・'.join(repeated)}")

    for error in errors:
        print("NG", error)
    for name in DIFFICULTIES:
        print(f"{name}: {len(questions[name])} 問")
    print(f"問題：{len(errors)} 件")
    if errors:
        return 1
    if not check_only:
        OUT_DIR.mkdir(parents=True, exist_ok=True)
        for name in DIFFICULTIES:
            path = OUT_DIR / f"KanjiUsage_{name}.json"
            lines = ["{", f'"id": "{name}",', f'"source": {json.dumps(SOURCE, ensure_ascii=False)},', '"questions": [']
            body = [json.dumps(q, ensure_ascii=False, separators=(",", ":")) for q in questions[name]]
            lines.append(",\n".join(body))
            lines += ["]", "}"]
            path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
            print(f"{path.relative_to(ROOT)} を書いた")
    return 0


if __name__ == "__main__":
    sys.exit(main())
