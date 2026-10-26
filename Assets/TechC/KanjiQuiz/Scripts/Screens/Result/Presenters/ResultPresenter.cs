using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面の Presenter
    /// 結果発表の演出（1問ずつ ○× → 点数のルーレット → 点数）を進め、終わったらボタンを出す
    /// </summary>
    public sealed class ResultPresenter : IDisposable
    {
        private readonly QuizResult result;
        private readonly IResultView view;
        private readonly ISceneLoader sceneLoader;
        private readonly IAudioPlayer audioPlayer;
        private readonly bool playReveal;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private CancellationTokenSource revealCts;
        private bool isRevealing;
        private bool isDisposed;

        /// <param name="result">表示する結果。直接このシーンを開いた場合は null</param>
        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        /// <param name="playReveal">結果発表の演出をするかどうか。false ならすぐにすべて表示する</param>
        public ResultPresenter(QuizResult result, IResultView view, ISceneLoader sceneLoader, IAudioPlayer audioPlayer, bool playReveal)
        {
            this.result = result;
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.playReveal = playReveal;
        }

        /// <summary>
        /// 結果の表示（または結果発表の演出）を始め、ボタンの購読を始める
        /// </summary>
        public void Initialize()
        {
            if (result == null)
            {
                view.ShowNoResult();
                return;
            }

            view.SetModeName(result.ModeName);
            view.SetReviewAvailable(result.CanReview);
            view.SetItems(result.Items);

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
            }
            catch (OperationCanceledException)
            {
                // 飛ばされたときは SkipReveal が残りを表示する
                return;
            }

            FinishReveal();
        }

        private void SkipReveal()
        {
            if (!isRevealing) return;

            CancelReveal();
            FinishReveal();
        }

        private void FinishReveal()
        {
            isRevealing = false;
            ShowAll(animateScore: true);
        }

        private void ShowAll(bool animateScore)
        {
            view.RevealAllItems();
            view.ShowScore(result.CorrectCount, result.Total, animateScore);
            if (animateScore) audioPlayer.PlaySe(SeType.ScoreReveal);
            view.SetMenuVisible(true);
        }

        private void OpenReview(int index)
        {
            if (isRevealing || !result.CanReview || index < 0 || index >= result.Total) return;

            QuizReviewStore.Request(index);
            sceneLoader.Load(result.ReviewSceneName);
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
