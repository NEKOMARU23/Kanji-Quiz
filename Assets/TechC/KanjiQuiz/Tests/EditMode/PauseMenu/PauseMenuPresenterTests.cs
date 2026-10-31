using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class PauseMenuPresenterTests
    {
        private FakePauseMenuView view;
        private FakeSceneLoader sceneLoader;
        private FakeAudioPlayer audioPlayer;
        private FakeVolumeRepository volumeRepository;
        private PauseMenuPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            view = new FakePauseMenuView();
            sceneLoader = new FakeSceneLoader();
            audioPlayer = new FakeAudioPlayer();
            volumeRepository = new FakeVolumeRepository { SeVolume = 0.4f };
            presenter = new PauseMenuPresenter(view, sceneLoader, audioPlayer, volumeRepository, "StrokeOrder");
            presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
            TitleLaunchOptions.OpenModeSelect = false;
        }

        [Test]
        public void 保存した音量をつまみに反映する()
        {
            Assert.AreEqual(0.4f, view.SliderVolume);
            Assert.IsFalse(view.IsShown);
        }

        [Test]
        public void メニューボタンで開き続けるで閉じる()
        {
            view.ClickOpen();
            Assert.IsTrue(presenter.IsOpen);
            Assert.IsTrue(view.IsShown);

            view.ClickResume();
            Assert.IsFalse(presenter.IsOpen);
            Assert.IsFalse(view.IsShown);
        }

        [Test]
        public void メニューのキーで開いたり閉じたりする()
        {
            view.PressMenuKey();
            Assert.IsTrue(view.IsShown);

            view.PressMenuKey();
            Assert.IsFalse(view.IsShown);
        }

        [Test]
        public void 最初からやり直すとメニューを閉じて今のシーンを読み込み直す()
        {
            view.ClickOpen();
            view.ClickRestart();

            Assert.IsFalse(view.IsShown);
            CollectionAssert.AreEqual(new[] { "StrokeOrder" }, sceneLoader.LoadedScenes);
        }

        [Test]
        public void やめるとメニューを閉じてモード選択から始まるタイトルのシーンを読み込む()
        {
            view.ClickOpen();
            view.ClickQuit();

            Assert.IsFalse(view.IsShown);
            Assert.IsTrue(TitleLaunchOptions.OpenModeSelect);
            CollectionAssert.AreEqual(new[] { SceneNames.Title }, sceneLoader.LoadedScenes);
        }

        [Test]
        public void 音量を変えると効果音に反映して保存する()
        {
            view.MoveVolume(0.25f);

            Assert.AreEqual(0.25f, audioPlayer.SeVolume);
            Assert.AreEqual(0.25f, volumeRepository.SeVolume);
        }

        [Test]
        public void 開いているときにもう一度開いても音は1回だけ鳴る()
        {
            view.ClickOpen();
            view.ClickOpen();

            CollectionAssert.AreEqual(new[] { SeType.Select }, audioPlayer.PlayedSe);
        }
    }
}
