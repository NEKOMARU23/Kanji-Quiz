using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.ReadingQuiz;

namespace TechC.KanjiQuiz.Tests
{
    public class ReadingQuizPresenterTests
    {
        private QuizSession<ReadingQuizQuestion> session;
        private FakeQuizFrameView frameView;
        private FakeReadingQuizView view;
        private FakeAudioPlayer audioPlayer;
        private ReadingQuizPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            // 1問目：正解は「あした」（番号1）だけ
            // 2問目：正解は「きょう」（番号0）と「こんにち」（番号2）の2つ
            session = new QuizSession<ReadingQuizQuestion>(new[]
            {
                new ReadingQuizQuestion("明日", new[] { "みょうにち", "あした", "あす" }, 1 << 1),
                new ReadingQuizQuestion("今日", new[] { "きょう", "こんじつ", "こんにち", "ほんじつ", "こんち" }, (1 << 0) | (1 << 2)),
            });
            frameView = new FakeQuizFrameView();
            view = new FakeReadingQuizView();
            audioPlayer = new FakeAudioPlayer();
            presenter = new ReadingQuizPresenter(session, frameView, view, audioPlayer);
            presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
            session.Dispose();
        }

        [Test]
        public void 開始時に熟語と選択肢が表示される()
        {
            Assert.AreEqual("明日", view.Word);
            Assert.AreEqual(3, view.Options.Count);
            Assert.AreEqual("正しい読みを1つ選んでください", view.Hint);
            Assert.IsTrue(view.OptionsInteractable);
        }

        [Test]
        public void 選ぶまで次へを押せない()
        {
            Assert.IsFalse(frameView.IsNextInteractable);
        }

        [Test]
        public void 一つだけの問題では押すと選び替わる()
        {
            view.Click(0);
            Assert.IsTrue(view.Selected[0]);

            view.Click(1);
            Assert.IsFalse(view.Selected[0], "1つだけの問題は、別の選択肢を押すと前の選択が外れる");
            Assert.IsTrue(view.Selected[1]);
        }

        [Test]
        public void 正しい読みを選んで次へを押すと正解になる()
        {
            view.Click(1);
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Correct, session.Answers[0].State);
            Assert.AreEqual(1, session.Score.CurrentValue);
        }

        [Test]
        public void 複数の問題では押すと選ぶか外すかが切り替わる()
        {
            GoToSecondQuestion();
            Assert.AreEqual("正しい読みを、すべて選んでください", view.Hint);

            view.Click(0);
            view.Click(2);
            Assert.IsTrue(view.Selected[0]);
            Assert.IsTrue(view.Selected[2]);

            view.Click(2);
            Assert.IsFalse(view.Selected[2], "もう一度押すと外れる");
        }

        [Test]
        public void 複数の問題で一部だけ選ぶと不正解になる()
        {
            GoToSecondQuestion();

            view.Click(0);
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Incorrect, session.Answers[1].State);
        }

        [Test]
        public void 複数の問題で正しい読みをすべて選ぶと正解になる()
        {
            GoToSecondQuestion();

            view.Click(0);
            view.Click(2);
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Correct, session.Answers[1].State);
        }

        [Test]
        public void 余分な読みを選ぶと不正解になる()
        {
            GoToSecondQuestion();

            view.Click(0);
            view.Click(1);
            view.Click(2);
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Incorrect, session.Answers[1].State);
        }

        [Test]
        public void 前の問題に戻ると前の選択が復元される()
        {
            view.Click(1);
            frameView.ClickNext();
            frameView.ClickPrev();

            Assert.IsTrue(view.Selected[1]);
        }

        /// <summary>1問目で正しい読みを選んで次へを押し、2問目を表示する</summary>
        private void GoToSecondQuestion()
        {
            view.Click(1);
            frameView.ClickNext();
        }
    }
}
