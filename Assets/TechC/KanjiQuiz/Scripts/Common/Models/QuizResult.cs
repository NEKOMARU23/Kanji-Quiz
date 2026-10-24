using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 1回のプレイの結果。プレイ画面から結果画面へ渡す
    /// </summary>
    public sealed class QuizResult
    {
        /// <summary>モードの表示名（例：書き順クイズ）</summary>
        public string ModeName { get; }

        /// <summary>[もう一度] で読み込むシーンの名前</summary>
        public string RetrySceneName { get; }

        /// <summary>問題を振り返るときに読み込むシーンの名前。振り返りに対応していなければ null</summary>
        public string ReviewSceneName { get; }

        /// <summary>問題を振り返れるかどうか</summary>
        public bool CanReview => !string.IsNullOrEmpty(ReviewSceneName);

        /// <summary>問題ごとの結果</summary>
        public IReadOnlyList<QuizResultItem> Items { get; }

        /// <summary>正解した問題の数</summary>
        public int CorrectCount { get; }

        /// <summary>問題の数</summary>
        public int Total => Items.Count;

        public QuizResult(string modeName, string retrySceneName, IReadOnlyList<QuizResultItem> items, string reviewSceneName = null)
        {
            ModeName = modeName ?? throw new ArgumentNullException(nameof(modeName));
            RetrySceneName = retrySceneName ?? throw new ArgumentNullException(nameof(retrySceneName));
            ReviewSceneName = reviewSceneName;
            Items = items ?? throw new ArgumentNullException(nameof(items));

            int correct = 0;
            foreach (var item in items)
            {
                if (item.State == AnswerState.Correct) correct++;
            }
            CorrectCount = correct;
        }

        /// <summary>
        /// [問題を振り返る] で最初に開く問題の番号
        /// 最初に間違えた（または答えていない）問題。全問正解なら 0
        /// </summary>
        public int GetFirstReviewIndex()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].State != AnswerState.Correct) return i;
            }
            return 0;
        }

        /// <summary>
        /// プレイの進行状況から結果を作る
        /// </summary>
        /// <param name="labelSelector">問題から表示用の短い文字列を取り出す</param>
        /// <param name="reviewSceneName">問題を振り返るときに読み込むシーンの名前。振り返りに対応していなければ null</param>
        public static QuizResult FromSession<TQuestion>(
            string modeName,
            string retrySceneName,
            QuizSession<TQuestion> session,
            Func<TQuestion, string> labelSelector,
            string reviewSceneName = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (labelSelector == null) throw new ArgumentNullException(nameof(labelSelector));

            var items = new List<QuizResultItem>(session.Count);
            for (int i = 0; i < session.Count; i++)
            {
                items.Add(new QuizResultItem(labelSelector(session.Questions[i]), session.Answers[i].State));
            }
            return new QuizResult(modeName, retrySceneName, items, reviewSceneName);
        }
    }
}
