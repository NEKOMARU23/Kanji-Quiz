"""
読み当てモードの問題データ（JSON）を、JMdict から作るスクリプト。

使い方:
    python -I Tools/generate_reading_quiz.py <JMdict_e.gz> <漢字セットのフォルダ> <出力フォルダ>

例:
    python -I Tools/generate_reading_quiz.py JMdict_e.gz Assets/TechC/KanjiQuiz/Data/KanjiSets Assets/TechC/KanjiQuiz/Data/ReadingQuiz

JMdict_e.gz は http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz から入手する
（Electronic Dictionary Research and Development Group、CC BY-SA 4.0）。

出力は難易度ごとに ReadingQuiz_Easy.json・ReadingQuiz_Normal.json・ReadingQuiz_Hard.json。

難易度（2026-11-07 の決定）:
    - 熟語は2〜4字で、漢字だけでできている。字は漢字セットにあり、Hyogai（人名用漢字）は使わない
    - 熟語の中で一番難しい字の漢字セットで決める
        簡単 ＝ 小学漢字（Elementary）だけの熟語。普通の読み
        常識 ＝ 常用漢字（JoyoCommon）を含み、困難の字（JoyoRare）を含まない熟語。普通の読み
        困難 ＝ 困難の字（JoyoRare）を含む熟語。普段は読まない読み（下の「困難の読み」）

普通の読み（簡単・常識）:
    - JMdict でよく使われる言葉（ke_pri がある）だけ
    - 当て読み（gikun）・不規則な読み（ik）・古い読み（ok）・まれな読み（rk）・検索用（sk）は使わない
    - 当て字（ateji）・不規則な表記（iK）・送り仮名の違い（io）・古い字体（oK）・まれな字体（rK）・検索用（sK）も使わない

困難の読み:
    - 当て読み（gikun）・不規則な読み（ik）・古い読み（ok）・まれな読み（rk）・検索用（sk）を含めて集める
    - 当て字（ateji）・不規則な表記（iK）・送り仮名の違い（io）・古い字体（oK）・まれな字体（rK）・検索用（sK）も含める
    - よく使われる言葉かどうかは問わない

共通:
    - 読みは2つ以上あってもよい（複数回答）。読みは最大3つ
    - 子ども向けでない言葉（EXCLUDED_WORDS）と、JMdict の品位の印（vulg など）は使わない

選択肢の数（2026-11-07 の決定）:
    - 正しい読みが1つ：簡単 3、常識 4、困難 4
    - 正しい読みが複数：簡単 5、常識 6、困難 6（1つだけの数に 2 を足す）
    - 数はゲームが決める。このスクリプトは、まちがいの読みを1問に最大 6 つ用意する

まちがいの読み（wrong）の作り方:
    - 同じ字を含む別の熟語の読みを、最大 3 つ（同じ字を別の読みで読んだもの）
    - 残りは、読みの長さが近く、同じ読みで始まる別の熟語の読み
    - 同じ難易度の熟語の読みから取る。その熟語の読み（印のない読みも含む）は入れない
    - 同じ入力なら同じ結果になるように、熟語の文字で乱数を作る

注意:
    - 同じ字で読みが分かれる熟語（下手：へた・したて など）は、意味によって読みが変わる。
      複数回答の正解は、人が1問ずつ確かめてから使う
    - このスクリプトは JMdict の印で絞っただけ。意味と例文は AI で作って確かめる（別の作業）
"""
import gzip
import importlib.util
import json
import os
import random
import re
import sys
from collections import defaultdict
import xml.etree.ElementTree as ET

# 熟語を選ぶ規則（子ども向けでない言葉、品位の印、漢字の読み込み）は generate_kanji_words.py と同じものを使う
_HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("generate_kanji_words", os.path.join(_HERE, "generate_kanji_words.py"))
kw = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(kw)

SOURCE = kw.SOURCE

# 漢字セットの順（0 が一番やさしい）
SET_KEYS = ["Elementary", "JoyoCommon", "JoyoRare"]

