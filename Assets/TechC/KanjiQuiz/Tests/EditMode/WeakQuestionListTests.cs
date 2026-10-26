using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class WeakQuestionListTests
    {
        [SetUp]
        public void SetUp() => UnityEngine.Random.InitState(42);

        [Test]
        public void 間違えた問題を加え正解した問題を外す()
        {
            var list = new WeakQuestionList();

            list.Record("令", AnswerState.Incorrect);
            list.Record("漢", AnswerState.Incorrect);
            list.Record("令", AnswerState.Correct);

            CollectionAssert.AreEqual(new[] { "漢" }, list.Items);
        }

        [Test]
        public void もう一度間違えた問題は一番新しくなり重複しない()
        {
            var list = new WeakQuestionList(new[] { "令", "漢" });

            list.Record("令", AnswerState.Incorrect);

            CollectionAssert.AreEqual(new[] { "漢", "令" }, list.Items);
        }

        [Test]
        public void 未回答は記録しない()
        {
            var list = new WeakQuestionList(new[] { "令" });

            list.Record("令", AnswerState.Unanswered);
            list.Record("漢", AnswerState.Unanswered);

            CollectionAssert.AreEqual(new[] { "令" }, list.Items);
        }

        [Test]
        public void 上限を超えたら古いものから外す()
        {
            var list = new WeakQuestionList();
            for (int i = 0; i < WeakQuestionList.MaxCount + 2; i++)
            {
                list.Record(i.ToString(), AnswerState.Incorrect);
            }

            Assert.AreEqual(WeakQuestionList.MaxCount, list.Count);
            Assert.IsFalse(list.Contains("0"));
            Assert.IsFalse(list.Contains("1"));
            Assert.IsTrue(list.Contains("2"));
        }

        [Test]
        public void 結果をまとめて記録する()
        {
            var list = new WeakQuestionList(new[] { "令" });
            var result = new QuizResult("テスト", "Scene", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Incorrect),
            });

            list.RecordResult(result);

            CollectionAssert.AreEqual(new[] { "漢" }, list.Items);
        }

        [Test]
        public void 出題範囲にある苦手な問題を上限まで先頭に置く()
        {
            var list = new WeakQuestionList(new[] { "令", "漢", "字", "範囲外" });
            var candidates = new[] { "一", "二", "三", "令", "漢", "字" };

            var ordered = list.OrderCandidates(candidates, 2);

            Assert.AreEqual(candidates.Length, ordered.Count, "出題範囲にない苦手な問題は加えない");
            CollectionAssert.AreEquivalent(candidates, ordered);
            Assert.IsTrue(ordered.Take(2).All(list.Contains), "先頭の2つは苦手な問題");
        }

        [Test]
        public void 苦手な問題がなければ出題範囲をそのまま混ぜる()
        {
            var candidates = new[] { "一", "二", "三" };

            var ordered = new WeakQuestionList().OrderCandidates(candidates, 2);

            CollectionAssert.AreEquivalent(candidates, ordered);
        }
    }
}
