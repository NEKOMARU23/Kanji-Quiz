using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 問題データ（RealKanjiEntry）から本物の漢字さがしの問題を作る
    /// 偽物の候補から毎回ランダムに選び、本物と部品を借りる漢字の SVG を読んで字を組み立てる
    /// </summary>
    public sealed class RealKanjiQuestionFactory
    {
        /// <summary>振り返りで本物の選択肢に出す説明</summary>
        public const string RealDescription = "本物";

        private readonly IKanjiSvgSource svgSource;
        private readonly KanjiWordDictionary wordDictionary;
        private readonly KanjiMeaningDictionary meaningDictionary;
        private readonly Dictionary<string, RealKanjiEntry> entries = new Dictionary<string, RealKanjiEntry>();
        private readonly List<string> candidates = new List<string>();

        /// <summary>出題できる漢字（問題データの順）</summary>
        public IReadOnlyList<string> Candidates => candidates;

        /// <param name="entries">問題データ</param>
        /// <param name="wordDictionary">振り返りで出す使用例。null なら使用例なし</param>
        /// <param name="meaningDictionary">振り返りで出す意味と例文。null なら意味と例文なし</param>
        public RealKanjiQuestionFactory(
            IKanjiSvgSource svgSource, IEnumerable<RealKanjiEntry> entries, KanjiWordDictionary wordDictionary = null,
            KanjiMeaningDictionary meaningDictionary = null)
        {
            this.svgSource = svgSource ?? throw new ArgumentNullException(nameof(svgSource));
            this.wordDictionary = wordDictionary;
            this.meaningDictionary = meaningDictionary;
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.kanji) || this.entries.ContainsKey(entry.kanji)) continue;
                this.entries.Add(entry.kanji, entry);
                candidates.Add(entry.kanji);
            }
        }

        /// <summary>
        /// 並べた順に漢字を選んで、指定した数の問題を作る。作った問題の出題順はシャッフルする
        /// 苦手な漢字を先頭に並べておけば、優先して出題できる（WeakQuestionList.OrderCandidates）
        /// </summary>
        /// <param name="orderedCandidates">問題を作る順に並べた漢字。問題データにない漢字は飛ばす</param>
        /// <param name="choiceCount">選択肢の数（本物1つと偽物）</param>
        /// <param name="questionCount">作る問題の数</param>
        /// <returns>作れた問題のリスト。漢字が足りないときは指定した数より少なくなる</returns>
        public async UniTask<List<RealKanjiQuestion>> CreateFromOrderedAsync(
            IReadOnlyList<string> orderedCandidates,
            int choiceCount,
            int questionCount,
            CancellationToken ct)
        {
            if (orderedCandidates == null) throw new ArgumentNullException(nameof(orderedCandidates));
            if (choiceCount < 2) throw new ArgumentOutOfRangeException(nameof(choiceCount), "選択肢は2つ以上必要です。");
            if (questionCount < 1) throw new ArgumentOutOfRangeException(nameof(questionCount));

            var questions = new List<RealKanjiQuestion>(questionCount);
            foreach (var kanji in orderedCandidates)
            {
                if (questions.Count >= questionCount) break;
                if (!entries.TryGetValue(kanji, out var entry)) continue;

                var question = await TryCreateAsync(entry, choiceCount, ct);
                if (question != null) questions.Add(question);
            }

            if (questions.Count < questionCount)
            {
                Debug.LogWarning($"[RealKanjiQuestionFactory] {questionCount} 問を作れませんでした（{questions.Count} 問）。");
            }

            // 苦手な漢字がいつも最初に出ないように、出題順を混ぜる
            ShuffleUtility.Shuffle(questions);
            return questions;
        }

        private async UniTask<RealKanjiQuestion> TryCreateAsync(RealKanjiEntry entry, int choiceCount, CancellationToken ct)
        {
            var realStrokes = await svgSource.LoadStrokesAsync(entry.kanji, ct);
            if (realStrokes.Count == 0)
            {
                Debug.LogWarning($"[RealKanjiQuestionFactory] 漢字 '{entry.kanji}' は書き順データがないため除外しました。");
                return null;
            }

            var radicalGroups = new HashSet<string>(entry.radicalGroups ?? Array.Empty<string>());
            var choices = new List<RealKanjiChoice>(choiceCount);
            foreach (var fake in ShuffleUtility.ShuffledCopy(entry.fakes ?? Array.Empty<RealKanjiFakeEntry>()))
            {
                if (choices.Count >= choiceCount - 1) break;
                if (fake == null || string.IsNullOrEmpty(fake.donor)) continue;

                var donorStrokes = await svgSource.LoadStrokesAsync(fake.donor, ct);
                var strokes = FakeKanjiComposer.Compose(
                    realStrokes, radicalGroups, donorStrokes, new HashSet<string>(fake.groups ?? Array.Empty<string>()), fake.box);
                if (strokes == null) continue;

                choices.Add(new RealKanjiChoice(strokes, fake.reading, DescribeFake(entry, fake)));
            }

            if (choices.Count < choiceCount - 1)
            {
                Debug.LogWarning($"[RealKanjiQuestionFactory] 漢字 '{entry.kanji}' は偽物を {choiceCount - 1} 個作れないため除外しました。");
                return null;
            }

            int correctIndex = UnityEngine.Random.Range(0, choiceCount);
            choices.Insert(correctIndex, new RealKanjiChoice(FakeKanjiComposer.ToStrokeData(realStrokes), entry.reading, RealDescription));

            var words = wordDictionary != null ? wordDictionary.GetWords(entry.kanji) : Array.Empty<KanjiWord>();
            var meaning = meaningDictionary != null ? meaningDictionary.Get(entry.kanji) : default;
            return new RealKanjiQuestion(entry.kanji, entry.reading, words, choices, correctIndex, meaning);
        }

        /// <summary>
        /// 偽物の部品の組み合わせを説明する文字列（例：氵＋舎（捨の右側）。漢字1字をまるごと借りたときは 氵＋寺）
        /// </summary>
        public static string DescribeFake(RealKanjiEntry entry, RealKanjiFakeEntry fake)
        {
            string text = $"{entry.radical}＋{fake.part}";
            bool isWholeKanji = fake.groups == null || fake.groups.Length == 0;
            if (isWholeKanji || fake.donor == fake.part) return text;

            return $"{text}（{fake.donor}の{RestPositionName(entry.position)}）";
        }

        /// <summary>
        /// 部首の位置から、残りの部分の場所の名前を返す（部首が左なら、残りは右側）
        /// </summary>
        private static string RestPositionName(string radicalPosition)
        {
            switch (radicalPosition)
            {
                case "left": return "右側";
                case "right": return "左側";
                case "top": return "下";
                case "bottom": return "上";
                default: return "内側";
            }
        }
    }
}