# 難易度 ＝ 熟語の一番難しい字の段階、選択肢の数（1つだけ正解／複数回答）、読みの種類
DIFFICULTIES = {
    "Easy": {"level": 0, "single_options": 3, "multi_options": 5, "reading": "ordinary"},
    "Normal": {"level": 1, "single_options": 4, "multi_options": 6, "reading": "ordinary"},
    "Hard": {"level": 2, "single_options": 4, "multi_options": 6, "reading": "rare"},
}

# 読み・熟語の書き方の印
KUNJI_READING_INFO = {"gikun", "ik"}            # 当て読み・不規則な読み
RARE_READING_INFO = {"ok", "rk", "sk"}          # 古い読み・まれな読み・検索用
KUNJI_KANJI_INFO = {"ateji", "iK"}              # 当て字・不規則な表記
RARE_KANJI_INFO = {"io", "oK", "rK", "sK"}      # 送り仮名の違い・古い字体・まれな字体・検索用

KANA = re.compile(r"^[ぁ-ゖー]+$")
MIN_CHARS = 2
MAX_CHARS = 4
MAX_READINGS = 3
WRONG_STORED = 6          # 1問に用意するまちがいの読みの数（ゲームは必要な数だけ使う）
COMMON_WRONG = 3          # 同じ字を含む熟語の読みから取る数の上限


def parse_jmdict(path):
    """(言葉, 熟語の印, 読みの一覧) を1つずつ返す。読みの一覧は (読み, 制限, 印)。"""
    with gzip.open(path, "rb") as f:
        data = f.read()
    data = re.sub(rb"<!DOCTYPE.*?\]>", b"", data, flags=re.S)
    data = kw.ENTITY.sub(lambda m: m.group(0) if m.group(1) in kw.XML_ENTITIES else m.group(1), data.decode("utf-8"))
    root = ET.fromstring(data)
    for entry in root.iter("entry"):
        misc = {m.text for sense in entry.iter("sense") for m in sense.iter("misc")}
        if misc & kw.BAD_MISC:
            continue
        readings = []
        for r in entry.iter("r_ele"):
            if r.find("re_nokanji") is not None:
                continue
            readings.append((r.findtext("reb"), {x.text for x in r.iter("re_restr")}, {i.text for i in r.iter("re_inf")}))
        for k in entry.iter("k_ele"):
            word = k.findtext("keb")
            kinf = {i.text for i in k.iter("ke_inf")}
            pri = {p.text for p in k.iter("ke_pri")}
            yield word, kinf, pri, readings


def build_candidates(src, level_of):
    """難易度ごとの問題の候補と、各言葉の全読み（まちがいに使わないため）を返す。"""
    candidates = {name: [] for name in DIFFICULTIES}
    all_readings = defaultdict(set)
    seen = set()
    for word, kinf, pri, readings in parse_jmdict(src):
        # まちがいに使わないため、印の付いた読みも含めて全部覚える
        for reb, restr, _ in readings:
            if reb and (not restr or word in restr):
                all_readings[word].add(reb)
        if word in seen or word in kw.EXCLUDED_WORDS:
            continue
        if not (MIN_CHARS <= len(word) <= MAX_CHARS) or not kw.KANJI.match(word):
            continue
        if any(ch not in level_of for ch in word):
            continue
        level = max(level_of[ch] for ch in word)
        name = next(n for n, rule in DIFFICULTIES.items() if rule["level"] == level)
        rule = DIFFICULTIES[name]

        # 読みを分ける：普通の読み（印なし）と、すべての読み（まれな読みを含む）
        all_valid, plain_valid, rare_reading = [], [], False
        for reb, restr, inf in readings:
            if not KANA.match(reb) or reb in all_valid:
                continue
            if restr and word not in restr:
                continue
            all_valid.append(reb)
            if inf & (KUNJI_READING_INFO | RARE_READING_INFO):
                rare_reading = True
            else:
                plain_valid.append(reb)

        common = kw.priority(pri) is not None
        rare_kanji = bool(kinf & (KUNJI_KANJI_INFO | RARE_KANJI_INFO))
        if rule["reading"] == "ordinary":
            if not common or rare_kanji:
                continue
            valid = plain_valid
        else:
            valid = all_valid
            if not (rare_reading or rare_kanji):
                continue  # 困難は、普段は読まない読みの熟語だけ
        if not (1 <= len(valid) <= MAX_READINGS):
            continue
        seen.add(word)
        candidates[name].append({"word": word, "readings": valid, "common": common})
    return candidates, all_readings


