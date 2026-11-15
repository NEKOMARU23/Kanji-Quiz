using System;
using System.Collections.Generic;
using System.Linq;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 問題データ（KanjiUsageEntry）から使い分けの問題を作る
    /// 選択肢の数は、問題データのまちがいの数＋1（簡単3、常識4、困難3または4）。並びは毎回シャッフルする
    /// </summary>
    public static class KanjiUsageQuestionFactory
    {
        /// <summary>まちがいの言葉の数の下限（選択肢は3つ以上）</summary>
        public const int MinWrongCount = 2;

        /// <summary>まちがいの言葉の数の上限（選択肢は6つまで。数字キーが6つ目まで）</summary>
        public const int MaxWrongCount = 5;

        /// <summary>
        /// 問題データを毎回シャッフルして、作れた問題を指定した数まで返す
        /// </summary>
        /// <param name="entries">問題データ（難易度で選んだもの）</param>
        /// <param name="questionCount">作る問題の数</param>
        /// <returns>作れた問題。データが足りないときは指定した数より少なくなる</returns>
        public static List<KanjiUsageQuestion> Create(IEnumerable<KanjiUsageEntry> entries, int questionCount)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (questionCount < 0) throw new ArgumentOutOfRangeException(nameof(questionCount));

            var questions = new List<KanjiUsageQuestion>();
            foreach (var entry in ShuffleUtility.ShuffledCopy(entries))
            {
                if (questions.Count >= questionCount) break;

                var question = CreateOrNull(entry);
                if (question != null) questions.Add(question);
            }
            return questions;
        }

        /// <summary>
        /// 並べた順に作れる問題を選び、選んだ問題の出題順だけシャッフルする
        /// 苦手な言葉を先頭に並べておけば、優先して出題できる（WeakQuestionList.OrderCandidates）
        /// </summary>
        public static List<KanjiUsageQuestion> CreateInOrder(IEnumerable<KanjiUsageEntry> orderedEntries, int questionCount)
        {
            if (orderedEntries == null) throw new ArgumentNullException(nameof(orderedEntries));
            if (questionCount < 0) throw new ArgumentOutOfRangeException(nameof(questionCount));

            var questions = new List<KanjiUsageQuestion>();
            foreach (var entry in orderedEntries)
            {
                if (questions.Count >= questionCount) break;

                var question = CreateOrNull(entry);
                if (question != null) questions.Add(question);
            }
            ShuffleUtility.Shuffle(questions);
            return questions;
        }

        /// <summary>
        /// 1件から問題を作る。文の空いたところが1つでないとき、まちがいの言葉の数が合わないときは null
        /// </summary>
        public static KanjiUsageQuestion CreateOrNull(KanjiUsageEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (string.IsNullOrEmpty(entry.answer) || string.IsNullOrEmpty(entry.sentence)) return null;
            if (CountBlanks(entry.sentence) != 1) return null;

            // 言葉と意味は組にしたままシャッフルする（意味は、正解、まちがいの順に並んでいる）
            var pairs = new List<(KanjiUsageChoice choice, bool isCorrect)>
            {
                (new KanjiUsageChoice(entry.answer, MeaningAt(entry, 0)), true),
            };
            var seen = new HashSet<string> { entry.answer };
            for (int i = 0; entry.wrong != null && i < entry.wrong.Length; i++)
            {
                string word = entry.wrong[i];
                if (string.IsNullOrEmpty(word) || !seen.Add(word)) continue;
                pairs.Add((new KanjiUsageChoice(word, MeaningAt(entry, i + 1)), false));
            }

            int wrongCount = pairs.Count - 1;
            if (wrongCount < MinWrongCount || wrongCount > MaxWrongCount) return null;

            ShuffleUtility.Shuffle(pairs);
            int correctIndex = pairs.FindIndex(pair => pair.isCorrect);
            return new KanjiUsageQuestion(entry.reading ?? string.Empty, entry.sentence, pairs.Select(pair => pair.choice), correctIndex);
        }

        private static string MeaningAt(KanjiUsageEntry entry, int index)
        {
            return entry.meanings != null && index < entry.meanings.Length ? entry.meanings[index] : string.Empty;
        }

        private static int CountBlanks(string sentence)
        {
            int count = 0;
            foreach (char ch in sentence)
            {
                if (ch.ToString() == KanjiUsageQuestion.Blank) count++;
            }
            return count;
        }
    }
}
