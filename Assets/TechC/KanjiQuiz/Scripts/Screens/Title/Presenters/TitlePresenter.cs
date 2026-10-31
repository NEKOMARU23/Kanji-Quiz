using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトル画面の Presenter
    /// 黒板にロゴを書かせ、[はじめる] で黒板を消させてからモード選択を開く。モード選択の [← タイトルへ] でタイトルに戻る
    /// </summary>
    public sealed class TitlePresenter : IDisposable
    {
        private readonly ITitleView view;
        private readonly ModeSelectPresenter modeSelect;
        private readonly IApplicationQuitter quitter;
        private readonly IAudioPlayer audioPlayer;
        private readonly IKanjiStrokeSource strokeSource;
        private readonly string logoText;
        private readonly bool openModeSelectFirst;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly CancellationTokenSource lifetimeCts = new CancellationTokenSource();
        private IReadOnlyList<IReadOnlyList<string>> logoStrokePaths;
        private bool isTitleVisible;
        private bool isBusy;
        private bool isDisposed;

        /// <summary>タイトルが表示されていて、メニューを押せる状態かどうか</summary>
        public bool IsMenuReady => isTitleVisible && !isBusy;

        /// <param name="modeSelect">モード選択画面の Presenter（Initialize 済みでなくてよい）</param>
        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        /// <param name="strokeSource">ロゴの書き順データの読み込み口</param>
        /// <param name="logoText">黒板に書くロゴの字（例：漢字クイズ）</param>
        /// <param name="openModeSelectFirst">タイトルを飛ばして、最初からモード選択を出すかどうか</param>
        public TitlePresenter(
            ITitleView view, ModeSelectPresenter modeSelect, IApplicationQuitter quitter, IAudioPlayer audioPlayer,
            IKanjiStrokeSource strokeSource, string logoText, bool openModeSelectFirst)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.modeSelect = modeSelect ?? throw new ArgumentNullException(nameof(modeSelect));
            this.quitter = quitter ?? throw new ArgumentNullException(nameof(quitter));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.strokeSource = strokeSource ?? throw new ArgumentNullException(nameof(strokeSource));
            this.logoText = logoText ?? string.Empty;
            this.openModeSelectFirst = openModeSelectFirst;
        }

        /// <summary>
        /// モード選択を準備し、タイトル（またはモード選択）を表示する
        /// </summary>
        public void Initialize()
        {
            modeSelect.Initialize();

            view.OnStartClicked.Subscribe(this, static (_, self) => self.OpenModeSelectAsync().Forget()).AddTo(disposables);
            view.OnQuitClicked.Subscribe(this, static (_, self) => self.Quit()).AddTo(disposables);
            modeSelect.OnBackToTitle.Subscribe(this, static (_, self) => self.ShowTitleAsync().Forget()).AddTo(disposables);

            if (openModeSelectFirst)
            {
                view.SetVisible(false);
                modeSelect.Open(fromTitle: false);
            }
            else
            {
                ShowTitleAsync().Forget();
            }
        }

        /// <summary>タイトルを出して、ロゴを書かせる</summary>
        private async UniTaskVoid ShowTitleAsync()
        {
            var ct = lifetimeCts.Token;
            isTitleVisible = true;
            isBusy = true;
            view.SetVisible(true);
            try
            {
                logoStrokePaths ??= await LoadLogoStrokesAsync(ct);
                await view.PlayIntroAsync(logoStrokePaths, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            isBusy = false;
        }

        /// <summary>黒板を消させてから、モード選択を開く。消すのを飛ばしたら、モード選択の演出も飛ばす</summary>
        private async UniTaskVoid OpenModeSelectAsync()
        {
            if (!IsMenuReady) return;

            var ct = lifetimeCts.Token;
            isBusy = true;
            audioPlayer.PlaySe(SeType.Select);
            bool skipped;
            try
            {
                skipped = await view.PlayExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            isTitleVisible = false;
            isBusy = false;
            view.SetVisible(false);
            modeSelect.Open(fromTitle: !skipped);
        }

        private void Quit()
        {
            if (!IsMenuReady) return;
            quitter.Quit();
        }

        /// <summary>ロゴの字ごとに書き順のデータを読む。データのない字は空のリストになる</summary>
        private async UniTask<IReadOnlyList<IReadOnlyList<string>>> LoadLogoStrokesAsync(CancellationToken ct)
        {
            var result = new List<IReadOnlyList<string>>();
            foreach (var character in KanjiSet.SplitCharacters(logoText))
            {
                result.Add(await strokeSource.LoadStrokePathsAsync(character, ct));
            }
            return result;
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            lifetimeCts.Cancel();
            lifetimeCts.Dispose();
            disposables.Dispose();
            modeSelect.Dispose();
        }
    }
}
