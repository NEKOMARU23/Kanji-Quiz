using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Title;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class SettingsPresenterTests
    {
        private QuizModeDefinition mode;
        private FakeSettingsView view;
        private FakeAudioPlayer audioPlayer;
        private FakeVolumeRepository volumeRepository;
        private FakeBgmPlayer bgmPlayer;
        private FakeDisplaySettingRepository displayRepository;
        private DisplaySettings displaySettings;
        private FakeBestRecordRepository bestRecords;
        private FakeWeakQuestionRepository weakQuestions;
        private SettingsPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            mode = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrder");
            view = new FakeSettingsView();
            audioPlayer = new FakeAudioPlayer();
            bgmPlayer = new FakeBgmPlayer();
            volumeRepository = new FakeVolumeRepository { SeVolume = 0.4f, BgmVolume = 0.3f };
            displayRepository = new FakeDisplaySettingRepository { UseHiragana = true };
            displaySettings = new DisplaySettings(displayRepository);
            bestRecords = new FakeBestRecordRepository();
            weakQuestions = new FakeWeakQuestionRepository();
            var eraser = new QuizRecordEraser(new[] { mode }, bestRecords, weakQuestions);
            presenter = new SettingsPresenter(view, audioPlayer, volumeRepository, displaySettings, eraser, bgmPlayer);
            presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
            Object.DestroyImmediate(mode);
        }

        [Test]
        public void 開くと保存した音量とひらがなの設定を見せる()
        {
            Assert.IsFalse(view.IsVisible);

            presenter.Open();

            Assert.IsTrue(view.IsVisible);
            Assert.AreEqual(0.4f, view.SeVolume, 1e-5f);
            Assert.AreEqual(0.3f, view.BgmVolume, 1e-5f);
            Assert.IsTrue(view.UseHiragana);
            Assert.IsFalse(view.IsEraseConfirmVisible);
        }

        [Test]
        public void 音量を変えると鳴らす音量と保存した値を変える()
        {
            presenter.Open();

            view.SeVolumeChanged.OnNext(0.7f);

            Assert.AreEqual(0.7f, audioPlayer.SeVolume, 1e-5f);
            Assert.AreEqual(0.7f, volumeRepository.SeVolume, 1e-5f);
        }

        [Test]
        public void BGMの音量を変えるとBGMに反映して保存し効果音の音量は変えない()
        {
            presenter.Open();

            view.BgmVolumeChanged.OnNext(0.7f);

            Assert.AreEqual(0.7f, bgmPlayer.BgmVolume, 1e-5f);
            Assert.AreEqual(0.7f, volumeRepository.BgmVolume, 1e-5f);
            Assert.AreEqual(0.4f, volumeRepository.SeVolume, 1e-5f);
        }

        [Test]
        public void BGMの音量は0から1の範囲に収める()
        {
            presenter.Open();

            view.BgmVolumeChanged.OnNext(1.5f);

            Assert.AreEqual(1f, bgmPlayer.BgmVolume, 1e-5f);
            Assert.AreEqual(1f, volumeRepository.BgmVolume, 1e-5f);
        }

        [Test]
        public void 開いていないときのBGMの音量の通知は無視する()
        {
            view.BgmVolumeChanged.OnNext(0.9f);

            Assert.AreEqual(0.3f, volumeRepository.BgmVolume, 1e-5f);
            Assert.AreEqual(1f, bgmPlayer.BgmVolume, 1e-5f);
        }

        [Test]
        public void 開いていないときの音量の通知は無視する()
        {
            view.SeVolumeChanged.OnNext(0.2f);

            Assert.AreEqual(0.4f, volumeRepository.SeVolume, 1e-5f);
        }

        [Test]
        public void ひらがなの設定を切り替えると保存して丸を動かす()
        {
            presenter.Open();

            view.HiraganaChanged.OnNext(false);

            Assert.IsFalse(displaySettings.UseHiragana.CurrentValue);
            Assert.IsFalse(displayRepository.UseHiragana);
            Assert.IsFalse(view.UseHiragana);
        }

        [Test]
        public void 消すを押すと確認を出し確認で消すとすべての記録を消す()
        {
            bestRecords.Save(mode.RecordKey(Difficulty.Easy), 8);
            bestRecords.Save(mode.RecordKey(Difficulty.Hard), 3);
            weakQuestions.Save(mode.RecordKey(Difficulty.Normal), new WeakQuestionList(new[] { "鬱" }));
            int erasedCount = 0;
            presenter.OnRecordsErased.Subscribe(_ => erasedCount++);
            presenter.Open();

            view.EraseClicked.OnNext(Unit.Default);
            Assert.IsTrue(view.IsEraseConfirmVisible);
            Assert.IsTrue(bestRecords.TryLoad(mode.RecordKey(Difficulty.Easy), out _), "確認の前には消さない");

            view.EraseConfirmed.OnNext(Unit.Default);

            Assert.IsFalse(view.IsEraseConfirmVisible);
            Assert.IsTrue(view.IsErasedMessageVisible);
            Assert.IsFalse(bestRecords.TryLoad(mode.RecordKey(Difficulty.Easy), out _));
            Assert.IsFalse(bestRecords.TryLoad(mode.RecordKey(Difficulty.Hard), out _));
            Assert.AreEqual(0, weakQuestions.Load(mode.RecordKey(Difficulty.Normal)).Items.Count);
            Assert.AreEqual(1, erasedCount);
        }

        [Test]
        public void 確認でやめると記録を消さない()
        {
            bestRecords.Save(mode.RecordKey(Difficulty.Easy), 8);
            presenter.Open();
            view.EraseClicked.OnNext(Unit.Default);

            view.EraseCancelled.OnNext(Unit.Default);

            Assert.IsFalse(view.IsEraseConfirmVisible);
            Assert.IsTrue(bestRecords.TryLoad(mode.RecordKey(Difficulty.Easy), out _));
        }

        [Test]
        public void 確認を出しているときのタイトルへは確認だけを閉じる()
        {
            int closedCount = 0;
            presenter.OnClosed.Subscribe(_ => closedCount++);
            presenter.Open();
            view.EraseClicked.OnNext(Unit.Default);

            view.BackClicked.OnNext(Unit.Default);

            Assert.IsFalse(view.IsEraseConfirmVisible);
            Assert.IsTrue(view.IsVisible);
            Assert.AreEqual(0, closedCount);
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
        }

        [Test]
        public void 開き直すと消したことの表示を説明に戻す()
        {
            presenter.Open();
            view.EraseClicked.OnNext(Unit.Default);
            view.EraseConfirmed.OnNext(Unit.Default);
            view.BackClicked.OnNext(Unit.Default);

            presenter.Open();

            Assert.IsFalse(view.IsErasedMessageVisible);
        }
    }
}
