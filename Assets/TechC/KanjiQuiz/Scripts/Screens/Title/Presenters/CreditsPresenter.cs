using System;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// クレジット画面の Presenter。開いて見せ、[← タイトルへ] で閉じる
    /// </summary>
    public sealed class CreditsPresenter : ITitleSubScreen
    {
        private readonly ICreditsView view;
        private readonly IAudioPlayer audioPlayer;
        private readonly Subject<Unit> closed = new Subject<Unit>();
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private bool isOpen;

        public Observable<Unit> OnClosed => closed;

        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        public CreditsPresenter(ICreditsView view, IAudioPlayer audioPlayer)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        }

        public void Initialize()
        {
            view.SetVisible(false);
            view.OnBackClicked.Subscribe(this, static (_, self) => self.Close()).AddTo(disposables);
        }

        public void Open()
        {
            isOpen = true;
            view.SetVisible(true);
        }

        private void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            audioPlayer.PlaySe(SeType.Select);
            view.SetVisible(false);
            closed.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            disposables.Dispose();
            closed.Dispose();
        }
    }
}
