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
    /// メニューから開く画面は、タイトルの上に重ねる（漢字帳・クレジット）か、黒板を消して板書する（設定）。閉じるとロゴを書き直さずにタイトルに戻る
    /// </summary>
    public sealed class TitlePresenter : IDisposable
    {
        private readonly ITitleView view;
        private readonly ModeSelectPresenter modeSelect;
        private readonly TitleSubScreens subScreens;
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
        private ITitleSubScreen openSubScreen;
        private bool isSubScreenOnBoard;
        private bool isDisposed;

        /// <summary>タイトルが表示されていて、メニューを押せる状態かどうか</summary>
        public bool IsMenuReady => isTitleVisible && !isBusy && openSubScreen == null;

        /// <param name="modeSelect">モード選択画面の Presenter（Initialize 済みでなくてよい）</param>
        /// <param name="subScreens">メニューから開く画面（Initialize 済みでなくてよい）</param>
        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        /// <param name="strokeSource">ロゴの書き順データの読み込み口</param>
        /// <param name="logoText">黒板に書くロゴの字（例：漢字クイズ）</param>
        /// <param name="openModeSelectFirst">タイトルを飛ばして、最初からモード選択を出すかどうか</param>
        public TitlePresenter(
            ITitleView view, ModeSelectPresenter modeSelect, TitleSubScreens subScreens, IApplicationQuitter quitter,
            IAudioPlayer audioPlayer, IKanjiStrokeSource strokeSource, string logoText, bool openModeSelectFirst)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.modeSelect = modeSelect ?? throw new ArgumentNullException(nameof(modeSelect));
            this.subScreens = subScreens ?? throw new ArgumentNullException(nameof(subScreens));
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
            foreach (var screen in subScreens.All())
            {
                screen.Initialize();
                screen.OnClosed.Subscribe(this, static (_, self) => self.OnSubScreenClosed()).AddTo(disposables);
            }

            view.OnStartClicked.Subscribe(this, static (_, self) => self.OpenModeSelectAsync().Forget()).AddTo(disposables);
            view.OnQuitClicked.Subscribe(this, static (_, self) => self.Quit()).AddTo(disposables);
            view.OnSettingsClicked
                .Subscribe(this, static (_, self) => self.OpenBoardSubScreenAsync(self.subScreens.Settings, TitleMenuItem.Settings).Forget())
                .AddTo(disposables);
            view.OnKanjiBookClicked.Subscribe(this, static (_, self) => self.OpenSubScreen(self.subScreens.KanjiBook)).AddTo(disposables);
            view.OnCreditsClicked.Subscribe(this, static (_, self) => self.OpenSubScreen(self.subScreens.Credits)).AddTo(disposables);
            modeSelect.OnBackToTitle.Subscribe(this, static (_, self) => self.ShowTitleAsync().Forget()).AddTo(disposables);
            view.OnEffectSound.Subscribe(this, static (type, self) => self.audioPlayer.PlaySe(type)).AddTo(disposables);

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
                skipped = await view.PlayExitAsync(TitleMenuItem.Start, ct);
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

        /// <summary>メニューから開く画面を、タイトルの上に開く</summary>
        private void OpenSubScreen(ITitleSubScreen screen)
        {
            if (screen == null || !IsMenuReady) return;

            audioPlayer.PlaySe(SeType.Select);
            openSubScreen = screen;
            isSubScreenOnBoard = false;
            screen.Open();
        }

        /// <summary>黒板消しでタイトルを消してから、メニューから開く画面を黒板に出す</summary>
        private async UniTaskVoid OpenBoardSubScreenAsync(ITitleSubScreen screen, TitleMenuItem pressed)
        {
            if (screen == null || !IsMenuReady) return;

            var ct = lifetimeCts.Token;
            isBusy = true;
            audioPlayer.PlaySe(SeType.Select);
            try
            {
                await view.PlayExitAsync(pressed, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            isBusy = false;
            view.SetVisible(false);
            openSubScreen = screen;
            isSubScreenOnBoard = true;
            screen.Open();
        }

        /// <summary>メニューから開いた画面が閉じたら、タイトルに戻る（黒板に出していたなら、書き終えたロゴを出す）</summary>
        private void OnSubScreenClosed()
        {
            if (openSubScreen == null) return;

            openSubScreen = null;
            if (isSubScreenOnBoard) view.ShowWithoutIntro(logoStrokePaths);
            isSubScreenOnBoard = false;
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
            foreach (var screen in subScreens.All()) screen.Dispose();
        }
    }
}
