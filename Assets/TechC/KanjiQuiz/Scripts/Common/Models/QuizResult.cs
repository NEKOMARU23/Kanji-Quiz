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

        /// <summary>問題ごとの結果</summary>
        public IReadOnlyList<QuizResultItem> Items { get; }

        /// <summary>正解した問題の数</summary>
        public int CorrectCount { get; }

        /// <summary>問題の数</summary>
        public int Total => Items.Count;

        public QuizResult(string modeName, string retrySceneName, IReadOnlyList<QuizResultItem> items)
        {
            ModeName = modeName ?? throw new ArgumentNullException(nameof(modeName));
            RetrySceneName = retrySceneName ?? throw new ArgumentNullException(nameof(retrySceneName));
            Items = items ?? throw new ArgumentNullException(nameof(items));

            int correct = 0;
            foreach (var item in items)
            {
                if (item.State == AnswerState.Correct) correct++;
            }
            CorrectCount = correct;
        }

        /// <summary>
        /// プレイの進行状況から結果を作る
        /// </summary>
        /// <param name="labelSelector">問題から表示用の短い文字列を取り出す</param>
        public static QuizResult FromSession<TQuestion>(
            string modeName,
            string retrySceneName,
            QuizSession<TQuestion> session,
            Func<TQuestion, string> labelSelector)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (labelSelector == null) throw new ArgumentNullException(nameof(labelSelector));

            var items = new List<QuizResultItem>(session.Count);
            for (int i = 0; i < session.Count; i++)
            {
                items.Add(new QuizResultItem(labelSelector(session.Questions[i]), session.Answers[i].State));
            }
            return new QuizResult(modeName, retrySceneName, items);
        }
    }
}
