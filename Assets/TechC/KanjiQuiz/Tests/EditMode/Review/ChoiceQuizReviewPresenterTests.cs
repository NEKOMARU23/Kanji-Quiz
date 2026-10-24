using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class ChoiceQuizReviewPresenterTests
    {
        private FakeChoiceQuestion[] questions;
        private AnswerRecord[] answers;
        private FakeQuizFrameView frameView;
        private FakeChoiceListView choiceListView;
        private FakeQuizReviewView reviewView;
        private FakeSceneLoader sceneLoader;

        [SetUp]
        public void SetUp()
        {
            // 正解は 0番、1番、2番。回答は 正解、不正解（2番を選んだ）、未回答
            questions = new[]
            {
                new FakeChoiceQuestion(3, 0),
                new FakeChoiceQuestion(3, 1),
                new FakeChoiceQuestion(3, 2),
            };
            answers = new[]
            {
                AnswerRecord.Create(0, true),
                AnswerRecord.Create(2, false),
                AnswerRecord.Unanswered,
            };
            frameView = new FakeQuizFrameView();
            choiceListView = new FakeChoiceListView();
            reviewView = new FakeQuizReviewView();
            sceneLoader = new FakeSceneLoader();
        }

        private TestChoiceQuizReviewPresenter Create(int startIndex)
        {
            var presenter = new TestChoiceQuizReviewPresenter(questions, answers, startIndex, frameView, choiceListView, reviewView, sceneLoader);
            presenter.Initialize();
            return presenter;
        }

        [Test]
        public void 頼まれた問題を印つきで表示し選択肢は押せない()
        {
            using (var presenter = Create(1))
            {
                Assert.AreSame(questions[1], presenter.ShownQuestions[0]);
                Assert.AreEqual(2, frameView.ProgressCurrent);
                Assert.AreEqual(1, choiceListView.MarkedCorrectIndex);
                Assert.AreEqual(2, choiceListView.MarkedSelectedIndex);
                Assert.AreEqual(2, choiceListView.SelectedIndex);
                Assert.IsFalse(choiceListView.IsInteractable);
                Assert.IsTrue(reviewView.IsVisible);
                Assert.AreEqual(AnswerState.Incorrect, reviewView.AnswerState);
            }
        }

        [Test]
        public void 次へと前へで問題を切り替える()
        {
            using (var presenter = Create(0))
            {
                Assert.IsFalse(frameView.IsPrevInteractable);
                Assert.IsTrue(frameView.IsNextInteractable);

                frameView.ClickNext();
                Assert.AreEqual(1, presenter.CurrentIndex);

                frameView.ClickNext();
                Assert.AreEqual(2, presenter.CurrentIndex);
                Assert.IsFalse(frameView.IsNextInteractable);

                frameView.ClickNext();
                Assert.AreEqual(2, presenter.CurrentIndex, "最後の問題より先へは進まない");

                frameView.ClickPrev();
                Assert.AreEqual(1, presenter.CurrentIndex);
            }
        }

        [Test]
        public void 未回答の問題は選択なしで正解だけに印をつける()
        {
            using (Create(2))
            {
                Assert.AreEqual(-1, choiceListView.SelectedIndex);
                Assert.AreEqual(2, choiceListView.MarkedCorrectIndex);
                Assert.AreEqual(-1, choiceListView.MarkedSelectedIndex);
                Assert.AreEqual(AnswerState.Unanswered, reviewView.AnswerState);
            }
        }

        [Test]
        public void 結果に戻るを押すと結果画面を読み込む()
        {
            using (Create(0))
            {
                reviewView.ClickBack();
            }

            CollectionAssert.AreEqual(new[] { "Result" }, sceneLoader.LoadedScenes);
        }
    }
}
