using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面の Presenter
    /// 結果発表の演出（1問ずつ ○× → 点数のルーレット → 点数 → 級のハンコ → 全問正解なら花丸 → 自己ベスト）を進め、
    /// 終わったらボタン（もう一度・問題を振り返る・モード選択へ・タイトルへ）を出す
    /// </summary>
    public sealed class ResultPresenter : IDisposable
    {
        private readonly QuizResult result;
        private readonly IResultView view;
        private readonly ISceneLoader sceneLoader;
        private readonly IAudioPlayer audioPlayer;
        private readonly IBestRecordRepository bestRecordRepository;
        private readonly ISceneTransition transition;
        private readonly bool playReveal;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private CancellationTokenSource revealCts;
        private bool isRevealing;
        private bool isDisposed;
        private BestRecordUpdate? recordUpdate;

        /// <param name="result">表示する結果。直接このシーンを開いた場合は null</param>
        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        /// <param name="bestRecordRepository">自己ベストの保存先</param>
        /// <param name="playReveal">結果発表の演出をするかどうか。false ならすぐにすべて表示する</param>
        /// <param name="transition">シーンの切り替えの演出。切り替えが終わってから結果発表を始める。なければ null</param>
        public ResultPresenter(
            QuizResult result,
            IResultView view,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer,
            IBestRecordRepository bestRecordRepository,
            bool playReveal,
            ISceneTransition transition = null)
        {
            this.result = result;
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.bestRecordRepository = bestRecordRepository ?? throw new ArgumentNullException(nameof(bestRecordRepository));
            this.playReveal = playReveal;
            this.transition = transition;
        }

        /// <summary>
        /// 結果の表示（または結果発表の演出）を始め、ボタンの購読を始める
        /// </summary>
        public void Initialize()
        {
            // 結果がなくても、モード選択やタイトルには戻れるようにする
            view.OnModeSelectClicked
                .Subscribe(this, static (_, self) => self.GoToTitleScene(openModeSelect: true))
                .AddTo(disposables);
            view.OnTitleClicked
                .Subscribe(this, static (_, self) => self.GoToTitleScene(openModeSelect: false))
                .AddTo(disposables);

            if (result == null)
            {
                view.ShowNoResult();
                return;
            }

            view.SetModeName(result.ModeName);
            view.SetReviewAvailable(result.CanReview);
            view.SetItems(result.Items);
            if (result.HasRecord)
            {
                recordUpdate = result.UpdateBestRecord(bestRecordRepository);
            }

            view.OnItemClicked
                .Subscribe(this, static (index, self) => self.OpenReview(index))
                .AddTo(disposables);
            view.OnReviewClicked
                .Subscribe(this, static (_, self) => self.OpenReview(self.result.GetFirstReviewIndex()))
                .AddTo(disposables);
            view.OnRetryClicked
                .Subscribe(this, static (_, self) => self.sceneLoader.Load(self.result.RetrySceneName))
                .AddTo(disposables);
            view.OnSkipRequested
                .Subscribe(this, static (_, self) => self.SkipReveal())
                .AddTo(disposables);

            if (playReveal)
            {
                PlayRevealAsync().Forget();
            }
            else
            {
                ShowAll(animateScore: false);
            }
        }

        private async UniTaskVoid PlayRevealAsync()
        {
            isRevealing = true;
            revealCts = new CancellationTokenSource();
            var ct = revealCts.Token;

            view.SetMenuVisible(false);

            try
            {
                // 切り替えの演出で画面が見えきってから発表を始める
                if (transition != null && transition.IsPlaying)
                {
                    await UniTask.WaitWhile(() => transition.IsPlaying, cancellationToken: ct);
                }

                for (int i = 0; i < result.Total; i++)
                {
                    audioPlayer.PlaySe(result.Items[i].State == AnswerState.Correct ? SeType.Correct : SeType.Incorrect);
                    await view.RevealItemAsync(i, ct);
                    ct.ThrowIfCancellationRequested();
                }

                audioPlayer.PlayLoop(SeType.Roulette);
                await view.PlayScoreRouletteAsync(result.Total, ct);
                ct.ThrowIfCancellationRequested();
                audioPlayer.StopLoop();

                view.ShowScore(result.CorrectCount, result.Total, animate: true);
                audioPlayer.PlaySe(SeType.ScoreReveal);

                if (result.Rank != null)
                {
                    await view.WaitBeatAsync(ct);
                    ct.ThrowIfCancellationRequested();
                    audioPlayer.PlaySe(SeType.Stamp);
                    await view.PlayRankStampAsync(result.Rank, ct);
                    ct.ThrowIfCancellationRequested();
                }

                if (result.IsPerfect)
                {
                    await view.WaitBeatAsync(ct);
                    ct.ThrowIfCancellationRequested();
                    audioPlayer.PlaySe(SeType.Hanamaru);
                    await view.PlayHanamaruAsync(ct);
                    ct.ThrowIfCancellationRequested();
                }

                ShowBestRecord(animate: true);
            }
            catch (OperationCanceledException)
            {
                // 飛ばされたときは SkipReveal が残りを表示する
                return;
            }

            isRevealing = false;
            view.SetMenuVisible(true);
        }

        private void SkipReveal()
        {
            if (!isRevealing) return;

            CancelReveal();
            isRevealing = false;
            ShowAll(animateScore: true);
        }

        /// <summary>
        /// 演出をせずに、残りをすべて表示する
        /// </summary>
        /// <param name="animateScore">正解数を演出つきで出し、発表の音を鳴らすかどうか（演出を飛ばしたとき）</param>
        private void ShowAll(bool animateScore)
        {
            view.RevealAllItems();
            view.ShowScore(result.CorrectCount, result.Total, animateScore);
            if (result.Rank != null) view.ShowRank(result.Rank);
            if (result.IsPerfect) view.ShowHanamaru();
            ShowBestRecord(animate: false);
            if (animateScore) audioPlayer.PlaySe(SeType.ScoreReveal);
            view.SetMenuVisible(true);
        }

        private void ShowBestRecord(bool animate)
        {
            if (!recordUpdate.HasValue) return;

            var update = recordUpdate.Value;
            view.ShowBestRecord(update.BestCorrectCount, result.Total, update.IsNewBest, animate);
            if (animate && update.IsNewBest) audioPlayer.PlaySe(SeType.NewRecord);
        }

        private void OpenReview(int index)
        {
            if (isRevealing || !result.CanReview || index < 0 || index >= result.Total) return;

            QuizReviewStore.Request(index);
            sceneLoader.Load(result.ReviewSceneName);
        }

        /// <summary>
        /// Title シーンを開く。openModeSelect なら最初からモード選択（時間割）を出す
        /// </summary>
        private void GoToTitleScene(bool openModeSelect)
        {
            if (isRevealing) return;

            TitleLaunchOptions.OpenModeSelect = openModeSelect;
            sceneLoader.Load(SceneNames.Title);
        }

        private void CancelReveal()
        {
            audioPlayer.StopLoop();
            if (revealCts == null) return;

            revealCts.Cancel();
            revealCts.Dispose();
            revealCts = null;
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            isRevealing = false;
            CancelReveal();
            disposables.Dispose();
        }
    }
}
