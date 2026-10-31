using System;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトル画面の Presenter
    /// [はじめる] でモード選択を開き、モード選択の [← タイトルへ] でタイトルに戻る
    /// </summary>
    public sealed class TitlePresenter : IDisposable
    {
        private readonly ITitleView view;
        private readonly ModeSelectPresenter modeSelect;
        private readonly IApplicationQuitter quitter;
        private readonly IAudioPlayer audioPlayer;
        private readonly bool openModeSelectFirst;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private bool isTitleVisible;

        /// <param name="modeSelect">モード選択画面の Presenter（Initialize 済みでなくてよい）</param>
        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        /// <param name="openModeSelectFirst">タイトルを飛ばして、最初からモード選択を出すかどうか</param>
        public TitlePresenter(
            ITitleView view, ModeSelectPresenter modeSelect, IApplicationQuitter quitter, IAudioPlayer audioPlayer, bool openModeSelectFirst)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.modeSelect = modeSelect ?? throw new ArgumentNullException(nameof(modeSelect));
            this.quitter = quitter ?? throw new ArgumentNullException(nameof(quitter));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.openModeSelectFirst = openModeSelectFirst;
        }

        /// <summary>
        /// モード選択を準備し、タイトル（またはモード選択）を表示する
        /// </summary>
        public void Initialize()
        {
            modeSelect.Initialize();

            view.OnStartClicked.Subscribe(this, static (_, self) => self.OpenModeSelect()).AddTo(disposables);
            view.OnQuitClicked.Subscribe(this, static (_, self) => self.Quit()).AddTo(disposables);
            modeSelect.OnBackToTitle.Subscribe(this, static (_, self) => self.ShowTitle()).AddTo(disposables);

            if (openModeSelectFirst)
            {
                view.SetVisible(false);
                modeSelect.Open();
            }
            else
            {
                ShowTitle();
            }
        }

        private void ShowTitle()
        {
            isTitleVisible = true;
            view.SetVisible(true);
        }

        private void OpenModeSelect()
        {
            if (!isTitleVisible) return;

            isTitleVisible = false;
            view.SetVisible(false);
            audioPlayer.PlaySe(SeType.PageTurn);
            modeSelect.Open();
        }

        private void Quit()
        {
            if (!isTitleVisible) return;
            quitter.Quit();
        }

        public void Dispose()
        {
            disposables.Dispose();
            modeSelect.Dispose();
        }
    }
}