def build_index(pool):
    """まちがいの読みを探すための索引：字ごと・読みの始まりごと。"""
    by_char = defaultdict(list)
    by_head = defaultdict(list)
    for cand in pool:
        for ch in set(cand["word"]):
            by_char[ch].append(cand)
        for reading in cand["readings"]:
            by_head[reading[0]].append(reading)
    return by_char, by_head


def wrong_readings(cand, rule, index, forbidden):
    """1問のまちがいの読みを選ぶ。作れなければ None。"""
    by_char, by_head = index
    word = cand["word"]
    own = cand["readings"]
    options = rule["single_options"] if len(own) == 1 else rule["multi_options"]
    needed = options - len(own)

    same_char = []
    for ch in set(word):
        for other in by_char[ch]:
            if other["word"] == word:
                continue
            same_char.extend(r for r in other["readings"] if r not in forbidden)

    similar = []
    for head in {r[0] for r in own}:
        for reading in by_head[head]:
            if reading in forbidden:
                continue
            if any(abs(len(reading) - len(r)) <= 1 for r in own):
                similar.append(reading)

    rng = random.Random(word)
    chosen = []
    for group, limit in ((same_char, COMMON_WRONG), (similar, WRONG_STORED)):
        pick = sorted(set(group))
        rng.shuffle(pick)
        for reading in pick:
            if len(chosen) >= min(limit, WRONG_STORED):
                break
            if reading not in chosen:
                chosen.append(reading)
    if len(chosen) < needed:
        return None
    return chosen[:WRONG_STORED]


def write_json(path, difficulty, questions):
    """1問を1行に書く（差分を見やすくするため）。"""
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("{\n")
        f.write(f'"id": {json.dumps(difficulty)},\n')
        f.write(f'"source": {json.dumps(SOURCE, ensure_ascii=False)},\n')
        f.write('"questions": [\n')
        f.write(",\n".join(json.dumps(q, ensure_ascii=False, separators=(",", ":")) for q in questions))
        f.write("\n]\n}\n")


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    src, sets_dir, out_dir = sys.argv[1:4]

    level_of = {}
    for lv, key in enumerate(SET_KEYS):
        with open(os.path.join(sets_dir, f"KanjiSet_{key}.json"), encoding="utf-8") as f:
            for ch in json.load(f)["kanji"]:
                level_of[ch] = lv

    candidates, all_readings = build_candidates(src, level_of)
    os.makedirs(out_dir, exist_ok=True)

    for name, rule in DIFFICULTIES.items():
        pool = sorted(candidates[name], key=lambda c: c["word"])
        index = build_index(pool)
        questions = []
        skipped = 0
        for cand in pool:
            forbidden = all_readings[cand["word"]] | set(cand["readings"])
            wrong = wrong_readings(cand, rule, index, forbidden)
            if wrong is None:
                skipped += 1
                continue
            questions.append({
                "word": cand["word"],
                "readings": cand["readings"],
                "wrong": wrong,
                "common": cand["common"],
            })
        single = sum(1 for q in questions if len(q["readings"]) == 1)
        multi = len(questions) - single
        common = sum(1 for q in questions if q["common"])
        path = os.path.join(out_dir, f"ReadingQuiz_{name}.json")
        write_json(path, name, questions)
        print(f"{path}: {len(questions)} 問（1つの読み {single}・複数の読み {multi}・よく使う {common}・まちがいが作れず除外 {skipped}）")


if __name__ == "__main__":
    main()
