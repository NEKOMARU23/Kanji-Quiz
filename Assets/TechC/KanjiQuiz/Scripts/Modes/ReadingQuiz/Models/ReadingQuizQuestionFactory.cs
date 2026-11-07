using System;
using System.Collections.Generic;
using System.Linq;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 問題データ（ReadingQuizEntry）から読み当ての問題を作る
    /// 正しい読みと、まちがいの読みを必要な数だけ選んで、選択肢の並びを毎回シャッフルする
    /// </summary>
    public static class ReadingQuizQuestionFactory
    {
        /// <summary>
        /// 問題データを毎回シャッフルして、作れた問題を指定した数まで返す
        /// </summary>
        /// <param name="entries">問題データ（難易度で選んだもの）</param>
        /// <param name="difficulty">難易度（選択肢の数を決める）</param>
        /// <param name="questionCount">作る問題の数</param>
        /// <returns>作れた問題。データが足りないときは指定した数より少なくなる</returns>
        public static List<ReadingQuizQuestion> Create(IEnumerable<ReadingQuizEntry> entries, Difficulty difficulty, int questionCount)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (questionCount < 0) throw new ArgumentOutOfRangeException(nameof(questionCount));

            var questions = new List<ReadingQuizQuestion>();
            foreach (var entry in ShuffleUtility.ShuffledCopy(entries))
            {
                if (questions.Count >= questionCount) break;

                var question = CreateOrNull(entry, difficulty);
                if (question != null) questions.Add(question);
            }
            return questions;
        }

        /// <summary>
        /// 並べた順に作れる問題を選び、選んだ問題の出題順だけシャッフルする
        /// 苦手な熟語を先頭に並べておけば、優先して出題できる（WeakQuestionList.OrderCandidates）
        /// </summary>
        public static List<ReadingQuizQuestion> CreateInOrder(IEnumerable<ReadingQuizEntry> orderedEntries, Difficulty difficulty, int questionCount)
        {
            if (orderedEntries == null) throw new ArgumentNullException(nameof(orderedEntries));
            if (questionCount < 0) throw new ArgumentOutOfRangeException(nameof(questionCount));

            var questions = new List<ReadingQuizQuestion>();
            foreach (var entry in orderedEntries)
            {
                if (questions.Count >= questionCount) break;

                var question = CreateOrNull(entry, difficulty);
                if (question != null) questions.Add(question);
            }
            ShuffleUtility.Shuffle(questions);
            return questions;
        }

        /// <summary>
        /// 1件から問題を作る。まちがいの読みが足りないときは null
        /// </summary>
        public static ReadingQuizQuestion CreateOrNull(ReadingQuizEntry entry, Difficulty difficulty)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var correct = Clean(entry.readings);
            if (correct.Count == 0 || correct.Count > ReadingQuizAnswer.MaxChoiceCount) return null;

            int total = ReadingQuizRule.ChoiceCount(difficulty, correct.Count);
            if (total > ReadingQuizAnswer.MaxChoiceCount) return null;

            int needed = total - correct.Count;
            var wrong = Clean(entry.wrong).Where(reading => !correct.Contains(reading)).ToList();
            if (wrong.Count < needed) return null;

            // 正しい読みとまちがいの読みを混ぜ、並びを決める
            ShuffleUtility.Shuffle(wrong);
            var choices = correct.Select(reading => (text: reading, isCorrect: true))
                .Concat(wrong.Take(needed).Select(reading => (text: reading, isCorrect: false)))
                .ToList();
            ShuffleUtility.Shuffle(choices);

            int correctMask = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].isCorrect) correctMask |= 1 << i;
            }
            return new ReadingQuizQuestion(entry.word, choices.Select(c => c.text), correctMask);
        }

        /// <summary>空の読みと重なりを取り除く</summary>
        private static List<string> Clean(IEnumerable<string> readings)
        {
            if (readings == null) return new List<string>();
            return readings
                .Where(reading => !string.IsNullOrEmpty(reading))
                .Distinct()
                .ToList();
        }
    }
}
