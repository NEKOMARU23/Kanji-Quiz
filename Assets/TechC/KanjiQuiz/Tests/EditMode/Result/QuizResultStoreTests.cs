using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class QuizResultStoreTests
    {
        [SetUp]
        public void SetUp() => QuizResultStore.Clear();

        [TearDown]
        public void TearDown() => QuizResultStore.Clear();

        [Test]
        public void 結果を置くと演出待ちになり演出を見せたら待ちが消える()
        {
            Assert.IsFalse(QuizResultStore.IsRevealPending);

            QuizResultStore.Set(new QuizResult("テスト", "Scene", new[] { new QuizResultItem("一", AnswerState.Correct) }));
            Assert.IsTrue(QuizResultStore.IsRevealPending);

            QuizResultStore.MarkRevealed();
            Assert.IsFalse(QuizResultStore.IsRevealPending, "振り返りから戻ったときは演出しない");
            Assert.IsNotNull(QuizResultStore.Last, "結果は残っている");
        }
    }
}
