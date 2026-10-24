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

        [Test]
        public void 問題を押すとその問題の振り返りを頼んでモードのシーンを読み込む()
        {
            QuizReviewStore.Clear();
            var result = new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Incorrect),
            }, "StrokeOrder");

            using (var presenter = new ResultPresenter(result, view, sceneLoader))
            {
                presenter.Initialize();
                view.ClickItem(1);
            }

            Assert.IsTrue(view.IsReviewAvailable);
            Assert.AreEqual(1, QuizReviewStore.RequestedIndex);
            CollectionAssert.AreEqual(new[] { "StrokeOrder" }, sceneLoader.LoadedScenes);
            QuizReviewStore.Clear();
        }

        [Test]
        public void 問題を振り返るボタンを押すと最初に間違えた問題から振り返る()
        {
            QuizReviewStore.Clear();
            var result = new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Correct),
                new QuizResultItem("字", AnswerState.Incorrect),
            }, "StrokeOrder");

            using (var presenter = new ResultPresenter(result, view, sceneLoader))
            {
                presenter.Initialize();
                view.ClickReview();
            }

            Assert.AreEqual(2, QuizReviewStore.RequestedIndex);
            CollectionAssert.AreEqual(new[] { "StrokeOrder" }, sceneLoader.LoadedScenes);
            QuizReviewStore.Clear();
        }

        [Test]
        public void 全問正解なら問題を振り返るボタンで1問目から振り返る()
        {
            QuizReviewStore.Clear();
            var result = new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Correct),
            }, "StrokeOrder");

            using (var presenter = new ResultPresenter(result, view, sceneLoader))
            {
                presenter.Initialize();
                view.ClickReview();
            }

            Assert.AreEqual(0, QuizReviewStore.RequestedIndex);
            QuizReviewStore.Clear();
        }

        [Test]
        public void 振り返りに対応していない結果では問題を押しても何もしない()
        {
            QuizReviewStore.Clear();
            var result = new QuizResult("テスト", "Scene", new[] { new QuizResultItem("一", AnswerState.Correct) });

            using (var presenter = new ResultPresenter(result, view, sceneLoader))
            {
                presenter.Initialize();
                view.ClickItem(0);
                view.ClickReview();
            }

            Assert.IsFalse(view.IsReviewAvailable);
            Assert.IsNull(QuizReviewStore.RequestedIndex);
            Assert.IsEmpty(sceneLoader.LoadedScenes);
        }
    }
}
