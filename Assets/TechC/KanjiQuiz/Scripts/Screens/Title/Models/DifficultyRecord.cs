using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// モード選択画面の黒板に書く、1つの難易度の記録（自己ベスト・級・次の目標・苦手な問題）
    /// </summary>
    public sealed class DifficultyRecord
    {
        /// <summary>難易度</summary>
        public Difficulty Difficulty { get; }

        /// <summary>出題範囲の説明（例：小学漢字）</summary>
        public string Range { get; }

        /// <summary>1回のプレイの問題数</summary>
        public int QuestionCount { get; }

        /// <summary>自己ベストがあるかどうか</summary>
        public bool HasRecord { get; }

        /// <summary>自己ベスト（最高の正解数）。記録がなければ 0</summary>
        public int BestCorrectCount { get; }

        /// <summary>自己ベストの級（例：5級）。記録がなければ null</summary>
        public string Rank { get; }

        /// <summary>自己ベストの級の高さ（KanjiRank.EvaluateLevel）。記録がなければ -1</summary>
        public int RankLevel { get; }

        /// <summary>
        /// 次の目標のひとこと（例：あと1問で 5級！）
        /// 全問正解なら「全問正解！」、記録がなければ「全問正解で ○級」
        /// </summary>
        public string Goal { get; }

        /// <summary>苦手な問題（書き順クイズと本物の漢字さがしは漢字）。新しい順</summary>
        public IReadOnlyList<string> WeakQuestions { get; }

        /// <param name="bestCorrectCount">自己ベスト。記録がなければ null</param>
        /// <param name="weakQuestions">苦手な問題。古い順（WeakQuestionList.Items と同じ）</param>
        public DifficultyRecord(
            Difficulty difficulty, string range, int questionCount, int? bestCorrectCount, IReadOnlyList<string> weakQuestions)
        {
            if (questionCount <= 0) throw new ArgumentOutOfRangeException(nameof(questionCount));

            Difficulty = difficulty;
            Range = range ?? string.Empty;
            QuestionCount = questionCount;

            // 問題数を減らしたあとに古い記録が残っていても、問題数を超えないようにする
            HasRecord = bestCorrectCount.HasValue;
            BestCorrectCount = HasRecord ? Math.Clamp(bestCorrectCount.Value, 0, questionCount) : 0;
            Rank = HasRecord ? KanjiRank.Evaluate(BestCorrectCount, questionCount, difficulty) : null;
            RankLevel = HasRecord ? KanjiRank.EvaluateLevel(BestCorrectCount, questionCount, difficulty) : -1;
            Goal = CreateGoal();

            var weak = new List<string>();
            if (weakQuestions != null)
            {
                for (int i = weakQuestions.Count - 1; i >= 0; i--) weak.Add(weakQuestions[i]);
            }
            WeakQuestions = weak;
        }

        private string CreateGoal()
        {
            if (!HasRecord) return $"全問正解で {KanjiRank.TopRank(Difficulty)}";

            if (KanjiRank.TryGetNextRank(BestCorrectCount, QuestionCount, Difficulty, out int more, out string next))
            {
                return $"あと{more}問で {next}！";
            }
            return "全問正解！";
        }
    }
}
