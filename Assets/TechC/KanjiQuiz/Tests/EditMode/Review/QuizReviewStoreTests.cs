using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class QuizReviewStoreTests
    {
        [SetUp]
        public void SetUp() => QuizReviewStore.Clear();

        [TearDown]
        public void TearDown() => QuizReviewStore.Clear();

        [Test]
        public void 頼まれた振り返りを受け取ると頼みは消えデータは残る()
        {
            QuizReviewStore.Set(new[] { "一", "二" }, new[] { AnswerRecord.Create(0, true), AnswerRecord.Create(1, false) });
            QuizReviewStore.Request(1);

            Assert.IsTrue(QuizReviewStore.TryTakeRequest<string>(out var questions, out var answers, out int index));
            Assert.AreEqual(1, index);
            Assert.AreEqual("二", questions[1]);
            Assert.AreEqual(AnswerState.Incorrect, answers[1].State);

            Assert.IsFalse(QuizReviewStore.TryTakeRequest<string>(out _, out _, out _), "頼みは1回だけ使える");

            QuizReviewStore.Request(0);
            Assert.IsTrue(QuizReviewStore.TryTakeRequest<string>(out _, out _, out _), "データは残っている");
        }

        [Test]
        public void 頼まれていなければ受け取れない()
        {
            QuizReviewStore.Set(new[] { "一" }, new[] { AnswerRecord.Unanswered });

            Assert.IsFalse(QuizReviewStore.TryTakeRequest<string>(out _, out _, out _));
        }

        [Test]
        public void 問題の型が違えば受け取れない()
        {
            QuizReviewStore.Set(new[] { "一" }, new[] { AnswerRecord.Unanswered });
            QuizReviewStore.Request(0);

            Assert.IsFalse(QuizReviewStore.TryTakeRequest<int>(out _, out _, out _));
        }

        [Test]
        public void 新しいデータを置くと前の頼みは取り消される()
        {
            QuizReviewStore.Set(new[] { "一" }, new[] { AnswerRecord.Unanswered });
            QuizReviewStore.Request(0);
            QuizReviewStore.Set(new[] { "二" }, new[] { AnswerRecord.Unanswered });

            Assert.IsFalse(QuizReviewStore.TryTakeRequest<string>(out _, out _, out _));
        }
    }
}
