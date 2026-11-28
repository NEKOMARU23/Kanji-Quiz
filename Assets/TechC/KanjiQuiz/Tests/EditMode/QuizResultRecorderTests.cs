using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class QuizResultRecorderTests
    {
        private QuizModeDefinition mode;
        private FakeWeakQuestionRepository weakQuestions;
        private FakeKanjiBookRepository kanjiBook;
        private QuizSession<string> session;

        [SetUp]
        public void SetUp()
        {
            QuizResultStore.Clear();
            QuizReviewStore.Clear();
            mode = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrderScene");
            weakQuestions = new FakeWeakQuestionRepository();
            kanjiBook = new FakeKanjiBookRepository();

            // 1問目は正解、2問目は不正解、3問目は未回答
            session = new QuizSession<string>(new[] { "海", "山", "川" });
            session.RecordAnswer(0, true);
            session.MoveNext();
            session.RecordAnswer(1, false);
        }

        [TearDown]
        public void TearDown()
        {
            session.Dispose();
            Object.DestroyImmediate(mode);
            QuizResultStore.Clear();
            QuizReviewStore.Clear();
        }

        [Test]
        public void モードと難易度に合った結果を置く()
        {
            var result = Record(new WeakQuestionList());

            Assert.AreSame(result, QuizResultStore.Last);
            Assert.IsTrue(QuizResultStore.IsRevealPending);
            Assert.AreEqual(mode.Title(Difficulty.Hard), result.ModeName);
            Assert.AreEqual("StrokeOrderScene", result.RetrySceneName);
            Assert.AreEqual("StrokeOrderScene", result.ReviewSceneName);
            Assert.AreEqual(Difficulty.Hard, result.Difficulty);
            Assert.AreEqual("StrokeOrder_Hard", result.RecordKey);
            CollectionAssert.AreEqual(new[] { "海", "山", "川" }, result.Items.Select(item => item.Label).ToArray());
        }

        [Test]
        public void 振り返りに使う問題と回答を置く()
        {
            Record(new WeakQuestionList());
            QuizReviewStore.Request(1);

            Assert.IsTrue(QuizReviewStore.TryTakeRequest<string>(out var questions, out var answers, out int index));
            CollectionAssert.AreEqual(new[] { "海", "山", "川" }, questions);
            Assert.AreEqual(AnswerState.Incorrect, answers[1].State);
            Assert.AreEqual(1, index);
        }

        [Test]
        public void 間違いは苦手に加え_正解は外し_未回答は変えない()
        {
            var weakList = new WeakQuestionList(new[] { "海", "川" });

            Record(weakList);

            var saved = weakQuestions.Load("StrokeOrder_Hard").Items;
            CollectionAssert.AreEquivalent(new[] { "川", "山" }, saved);
            CollectionAssert.AreEquivalent(saved, weakList.Items, "渡した苦手のリストも更新する");
        }

        [Test]
        public void 正解したものだけを_そのモードの漢字帳に足す()
        {
            kanjiBook.Save("StrokeOrder", new[] { "空" });

            Record(new WeakQuestionList());

            CollectionAssert.AreEqual(new[] { "空", "海" }, kanjiBook.Load("StrokeOrder"));
        }

        private QuizResult Record(WeakQuestionList weakList)
        {
            return new QuizResultRecorder(weakQuestions, kanjiBook)
                .Record(mode, Difficulty.Hard, session, question => question, weakList);
        }
    }
}
