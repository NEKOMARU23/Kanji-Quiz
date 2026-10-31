using System.Collections.Generic;
using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Title;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class ModeSelectPresenterTests
    {
        private readonly List<QuizModeDefinition> created = new List<QuizModeDefinition>();
        private FakeModeSelectView view;
        private FakeSceneLoader sceneLoader;
        private FakeAudioPlayer audio;
        private FakeBestRecordRepository best;
        private ModeSelectPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            QuizLaunchOptions.Clear();
            view = new FakeModeSelectView();
            sceneLoader = new FakeSceneLoader();
            audio = new FakeAudioPlayer();
            best = new FakeBestRecordRepository();
        }

        [TearDown]
        public void TearDown()
        {
            presenter?.Dispose();
            presenter = null;
            foreach (var mode in created) Object.DestroyImmediate(mode);
            created.Clear();
            QuizLaunchOptions.Clear();
        }

        private QuizModeDefinition CreateMode(string id, bool isAvailable = true)
        {
            var mode = QuizModeDefinition.Create(id, id, id + "Scene", isAvailable: isAvailable);
            created.Add(mode);
            return mode;
        }

        /// <summary>時間割：0 準備中、1 StrokeOrder、2 RealKanji、3 準備中</summary>
        private void CreatePresenter()
        {
            var modes = new[] { CreateMode("Idiom", isAvailable: false), CreateMode("StrokeOrder"), CreateMode("RealKanji"), CreateMode("Image", isAvailable: false) };
            var summaries = ModeSummary.Load(modes, best, new FakeWeakQuestionRepository());
            presenter = new ModeSelectPresenter(summaries, view, sceneLoader, audio);
            presenter.Initialize();
        }

        [Test]
        public void 開くまでは画面を見せない()
        {
            CreatePresenter();

            Assert.IsFalse(view.IsVisible);
            Assert.AreEqual(4, view.Timetable.Count);
        }

        [Test]
        public void 開くと遊べる一番上のモードを選ぶ()
        {
            CreatePresenter();
            presenter.Open();

            Assert.IsTrue(view.IsVisible);
            Assert.AreEqual(1, view.SelectedMode);
            Assert.IsTrue(view.CanAttend);
        }

        [Test]
        public void 準備中のモードは選べるが授業を受けられない()
        {
            CreatePresenter();
            presenter.Open();

            view.ModeClicked.OnNext(3);
            Assert.AreEqual(3, view.SelectedMode);
            Assert.IsFalse(view.CanAttend);

            view.AttendClicked.OnNext(Unit.Default);
            Assert.IsFalse(view.IsBoardShown);
        }

        [Test]
        public void 上下のキーで選択が動き端で止まる()
        {
            CreatePresenter();
            presenter.Open();

            view.ModeMoved.OnNext(-1);
            view.ModeMoved.OnNext(-1);
            Assert.AreEqual(0, view.SelectedMode);

            for (int i = 0; i < 5; i++) view.ModeMoved.OnNext(1);
            Assert.AreEqual(3, view.SelectedMode);
            Assert.Contains(SeType.Select, audio.PlayedSe);
        }

        [Test]
        public void 授業を受けると黒板を広げまだ全問正解していない難易度を選ぶ()
        {
            best.Save("StrokeOrder_Easy", 5); // 簡単は全問正解
            CreatePresenter();
            presenter.Open();

            view.AttendClicked.OnNext(Unit.Default);

            Assert.IsTrue(view.IsBoardShown);
            Assert.AreEqual("StrokeOrder", view.BoardMode.Definition.ModeId);
            Assert.AreEqual(1, view.SelectedDifficulty);
            Assert.AreEqual("常識ではじめる", view.StartLabel);
        }

        [Test]
        public void 黒板を開いている間は時間割の操作を受け付けない()
        {
            CreatePresenter();
            presenter.Open();
            view.AttendClicked.OnNext(Unit.Default);

            view.ModeClicked.OnNext(2);
            view.BackToTitleClicked.OnNext(Unit.Default);

            Assert.AreEqual(1, view.SelectedMode);
            Assert.IsTrue(view.IsVisible);
        }

        [Test]
        public void 難易度を選んではじめると難易度を渡してシーンを開く()
        {
            CreatePresenter();
            presenter.Open();
            view.ModeClicked.OnNext(2);
            view.AttendClicked.OnNext(Unit.Default);

            view.DifficultyMoved.OnNext(1);
            view.DifficultyMoved.OnNext(1);
            view.DifficultyMoved.OnNext(1);
            Assert.AreEqual(2, view.SelectedDifficulty);
            Assert.AreEqual("困難ではじめる", view.StartLabel);

            view.StartClicked.OnNext(Unit.Default);
            view.StartClicked.OnNext(Unit.Default);

            Assert.AreEqual(Difficulty.Hard, QuizLaunchOptions.Difficulty);
            CollectionAssert.AreEqual(new[] { "RealKanjiScene" }, sceneLoader.LoadedScenes);
        }

        [Test]
        public void 時間割へ戻ると同じモードを選んだまま黒板を閉じる()
        {
            CreatePresenter();
            presenter.Open();
            view.ModeClicked.OnNext(2);
            view.AttendClicked.OnNext(Unit.Default);

            view.BackToTimetableClicked.OnNext(Unit.Default);

            Assert.IsFalse(view.IsBoardShown);
            Assert.AreEqual(2, view.SelectedMode);
            view.StartClicked.OnNext(Unit.Default);
            Assert.AreEqual(0, sceneLoader.LoadedScenes.Count);
        }

        [Test]
        public void タイトルへ戻ると画面を隠して知らせる()
        {
            CreatePresenter();
            presenter.Open();
            int count = 0;
            presenter.OnBackToTitle.Subscribe(_ => count++);

            view.BackToTitleClicked.OnNext(Unit.Default);

            Assert.IsFalse(view.IsVisible);
            Assert.AreEqual(1, count);
        }
    }
}
