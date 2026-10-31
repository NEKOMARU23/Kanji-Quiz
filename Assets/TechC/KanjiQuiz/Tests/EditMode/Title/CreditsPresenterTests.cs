using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    public class CreditsPresenterTests
    {
        private FakeCreditsView view;
        private FakeAudioPlayer audioPlayer;
        private CreditsPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            view = new FakeCreditsView();
            audioPlayer = new FakeAudioPlayer();
            presenter = new CreditsPresenter(view, audioPlayer);
            presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
        }

        [Test]
        public void 最初は隠しておき開くと見せる()
        {
            Assert.IsFalse(view.IsVisible);

            presenter.Open();

            Assert.IsTrue(view.IsVisible);
        }

        [Test]
        public void タイトルへで閉じて閉じたことを知らせる()
        {
            int closedCount = 0;
            presenter.OnClosed.Subscribe(_ => closedCount++);
            presenter.Open();

            view.BackClicked.OnNext(Unit.Default);

            Assert.IsFalse(view.IsVisible);
            Assert.AreEqual(1, closedCount);
            CollectionAssert.Contains(audioPlayer.PlayedSe, SeType.Select);
        }

        [Test]
        public void 開いていないときのタイトルへは無視する()
        {
            int closedCount = 0;
            presenter.OnClosed.Subscribe(_ => closedCount++);

            view.BackClicked.OnNext(Unit.Default);

            Assert.AreEqual(0, closedCount);
        }
    }
}
