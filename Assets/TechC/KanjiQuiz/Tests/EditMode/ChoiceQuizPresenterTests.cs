using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class ChoiceQuizPresenterTests
    {
        private QuizSession<FakeChoiceQuestion> session;
        private FakeQuizFrameView frameView;
        private FakeChoiceListView choiceListView;
        private TestChoiceQuizPresenter presenter;
        private FakeAudioPlayer audioPlayer;

        [SetUp]
        public void SetUp()
        {
            // 正解はそれぞれ 0番、1番、2番
            session = new QuizSession<FakeChoiceQuestion>(new[]
            {
                new FakeChoiceQuestion(3, 0),
                new FakeChoiceQuestion(3, 1),
                new FakeChoiceQuestion(4, 2),
            });
            frameView = new FakeQuizFrameView();
            choiceListView = new FakeChoiceListView();
            audioPlayer = new FakeAudioPlayer();
            presenter = new TestChoiceQuizPresenter(session, frameView, choiceListView, audioPlayer);
            presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
            session.Dispose();
        }

        [Test]
        public void 開始時に最初の問題が表示される()
        {
            Assert.AreEqual(1, presenter.ShowCount);
            Assert.AreEqual(1, frameView.ProgressCurrent);
            Assert.AreEqual(3, frameView.ProgressTotal);
            Assert.AreEqual(3, choiceListView.ChoiceCount);
            Assert.AreEqual(-1, choiceListView.SelectedIndex);
        }

        [Test]
        public void 最初の問題では前へを押せない()
        {
            Assert.IsFalse(frameView.IsPrevInteractable);
        }

        [Test]
        public void 選択するまで次へを押せない()
        {
            Assert.IsFalse(frameView.IsNextInteractable);

            choiceListView.Select(1);

            Assert.IsTrue(frameView.IsNextInteractable);
        }

        [Test]
        public void 未選択で次へを押しても進まない()
        {
            frameView.ClickNext();

            Assert.AreEqual(0, session.CurrentIndex.CurrentValue);
            Assert.AreEqual(AnswerState.Unanswered, session.Answers[0].State);
        }

        [Test]
        public void 正解を選んで次へを押すと正解が記録され次の問題へ進む()
        {
            choiceListView.Select(0);
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Correct, session.Answers[0].State);
            Assert.AreEqual(1, session.Score.CurrentValue);
            Assert.AreEqual(1, session.CurrentIndex.CurrentValue);
            Assert.AreEqual(2, frameView.ProgressCurrent);
            Assert.IsTrue(frameView.IsPrevInteractable);
            Assert.IsFalse(frameView.IsNextInteractable);
        }

        [Test]
        public void 不正解を選んで次へを押すと不正解が記録される()
        {
            choiceListView.Select(2);
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Incorrect, session.Answers[0].State);
            Assert.AreEqual(0, session.Score.CurrentValue);
        }

        [Test]
        public void 前の問題に戻ると以前の選択が復元される()
        {
            choiceListView.Select(2);
            frameView.ClickNext();

            frameView.ClickPrev();

            Assert.AreEqual(0, session.CurrentIndex.CurrentValue);
            Assert.AreEqual(2, choiceListView.SelectedIndex);
            Assert.IsTrue(frameView.IsNextInteractable);
        }

        [Test]
        public void 戻った問題で回答を変えると記録が上書きされる()
        {
            choiceListView.Select(2);
            frameView.ClickNext();
            frameView.ClickPrev();

            choiceListView.Select(0);
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Correct, session.Answers[0].State);
            Assert.AreEqual(1, session.Score.CurrentValue);
        }

        [Test]
        public void 最後の問題に回答して次へを押すと終了が通知される()
        {
            bool finished = false;
            using (presenter.OnFinished.Subscribe(_ => finished = true))
            {
                choiceListView.Select(0);
                frameView.ClickNext();
                choiceListView.Select(1);
                frameView.ClickNext();

                Assert.IsFalse(finished);

                choiceListView.Select(2);
                frameView.ClickNext();
            }

            Assert.IsTrue(finished);
            Assert.AreEqual(3, session.Score.CurrentValue);
            Assert.IsFalse(frameView.IsNextInteractable);
            Assert.IsFalse(frameView.IsPrevInteractable);
        }
    
        [Test]
        public void 選択肢を選ぶと選択の音が鳴る()
        {
            choiceListView.Select(1);

            CollectionAssert.AreEqual(new[] { SeType.Select }, audioPlayer.PlayedSe);
        }

        [Test]
        public void 問題を切り替えるとページをめくる音と向きに合った演出をする()
        {
            choiceListView.Select(0);
            frameView.ClickNext();
            frameView.ClickPrev();

            // 最初の問題は下から、次へは右から、前へは左から入る
            CollectionAssert.AreEqual(new[] { 0, 1, -1 }, frameView.TransitionDirections);
            CollectionAssert.AreEqual(new[] { SeType.Select, SeType.PageTurn, SeType.PageTurn }, audioPlayer.PlayedSe);
        }
    }
}
