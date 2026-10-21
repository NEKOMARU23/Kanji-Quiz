using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Result;

namespace TechC.KanjiQuiz.Tests
{
    public class ResultPresenterTests
    {
        private FakeResultView view;
        private FakeSceneLoader sceneLoader;

        [SetUp]
        public void SetUp()
        {
            view = new FakeResultView();
            sceneLoader = new FakeSceneLoader();
        }

        [Test]
        public void 結果を表示する()
        {
            var result = new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Incorrect),
            });

            using (var presenter = new ResultPresenter(result, view, sceneLoader))
            {
                presenter.Initialize();
            }

            Assert.AreEqual("書き順クイズ", view.ModeName);
            Assert.AreEqual(1, view.CorrectCount);
            Assert.AreEqual(2, view.Total);
            Assert.AreEqual(2, view.Items.Count);
            Assert.IsFalse(view.IsNoResultShown);
        }

        [Test]
        public void もう一度を押すとモードのシーンを読み込む()
        {
            var result = new QuizResult("書き順クイズ", "StrokeOrder", new[] { new QuizResultItem("令", AnswerState.Correct) });

            using (var presenter = new ResultPresenter(result, view, sceneLoader))
            {
                presenter.Initialize();
                view.ClickRetry();
            }

            CollectionAssert.AreEqual(new[] { "StrokeOrder" }, sceneLoader.LoadedScenes);
        }

        [Test]
        public void 結果がないときはその旨を表示しボタンに反応しない()
        {
            using (var presenter = new ResultPresenter(null, view, sceneLoader))
            {
                presenter.Initialize();
                view.ClickRetry();
            }

            Assert.IsTrue(view.IsNoResultShown);
            Assert.IsEmpty(sceneLoader.LoadedScenes);
        }
    }
}
