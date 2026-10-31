using System.Collections.Generic;
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
        private FakeKanjiStrokeSource strokeSource;
        private TitlePresenter presenter;

        [SetUp]
        public void SetUp()
        {
            mode = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrder");
            titleView = new FakeTitleView();
            modeSelectView = new FakeModeSelectView();
            quitter = new FakeApplicationQuitter();
            strokeSource = new FakeKanjiStrokeSource(new Dictionary<string, int> { { "漢", 13 }, { "字", 6 } });
        }

        [TearDown]
        public void TearDown()
        {
            presenter?.Dispose();
            presenter = null;
            Object.DestroyImmediate(mode);
            TitleLaunchOptions.OpenModeSelect = false;
        }

        private void CreatePresenter(bool openModeSelectFirst = false, string logoText = "漢字")
        {
            var modes = ModeSummary.Load(new[] { mode }, new FakeBestRecordRepository(), new FakeWeakQuestionRepository());
            var modeSelect = new ModeSelectPresenter(modes, modeSelectView, new FakeSceneLoader(), new FakeAudioPlayer());
            presenter = new TitlePresenter(
                titleView, modeSelect, quitter, new FakeAudioPlayer(), strokeSource, logoText, openModeSelectFirst);
            presenter.Initialize();
        }

        [Test]
        public void 最初はタイトルを見せてロゴの書き順を渡して書かせる()
        {
            CreatePresenter();

            Assert.IsTrue(titleView.IsVisible);
            Assert.IsFalse(modeSelectView.IsVisible);
            Assert.AreEqual(1, titleView.IntroCount);
            Assert.AreEqual(2, titleView.LastLogoStrokePaths.Count);
            Assert.AreEqual(13, titleView.LastLogoStrokePaths[0].Count);
            Assert.AreEqual(6, titleView.LastLogoStrokePaths[1].Count);
            Assert.IsTrue(presenter.IsMenuReady);
        }

        [Test]
        public void 書き順のデータがない字は空のリストを渡す()
        {
            CreatePresenter(logoText: "漢ク");

            Assert.AreEqual(0, titleView.LastLogoStrokePaths[1].Count);
        }

        [Test]
        public void はじめるで黒板を消してからモード選択を演出つきで開く()
        {
            CreatePresenter();

            titleView.StartClicked.OnNext(Unit.Default);

            Assert.AreEqual(1, titleView.ExitCount);
            Assert.IsFalse(titleView.IsVisible);
            Assert.IsTrue(modeSelectView.IsVisible);
            Assert.IsTrue(modeSelectView.LastShownFromTitle);
        }

        [Test]
        public void 黒板を消すのを飛ばしたらモード選択の演出も飛ばす()
        {
            titleView.SkipExit = true;
            CreatePresenter();

            titleView.StartClicked.OnNext(Unit.Default);

            Assert.IsTrue(modeSelectView.IsVisible);
            Assert.IsFalse(modeSelectView.LastShownFromTitle);
        }

        [Test]
        public void ロゴを書いている間はメニューに反応しない()
        {
            titleView.HoldIntro = true;
            CreatePresenter();

            titleView.StartClicked.OnNext(Unit.Default);
            titleView.QuitClicked.OnNext(Unit.Default);

            Assert.IsFalse(presenter.IsMenuReady);
            Assert.AreEqual(0, titleView.ExitCount);
            Assert.AreEqual(0, quitter.QuitCount);
        }

        [Test]
        public void モード選択からタイトルへ戻るとロゴをもう一度書く()
        {
            CreatePresenter();
            titleView.StartClicked.OnNext(Unit.Default);

            modeSelectView.BackToTitleClicked.OnNext(Unit.Default);

            Assert.IsTrue(titleView.IsVisible);
            Assert.IsFalse(modeSelectView.IsVisible);
            Assert.AreEqual(2, titleView.IntroCount);
        }

        [Test]
        public void モード選択から始める設定ならタイトルを飛ばして演出なしで開く()
        {
            CreatePresenter(openModeSelectFirst: true);

            Assert.IsFalse(titleView.IsVisible);
            Assert.IsTrue(modeSelectView.IsVisible);
            Assert.IsFalse(modeSelectView.LastShownFromTitle);
            Assert.AreEqual(0, titleView.IntroCount);
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
            titleView.StartClicked.OnNext(Unit.Default);

            Assert.AreEqual(0, quitter.QuitCount);
            Assert.AreEqual(1, titleView.ExitCount);
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
