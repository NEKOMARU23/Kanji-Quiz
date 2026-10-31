using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Result;

namespace TechC.KanjiQuiz.Tests
{
    public class ResultPresenterTests
    {
        private FakeResultView view;
        private FakeSceneLoader sceneLoader;
        private FakeAudioPlayer audioPlayer;
        private FakeBestRecordRepository recordRepository;

        [SetUp]
        public void SetUp()
        {
            view = new FakeResultView();
            sceneLoader = new FakeSceneLoader();
            audioPlayer = new FakeAudioPlayer();
            recordRepository = new FakeBestRecordRepository();
        }

        private ResultPresenter CreatePresenter(QuizResult result, bool playReveal = false)
        {
            return new ResultPresenter(result, view, sceneLoader, audioPlayer, recordRepository, playReveal);
        }

        private static QuizResult CreateResult()
        {
            return new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Incorrect),
                new QuizResultItem("字", AnswerState.Correct),
            }, "StrokeOrder");
        }

        [Test]
        public void 結果を表示する()
        {
            var result = new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Incorrect),
            });

            using (var presenter = CreatePresenter(result))
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

            using (var presenter = CreatePresenter(result))
            {
                presenter.Initialize();
                view.ClickRetry();
            }

            CollectionAssert.AreEqual(new[] { "StrokeOrder" }, sceneLoader.LoadedScenes);
        }

        [Test]
        public void モード選択へを押すとモード選択から始まるタイトルのシーンを読み込む()
        {
            TitleLaunchOptions.OpenModeSelect = false;

            using (var presenter = CreatePresenter(CreateResult()))
            {
                presenter.Initialize();
                view.ClickModeSelect();
            }

            Assert.IsTrue(TitleLaunchOptions.TakeOpenModeSelect());
            CollectionAssert.AreEqual(new[] { SceneNames.Title }, sceneLoader.LoadedScenes);
        }

        [Test]
        public void タイトルへを押すとタイトルから始まるタイトルのシーンを読み込む()
        {
            TitleLaunchOptions.OpenModeSelect = true;

            using (var presenter = CreatePresenter(CreateResult()))
            {
                presenter.Initialize();
                view.ClickTitle();
            }

            Assert.IsFalse(TitleLaunchOptions.TakeOpenModeSelect());
            CollectionAssert.AreEqual(new[] { SceneNames.Title }, sceneLoader.LoadedScenes);
        }

        [Test]
        public void 結果がなくてもモード選択には戻れる()
        {
            using (var presenter = CreatePresenter(null))
            {
                presenter.Initialize();
                view.ClickModeSelect();
            }

            CollectionAssert.AreEqual(new[] { SceneNames.Title }, sceneLoader.LoadedScenes);
            TitleLaunchOptions.OpenModeSelect = false;
        }

        [Test]
        public void 演出中はモード選択へとタイトルへに反応しない()
        {
            view.HoldReveal = true;

            using (var presenter = CreatePresenter(CreateResult(), playReveal: true))
            {
                presenter.Initialize();
                view.ClickModeSelect();
                view.ClickTitle();
            }

            Assert.IsEmpty(sceneLoader.LoadedScenes);
        }

        [Test]
        public void 結果がないときはその旨を表示しボタンに反応しない()
        {
            using (var presenter = CreatePresenter(null))
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

            using (var presenter = CreatePresenter(result))
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

            using (var presenter = CreatePresenter(result))
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

            using (var presenter = CreatePresenter(result))
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

            using (var presenter = CreatePresenter(result))
            {
                presenter.Initialize();
                view.ClickItem(0);
                view.ClickReview();
            }

            Assert.IsFalse(view.IsReviewAvailable);
            Assert.IsNull(QuizReviewStore.RequestedIndex);
            Assert.IsEmpty(sceneLoader.LoadedScenes);
        }
    
        [Test]
        public void 演出では1問ずつ正誤の音を鳴らしてからルーレットを回し点数を出す()
        {
            using (var presenter = CreatePresenter(CreateResult(), playReveal: true))
            {
                presenter.Initialize();
            }

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, view.RevealedIndices);
            CollectionAssert.AreEqual(
                new[] { SeType.Correct, SeType.Incorrect, SeType.Correct, SeType.ScoreReveal, SeType.Stamp },
                audioPlayer.PlayedSe);
            CollectionAssert.AreEqual(new[] { SeType.Roulette }, audioPlayer.PlayedLoops);
            Assert.IsFalse(audioPlayer.IsLooping);
            Assert.AreEqual(1, view.RouletteCount);
            Assert.AreEqual(2, view.CorrectCount);
            Assert.AreEqual(3, view.Total);
            Assert.IsTrue(view.IsScoreAnimated);
            Assert.IsTrue(view.IsMenuVisible);
        }

        [Test]
        public void 演出中はボタンを隠し問題を押しても振り返らない()
        {
            QuizReviewStore.Clear();
            view.HoldReveal = true;

            using (var presenter = CreatePresenter(CreateResult(), playReveal: true))
            {
                presenter.Initialize();

                Assert.IsFalse(view.IsMenuVisible);
                Assert.AreEqual(-1, view.CorrectCount);

                view.ClickItem(0);
                Assert.IsNull(QuizReviewStore.RequestedIndex);
                Assert.IsEmpty(sceneLoader.LoadedScenes);
            }
        }

        [Test]
        public void 演出中にクリックすると残りを飛ばして点数を出す()
        {
            view.HoldReveal = true;

            using (var presenter = CreatePresenter(CreateResult(), playReveal: true))
            {
                presenter.Initialize();
                view.ClickSkip();
            }

            CollectionAssert.AreEqual(new[] { 0 }, view.RevealedIndices);
            Assert.IsTrue(view.IsAllRevealed);
            Assert.AreEqual(0, view.RouletteCount);
            Assert.AreEqual(2, view.CorrectCount);
            Assert.IsTrue(view.IsMenuVisible);
            Assert.AreEqual(SeType.ScoreReveal, audioPlayer.PlayedSe[audioPlayer.PlayedSe.Count - 1]);
            Assert.IsFalse(audioPlayer.IsLooping);
        }

        [Test]
        public void 演出が終わったあとのクリックでは何もしない()
        {
            using (var presenter = CreatePresenter(CreateResult(), playReveal: true))
            {
                presenter.Initialize();
                int playedCount = audioPlayer.PlayedSe.Count;

                view.ClickSkip();

                Assert.AreEqual(playedCount, audioPlayer.PlayedSe.Count);
            }
        }

        [Test]
        public void 演出しないときは音を鳴らさずにすぐすべて表示する()
        {
            using (var presenter = CreatePresenter(CreateResult(), playReveal: false))
            {
                presenter.Initialize();
            }

            Assert.IsEmpty(view.RevealedIndices);
            Assert.IsTrue(view.IsAllRevealed);
            Assert.AreEqual(2, view.CorrectCount);
            Assert.IsFalse(view.IsScoreAnimated);
            Assert.IsTrue(view.IsMenuVisible);
            Assert.IsEmpty(audioPlayer.PlayedSe);
            Assert.IsEmpty(audioPlayer.PlayedLoops);
        }
    
        private static QuizResult CreatePerfectResult(string recordKey = null)
        {
            return new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Correct),
            }, "StrokeOrder", Difficulty.Hard, recordKey);
        }

        [Test]
        public void 正解数のあとに級のハンコを押す()
        {
            using (var presenter = CreatePresenter(CreateResult(), playReveal: true))
            {
                presenter.Initialize();
            }

            // 3問中2問正解（67%）で簡単なので、5級から2つ下がって7級
            Assert.AreEqual("7級", view.Rank);
            Assert.IsTrue(view.IsRankAnimated);
            Assert.IsFalse(view.IsHanamaruShown, "全問正解でなければ花丸は出ない");
        }

        [Test]
        public void 全問正解なら級のハンコのあとに花丸を書く()
        {
            using (var presenter = CreatePresenter(CreatePerfectResult(), playReveal: true))
            {
                presenter.Initialize();
            }

            Assert.AreEqual("初段", view.Rank);
            Assert.IsTrue(view.IsHanamaruAnimated);
            CollectionAssert.AreEqual(
                new[] { SeType.Correct, SeType.Correct, SeType.ScoreReveal, SeType.Stamp, SeType.Hanamaru },
                audioPlayer.PlayedSe);
        }

        [Test]
        public void 自己ベストを更新したら更新を表示して音を鳴らす()
        {
            using (var presenter = CreatePresenter(CreatePerfectResult("StrokeOrder_Hard"), playReveal: true))
            {
                presenter.Initialize();
            }

            Assert.AreEqual(2, view.BestCorrectCount);
            Assert.IsTrue(view.IsNewBestShown);
            Assert.AreEqual(SeType.NewRecord, audioPlayer.PlayedSe[audioPlayer.PlayedSe.Count - 1]);
            Assert.IsTrue(recordRepository.TryLoad("StrokeOrder_Hard", out int saved));
            Assert.AreEqual(2, saved);
        }

        [Test]
        public void 自己ベストを更新しなければ記録だけを表示する()
        {
            recordRepository.Save("StrokeOrder_Easy", 3);
            var result = new QuizResult("書き順クイズ", "StrokeOrder", new[]
            {
                new QuizResultItem("令", AnswerState.Correct),
                new QuizResultItem("漢", AnswerState.Incorrect),
                new QuizResultItem("字", AnswerState.Correct),
            }, "StrokeOrder", Difficulty.Easy, "StrokeOrder_Easy");

            using (var presenter = CreatePresenter(result, playReveal: true))
            {
                presenter.Initialize();
            }

            Assert.AreEqual(3, view.BestCorrectCount);
            Assert.IsFalse(view.IsNewBestShown);
            CollectionAssert.DoesNotContain(audioPlayer.PlayedSe, SeType.NewRecord);
        }

        [Test]
        public void 演出を飛ばしても級と花丸と自己ベストを表示する()
        {
            view.HoldReveal = true;

            using (var presenter = CreatePresenter(CreatePerfectResult("StrokeOrder_Hard"), playReveal: true))
            {
                presenter.Initialize();
                view.ClickSkip();
            }

            Assert.AreEqual("初段", view.Rank);
            Assert.IsFalse(view.IsRankAnimated);
            Assert.IsTrue(view.IsHanamaruShown);
            Assert.IsFalse(view.IsHanamaruAnimated);
            Assert.IsTrue(view.IsNewBestShown);
            CollectionAssert.DoesNotContain(audioPlayer.PlayedSe, SeType.Stamp);
        }

        [Test]
        public void 振り返りから戻ったときも自己ベストを保存し直さず更新の表示は残る()
        {
            var result = CreatePerfectResult("StrokeOrder_Hard");

            using (var presenter = CreatePresenter(result, playReveal: true))
            {
                presenter.Initialize();
            }
            var returnedView = new FakeResultView();
            using (var presenter = new ResultPresenter(result, returnedView, sceneLoader, audioPlayer, recordRepository, playReveal: false))
            {
                presenter.Initialize();
            }

            Assert.AreEqual(1, recordRepository.SaveCount);
            Assert.IsTrue(returnedView.IsNewBestShown);
            Assert.IsTrue(returnedView.IsHanamaruShown);
            Assert.AreEqual("初段", returnedView.Rank);
        }
    
        [Test]
        public void シーンの切り替えの途中は結果発表を始めない()
        {
            var transition = new FakeSceneTransition { IsPlaying = true };

            using (var presenter = new ResultPresenter(CreateResult(), view, sceneLoader, audioPlayer, recordRepository, true, transition))
            {
                presenter.Initialize();

                Assert.IsEmpty(view.RevealedIndices);
                Assert.IsEmpty(audioPlayer.PlayedSe);
                Assert.IsFalse(view.IsMenuVisible);
            }
        }
    }
}
