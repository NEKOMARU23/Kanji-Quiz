using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Title;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class TitlePresenterTests
    {
        private QuizModeDefinition mode;
        private FakeTitleView titleView;
        private FakeModeSelectView modeSelectView;
        private FakeApplicationQuitter quitter;
        private TitlePresenter presenter;

        [SetUp]
        public void SetUp()
        {
            mode = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrder");
            titleView = new FakeTitleView();
            modeSelectView = new FakeModeSelectView();
            quitter = new FakeApplicationQuitter();
        }

        [TearDown]
        public void TearDown()
        {
            presenter?.Dispose();
            Object.DestroyImmediate(mode);
        }

        private void CreatePresenter(bool openModeSelectFirst = false)
        {
            var modes = ModeSummary.Load(new[] { mode }, new FakeBestRecordRepository(), new FakeWeakQuestionRepository());
            var modeSelect = new ModeSelectPresenter(modes, modeSelectView, new FakeSceneLoader(), new FakeAudioPlayer());
            presenter = new TitlePresenter(titleView, modeSelect, quitter, new FakeAudioPlayer(), openModeSelectFirst);
            presenter.Initialize();
        }

        [Test]
        public void 最初はタイトルを見せてモード選択は隠す()
        {
            CreatePresenter();

            Assert.IsTrue(titleView.IsVisible);
            Assert.IsFalse(modeSelectView.IsVisible);
        }

        [Test]
        public void はじめるでモード選択に切り替わる()
        {
            CreatePresenter();

            titleView.StartClicked.OnNext(Unit.Default);

            Assert.IsFalse(titleView.IsVisible);
            Assert.IsTrue(modeSelectView.IsVisible);
        }

        [Test]
        public void モード選択からタイトルへ戻れる()
        {
            CreatePresenter();
            titleView.StartClicked.OnNext(Unit.Default);

            modeSelectView.BackToTitleClicked.OnNext(Unit.Default);

            Assert.IsTrue(titleView.IsVisible);
            Assert.IsFalse(modeSelectView.IsVisible);
        }

        [Test]
        public void モード選択から始める設定ならタイトルを飛ばす()
        {
            CreatePresenter(openModeSelectFirst: true);

            Assert.IsFalse(titleView.IsVisible);
            Assert.IsTrue(modeSelectView.IsVisible);
        }

        [Test]
        public void おわるでアプリを終わらせる()
        {
            CreatePresenter();

            titleView.QuitClicked.OnNext(Unit.Default);

            Assert.AreEqual(1, quitter.QuitCount);
        }

        [Test]
        public void モード選択を開いている間はタイトルのボタンに反応しない()
        {
            CreatePresenter();
            titleView.StartClicked.OnNext(Unit.Default);

            titleView.QuitClicked.OnNext(Unit.Default);

            Assert.AreEqual(0, quitter.QuitCount);
        }

        [Test]
        public void モード選択から始める設定は1回だけ効く()
        {
            TitleLaunchOptions.OpenModeSelect = true;

            Assert.IsTrue(TitleLaunchOptions.TakeOpenModeSelect());
            Assert.IsFalse(TitleLaunchOptions.TakeOpenModeSelect());
        }
    }
}
