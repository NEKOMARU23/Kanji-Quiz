"""
使い分けモードの問題データ（Data/KanjiUsage）のもとになる、同じ読みの言葉の組を JMdict から取り出す。

取り出した組から、AI（Claude）が出題する組を選び、文・候補それぞれの短い意味をつけて
Tools/kanji_usage_source.tsv に書く。それを generate_kanji_usage.py で確かめて JSON にする（別の作業）。

使い方:
    python -I Tools/extract_kanji_usage_groups.py <JMdict_e.gz> <漢字セットのフォルダ> <出力ファイル（.tsv）>

例:
    python -I Tools/extract_kanji_usage_groups.py JMdict_e.gz Assets/TechC/KanjiQuiz/Data/KanjiSets Tools/kanji_usage_candidates.tsv

JMdict_e.gz は http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz から入手する
（Electronic Dictionary Research and Development Group、CC BY-SA 4.0）。

言葉の選び方（2026-11-14 の決定。Docs/GameSpec.md 2.1）:
    - 漢字だけの言葉（同音異義語。回答・解答）と、漢字のあとに送り仮名が続く言葉・漢字1字の言葉
      （異字同訓。暑い・熱い、橋・端）
    - どの字も常用漢字（Elementary・JoyoCommon・JoyoRare の漢字セット）にある。人名用漢字は使わない
    - 組は、JMdict でよく使われる言葉（ke_pri がある）が2つ以上ある読み。よく使われない言葉も、
      選択肢の埋め合わせに使えるかを見るため一覧に入れる（頻度「-」）
    - 当て字・まれな書き方・まれな読み、下品・差別的・古い言葉、固有名詞、専門分野だけの言葉は使わない
      （規則は generate_reading_quiz.py と同じ）
    - 同じ読みの言葉が2つ以上ある読みだけを出す。候補は実在する言葉だけにするため、
      ゲームの選択肢の数（簡単3・常識4・困難4）に足りない組は、あとで出題しない

出力（タブ区切り、1行に1つの読み）:
    読み、よく使う言葉の数、そのうち小学漢字だけの言葉の数、よく使わない言葉も入れた数、
    言葉の一覧（「言葉|漢字セット|頻度|ふつうの読みか|英語の意味」を「 / 」でつなぐ）
    「ふつうの読みか」は、その読みが辞書でその言葉の最初の読みなら 1、2番目以降なら 0
    （細胞＝さいほう、神道＝しんどう、羽＝は、酸味＝すみ は 0。2026-11-14 にユーザーの確認で見つかった）
    漢字セットは E（小学）・C（常用の前半）・R（常用の後半）。頻度は小さいほどよく使う（nf の番号、なければ 25・50）。
    よく使われる印のない言葉は頻度を「-」にする。よく使う言葉が2つ以上ある読みだけを出す
"""
import gzip
import importlib.util
import json
import os
import re
import sys
from collections import defaultdict
import xml.etree.ElementTree as ET

# 言葉を選ぶ規則は generate_kanji_words.py・generate_reading_quiz.py と同じものを使う
_HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location("generate_kanji_words", os.path.join(_HERE, "generate_kanji_words.py"))
kw = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(kw)
_spec = importlib.util.spec_from_file_location("generate_reading_quiz", os.path.join(_HERE, "generate_reading_quiz.py"))
rq = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(rq)

SET_KEYS = ["Elementary", "JoyoCommon", "JoyoRare"]
LEVEL_MARK = "ECR"

# 漢字（々を含む）のあとに送り仮名（ひらがな）が続く言葉。漢字は1字以上
WORD = re.compile(r"^[㐀-鿿々]+[ぁ-ゖ]*$")
MAX_LENGTH = 6
BAD_KANJI_INFO = rq.KUNJI_KANJI_INFO | rq.RARE_KANJI_INFO
BAD_READING_INFO = rq.KUNJI_READING_INFO | rq.RARE_READING_INFO
UNCOMMON = 99  # よく使われる印（ke_pri）のない言葉の頻度


def load_levels(sets_dir):
    level_of = {}
    for lv, key in enumerate(SET_KEYS):
        with open(os.path.join(sets_dir, f"KanjiSet_{key}.json"), encoding="utf-8") as f:
            for ch in json.load(f)["kanji"]:
                level_of[ch] = lv
    return level_of


