using System;
using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトルから開く設定画面の Presenter
    /// 効果音の音量、ひらがなで表示の切り替え、記録を消す（確認してから）を受け付ける
    /// </summary>
    public sealed class SettingsPresenter : ITitleSubScreen
    {
        private readonly ISettingsView view;
        private readonly IAudioPlayer audioPlayer;
        private readonly IVolumeRepository volumeRepository;
        private readonly DisplaySettings displaySettings;
        private readonly QuizRecordEraser recordEraser;
        private readonly Subject<Unit> closed = new Subject<Unit>();
        private readonly Subject<Unit> recordsErased = new Subject<Unit>();
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private bool isOpen;
        private bool isConfirming;

        public Observable<Unit> OnClosed => closed;

        /// <summary>記録を消したとき（時間割の記録を読み直すのに使う）</summary>
        public Observable<Unit> OnRecordsErased => recordsErased;

        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        /// <param name="displaySettings">表示の設定（ふつうは DisplaySettings.Shared）</param>
        public SettingsPresenter(
            ISettingsView view, IAudioPlayer audioPlayer, IVolumeRepository volumeRepository,
            DisplaySettings displaySettings, QuizRecordEraser recordEraser)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.volumeRepository = volumeRepository ?? throw new ArgumentNullException(nameof(volumeRepository));
            this.displaySettings = displaySettings ?? throw new ArgumentNullException(nameof(displaySettings));
            this.recordEraser = recordEraser ?? throw new ArgumentNullException(nameof(recordEraser));
        }

        public void Initialize()
        {
            view.SetVisible(false);
            view.OnBackClicked.Subscribe(this, static (_, self) => self.Back()).AddTo(disposables);
            view.OnSeVolumeChanged.Subscribe(this, static (v, self) => self.ChangeSeVolume(v)).AddTo(disposables);
            view.OnHiraganaChanged.Subscribe(this, static (v, self) => self.ChangeHiragana(v)).AddTo(disposables);
            view.OnEraseClicked.Subscribe(this, static (_, self) => self.AskErase()).AddTo(disposables);
            view.OnEraseConfirmed.Subscribe(this, static (_, self) => self.Erase()).AddTo(disposables);
            view.OnEraseCancelled.Subscribe(this, static (_, self) => self.CancelErase()).AddTo(disposables);
        }

        public void Open()
        {
            isOpen = true;
            isConfirming = false;
            view.SetSeVolume(volumeRepository.LoadSeVolume());
            view.SetHiragana(displaySettings.UseHiragana.CurrentValue);
            view.SetEraseConfirmVisible(false);
            view.SetErasedMessageVisible(false);
            view.SetVisible(true);
        }

        /// <summary>[← タイトルへ]・Esc。確認を出しているときは、確認だけを閉じる</summary>
        private void Back()
        {
            if (!isOpen) return;
            if (isConfirming)
            {
                CancelErase();
                return;
            }

            isOpen = false;
            audioPlayer.PlaySe(SeType.Select);
            view.SetVisible(false);
            closed.OnNext(Unit.Default);
        }

        private void ChangeSeVolume(float volume)
        {
            if (!isOpen) return;
            volume = Mathf.Clamp01(volume);
            audioPlayer.SetSeVolume(volume);
            volumeRepository.SaveSeVolume(volume);
        }

        private void ChangeHiragana(bool useHiragana)
        {
            if (!isOpen) return;
            displaySettings.SetUseHiragana(useHiragana);
            view.SetHiragana(useHiragana);
            audioPlayer.PlaySe(SeType.Select);
        }

        private void AskErase()
        {
            if (!isOpen || isConfirming) return;
            isConfirming = true;
            audioPlayer.PlaySe(SeType.Select);
            view.SetEraseConfirmVisible(true);
        }

        private void Erase()
        {
            if (!isConfirming) return;
            isConfirming = false;
            recordEraser.EraseAll();
            audioPlayer.PlaySe(SeType.Select);
            view.SetEraseConfirmVisible(false);
            view.SetErasedMessageVisible(true);
            recordsErased.OnNext(Unit.Default);
        }

        private void CancelErase()
        {
            if (!isConfirming) return;
            isConfirming = false;
            view.SetEraseConfirmVisible(false);
        }

        public void Dispose()
        {
            disposables.Dispose();
            closed.Dispose();
            recordsErased.Dispose();
        }
    }
}
