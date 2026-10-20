using System;
using System.Collections.Generic;
using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 1回のプレイの進行状況（問題リスト、現在位置、回答履歴、スコア）を管理する
    /// </summary>
    /// <typeparam name="TQuestion">問題の型</typeparam>
    public sealed class QuizSession<TQuestion> : IDisposable
    {
        private readonly List<TQuestion> questions;
        private readonly AnswerRecord[] answers;
        private readonly ReactiveProperty<int> currentIndex = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int> score = new ReactiveProperty<int>(0);

        /// <summary>出題する問題のリスト</summary>
        public IReadOnlyList<TQuestion> Questions => questions;

        /// <summary>問題ごとの回答の記録</summary>
        public IReadOnlyList<AnswerRecord> Answers => answers;

        /// <summary>現在の問題の番号（0 始まり）</summary>
        public ReadOnlyReactiveProperty<int> CurrentIndex => currentIndex;

        /// <summary>正解した問題の数</summary>
        public ReadOnlyReactiveProperty<int> Score => score;

        /// <summary>問題の数</summary>
        public int Count => questions.Count;

        /// <summary>現在の問題</summary>
        public TQuestion Current => questions[currentIndex.Value];

        /// <summary>現在の問題に対する回答の記録</summary>
        public AnswerRecord CurrentAnswer => answers[currentIndex.Value];

        /// <summary>現在の問題が最初の問題かどうか</summary>
        public bool IsFirst => currentIndex.Value == 0;

        /// <summary>現在の問題が最後の問題かどうか</summary>
        public bool IsLast => currentIndex.Value == questions.Count - 1;

        public QuizSession(IEnumerable<TQuestion> questions)
        {
            if (questions == null) throw new ArgumentNullException(nameof(questions));

            this.questions = new List<TQuestion>(questions);
            if (this.questions.Count == 0)
            {
                throw new ArgumentException("問題が1問もありません。", nameof(questions));
            }

            answers = new AnswerRecord[this.questions.Count];
            for (int i = 0; i < answers.Length; i++)
            {
                answers[i] = AnswerRecord.Unanswered;
            }
        }

        /// <summary>
        /// 現在の問題に対する回答を記録する
        /// 同じ問題に回答し直した場合は上書きする
        /// </summary>
        public void RecordAnswer(int selectedIndex, bool isCorrect)
        {
            answers[currentIndex.Value] = AnswerRecord.Create(selectedIndex, isCorrect);
            score.Value = CountCorrect();
        }

        /// <summary>
        /// 次の問題へ進む
        /// </summary>
        /// <returns>進めた場合は true。最後の問題のときは false</returns>
        public bool MoveNext()
        {
            if (IsLast) return false;
            currentIndex.Value++;
            return true;
        }

        /// <summary>
        /// 前の問題へ戻る
        /// </summary>
        /// <returns>戻れた場合は true。最初の問題のときは false</returns>
        public bool MovePrev()
        {
            if (IsFirst) return false;
            currentIndex.Value--;
            return true;
        }

        public void Dispose()
        {
            currentIndex.Dispose();
            score.Dispose();
        }

        private int CountCorrect()
        {
            int count = 0;
            foreach (var answer in answers)
            {
                if (answer.State == AnswerState.Correct) count++;
            }
            return count;
        }
    }
}
