using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.ReadingQuiz;

namespace TechC.KanjiQuiz.Tests
{
    public class ReadingQuizReviewPresenterTests
    {
        private FakeQuizFrameView frameView;
        private FakeReadingQuizView view;
        private FakeQuizReviewView reviewView;
        private FakeSceneLoader sceneLoader;
        private ReadingQuizQuestion[] questions;
        private AnswerRecord[] answers;
        private ReadingQuizReviewPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            // 1問目：単一。正しいのは番号1の「あした」。プレイヤーは番号0を選んで不正解
            // 2問目：複数。正しいのは番号0と2。プレイヤーは両方選んで正解
            questions = new[]
            {
                new ReadingQuizQuestion("明日", new[] { "みょうにち", "あした", "あす" }, 1 << 1),
                new ReadingQuizQuestion("今日", new[] { "きょう", "こんじつ", "こんにち", "ほんじつ", "こんち" }, (1 << 0) | (1 << 2)),
            };
            answers = new[]
            {
                AnswerRecord.Create(1 << 0, isCorrect: false),
                AnswerRecord.Create((1 << 0) | (1 << 2), isCorrect: true),
            };
            frameView = new FakeQuizFrameView();
            view = new FakeReadingQuizView();
            reviewView = new FakeQuizReviewView();
            sceneLoader = new FakeSceneLoader();
            presenter = new ReadingQuizReviewPresenter(questions, answers, 0, frameView, view, reviewView, sceneLoader, "Result");
            presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
        }

        [Test]
        public void 開くと最初の問題と正誤が出て_選択肢は押せない()
        {
            Assert.IsTrue(reviewView.IsVisible);
            Assert.AreEqual("明日", view.Word);
            Assert.AreEqual(AnswerState.Incorrect, reviewView.AnswerState);
            Assert.IsFalse(view.OptionsInteractable);
        }

        [Test]
        public void 正しい読みには正解の印_選んだ読みには選んだ印が付く()
        {
            Assert.IsFalse(view.Correct[0]);
            Assert.IsTrue(view.Correct[1]);
            Assert.IsTrue(view.Selected[0], "プレイヤーが選んだのは番号0");
            Assert.IsFalse(view.Selected[1]);
        }

        [Test]
        public void 案内には正しい読みを出す()
        {
            Assert.AreEqual("{正|ただ}しい{読|よ}み：あした", view.Hint);
        }

        [Test]
        public void 次へで複数回答の問題を見せると_両方の正解が印される()
        {
            frameView.ClickNext();

            Assert.AreEqual("今日", view.Word);
            Assert.AreEqual(AnswerState.Correct, reviewView.AnswerState);
            Assert.IsTrue(view.Correct[0]);
            Assert.IsTrue(view.Correct[2]);
            Assert.IsFalse(view.Correct[1]);
            Assert.AreEqual("{正|ただ}しい{読|よ}み：きょう、こんにち", view.Hint);
        }

        [Test]
        public void 前の問題にも戻れる()
        {
            frameView.ClickNext();
            frameView.ClickPrev();

            Assert.AreEqual(0, presenter.CurrentIndex);
            Assert.AreEqual("明日", view.Word);
        }

        [Test]
        public void 結果に戻るで結果の画面へ移る()
        {
            reviewView.ClickBack();

            CollectionAssert.AreEqual(new[] { "Result" }, sceneLoader.LoadedScenes.ToArray());
        }
    }
}