def parse(path, level_of):
    """読み → {言葉: (漢字セット, 頻度, 英語の意味)}。"""
    with gzip.open(path, "rb") as f:
        data = f.read()
    data = re.sub(rb"<!DOCTYPE.*?\]>", b"", data, flags=re.S)
    data = kw.ENTITY.sub(lambda m: m.group(0) if m.group(1) in kw.XML_ENTITIES else m.group(1), data.decode("utf-8"))
    root = ET.fromstring(data)

    groups = defaultdict(dict)
    for entry in root.iter("entry"):
        senses = list(entry.iter("sense"))
        misc = {m.text for s in senses for m in s.iter("misc")}
        if misc & kw.BAD_MISC or misc & rq.PROPER_MISC:
            continue
        if senses and all({f.text for f in s.iter("field")} & rq.TECH_FIELDS for s in senses):
            continue
        gloss = senses[0].findtext("gloss") if senses else ""
        readings = []
        all_readings = []  # 印のある読みも含めた、辞書に書かれた順の読み（いちばんふつうの読みを知るため）
        for r in entry.iter("r_ele"):
            if r.find("re_nokanji") is not None:
                continue
            reb = r.findtext("reb")
            restr = {x.text for x in r.iter("re_restr")}
            if reb:
                all_readings.append((reb, restr))
            inf = {i.text for i in r.iter("re_inf")}
            if not reb or not rq.KANA.match(reb) or inf & BAD_READING_INFO:
                continue
            readings.append((reb, restr))
        for k in entry.iter("k_ele"):
            word = k.findtext("keb")
            if not word or len(word) > MAX_LENGTH or not WORD.match(word):
                continue
            if word in kw.EXCLUDED_WORDS or {i.text for i in k.iter("ke_inf")} & BAD_KANJI_INFO:
                continue
            # よく使われない言葉は、頻度を UNCOMMON にして別に数える（選択肢の埋め合わせに使えるかを見るため）
            freq = kw.priority({p.text for p in k.iter("ke_pri")})
            if freq is None:
                freq = UNCOMMON
            kanji = [ch for ch in word if "㐀" <= ch <= "鿿"]
            if not kanji or any(ch not in level_of for ch in kanji):
                continue
            level = max(level_of[ch] for ch in kanji)
            # その言葉のいちばんふつうの読み＝辞書でその書き方に当てはまる最初の読み
            # （細胞は「さいぼう」が最初で「さいほう」は2番目。2番目以降の読みで組に入った言葉は、問題に使わない）
            applicable = [reb for reb, restr in all_readings if not restr or word in restr]
            first = applicable[0] if applicable else None
            for reb, restr in readings:
                if restr and word not in restr:
                    continue
                main = reb == first
                old = groups[reb].get(word)
                if old is None:
                    groups[reb][word] = (level, freq, gloss or "", main)
                else:
                    # 同じ書き方が別の項目にもあるとき（入る：はいる・いる）は、どちらかでふつうの読みならふつうとする
                    best = old if old[1] <= freq else (level, freq, gloss or "", old[3])
                    groups[reb][word] = (best[0], best[1], best[2], old[3] or main)
    return groups


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    src, sets_dir, out = sys.argv[1:4]
    level_of = load_levels(sets_dir)
    groups = parse(src, level_of)

    rows = []
    for reading in sorted(groups):
        words = groups[reading]
        common = {w: v for w, v in words.items() if v[1] != UNCOMMON}
        # よく使う言葉が2つ以上ある読みだけ（よく使わない言葉だけの組は出さない）
        if len(common) < 2:
            continue
        items = sorted(words.items(), key=lambda kv: (kv[1][1], kv[0]))
        elementary = sum(1 for _, (lv, fq, _, _) in items if lv == 0 and fq != UNCOMMON)
        text = " / ".join(f"{w}|{LEVEL_MARK[lv]}|{fq if fq != UNCOMMON else '-'}|{'1' if main else '0'}|{gl}"
                          for w, (lv, fq, gl, main) in items)
        rows.append((reading, len(common), elementary, len(items), text))

    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("# 使い分けの候補（extract_kanji_usage_groups.py で作る。手で編集しない）。出典: " + kw.SOURCE + "\n")
        f.write("reading\tcommon\telementary\tall\twords\n")
        for row in rows:
            f.write("\t".join(str(x) for x in row) + "\n")

    three = sum(1 for r in rows if r[1] >= 3)
    four = sum(1 for r in rows if r[1] >= 4)
    easy = sum(1 for r in rows if r[2] >= 3)
    three_all = sum(1 for r in rows if r[3] >= 3)
    four_all = sum(1 for r in rows if r[3] >= 4)
    print(f"{out}: 読み {len(rows)} 組")
    print(f"  よく使う言葉だけ：3つ以上 {three}・4つ以上 {four}・小学漢字だけで3つ以上 {easy}")
    print(f"  よく使わない言葉も入れると：3つ以上 {three_all}・4つ以上 {four_all}")


if __name__ == "__main__":
    main()
