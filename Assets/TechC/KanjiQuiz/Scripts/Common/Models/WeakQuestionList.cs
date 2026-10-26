using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 苦手な問題（間違えた問題）のリスト
    /// 間違えた問題を追加し、正解した問題を外す。次のプレイで優先して出題するのに使う
    /// 問題は文字列（書き順クイズなら漢字）で表す
    /// </summary>
    public sealed class WeakQuestionList
    {
        /// <summary>覚えておく問題の数の上限。超えたら古いものから外す</summary>
        public const int MaxCount = 30;

        private readonly List<string> items;

        /// <summary>苦手な問題。古い順</summary>
        public IReadOnlyList<string> Items => items;

        /// <summary>苦手な問題の数</summary>
        public int Count => items.Count;

        public WeakQuestionList(IEnumerable<string> items = null)
        {
            this.items = new List<string>();
            if (items == null) return;

            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item) && !this.items.Contains(item)) this.items.Add(item);
            }
            TrimToMax();
        }

        /// <summary>苦手な問題かどうか</summary>
        public bool Contains(string question)
        {
            return items.Contains(question);
        }

        /// <summary>
        /// 1問の結果を記録する。間違えたら追加（すでにあれば一番新しくする）、正解したら外す
        /// 未回答は記録しない
        /// </summary>
        public void Record(string question, AnswerState state)
        {
            if (string.IsNullOrEmpty(question)) throw new ArgumentException("問題が空です。", nameof(question));
            if (state == AnswerState.Unanswered) return;

            items.Remove(question);
            if (state == AnswerState.Incorrect)
            {
                items.Add(question);
                TrimToMax();
            }
        }

        /// <summary>
        /// 1回のプレイの結果をまとめて記録する。結果の Label を問題として扱う
        /// </summary>
        public void RecordResult(QuizResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            foreach (var item in result.Items)
            {
                Record(item.Label, item.State);
            }
        }

        /// <summary>
        /// 出題範囲の並べ方を決める。苦手な問題（出題範囲にあるもの）からランダムに最大 maxWeakCount 個を先頭に置き、
        /// 残りをランダムに並べる。先頭から順に問題を作れば、苦手な問題が優先して出る
        /// </summary>
        /// <param name="candidates">出題範囲</param>
        /// <param name="maxWeakCount">先頭に置く苦手な問題の数の上限</param>
        public List<string> OrderCandidates(IReadOnlyList<string> candidates, int maxWeakCount)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (maxWeakCount < 0) throw new ArgumentOutOfRangeException(nameof(maxWeakCount));

            var weak = new List<string>();
            var others = new List<string>();
            foreach (var candidate in candidates)
            {
                if (Contains(candidate)) weak.Add(candidate);
                else others.Add(candidate);
            }

            ShuffleUtility.Shuffle(weak);
            int takeCount = Math.Min(maxWeakCount, weak.Count);
            var ordered = weak.GetRange(0, takeCount);

            // 先頭に置かなかった苦手な問題も、ふつうの候補として混ぜる
            others.AddRange(weak.GetRange(takeCount, weak.Count - takeCount));
            ShuffleUtility.Shuffle(others);
            ordered.AddRange(others);
            return ordered;
        }

        private void TrimToMax()
        {
            if (items.Count > MaxCount) items.RemoveRange(0, items.Count - MaxCount);
        }
    }
}
