using System;
using R3;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 中断メニューの Presenter
    /// [メニュー] ボタンか Esc で開き、続ける・最初からやり直す・やめる（モード選択へ）・効果音の音量・ひらがなで表示の切り替えを受け付ける
    /// </summary>
    public sealed class PauseMenuPresenter : IDisposable
    {
        private readonly IPauseMenuView view;
        private readonly ISceneLoader sceneLoader;
        private readonly IAudioPlayer audioPlayer;
        private readonly IVolumeRepository volumeRepository;
        private readonly string restartSceneName;
        private readonly string quitSceneName;
        private readonly DisplaySettings displaySettings;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        /// <summary>メニューを開いているかどうか</summary>
        public bool IsOpen { get; private set; }

        /// <param name="restartSceneName">[最初からやり直す] で読み込むシーンの名前</param>
        /// <param name="quitSceneName">[やめる] で読み込むシーンの名前（モード選択のあるシーン）</param>
        /// <param name="displaySettings">表示の設定（ひらがなで表示）。null なら切り替えを使わない</param>
        public PauseMenuPresenter(
            IPauseMenuView view,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer,
            IVolumeRepository volumeRepository,
            string restartSceneName,
            string quitSceneName = SceneNames.Title,
            DisplaySettings displaySettings = null)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.volumeRepository = volumeRepository ?? throw new ArgumentNullException(nameof(volumeRepository));
            this.restartSceneName = restartSceneName ?? throw new ArgumentNullException(nameof(restartSceneName));
            this.quitSceneName = quitSceneName ?? throw new ArgumentNullException(nameof(quitSceneName));
            this.displaySettings = displaySettings;
        }

        /// <summary>
        /// 保存した音量をつまみに反映し、入力の購読を始める
        /// </summary>
        public void Initialize()
        {
            view.SetSeVolume(volumeRepository.LoadSeVolume());
            if (displaySettings != null)
            {
                view.SetHiragana(displaySettings.UseHiragana.CurrentValue);
                view.OnHiraganaChanged
                    .Subscribe(this, static (value, self) => self.displaySettings.SetUseHiragana(value))
                    .AddTo(disposables);
            }

            view.OnOpenClicked
                .Subscribe(this, static (_, self) => self.Open())
                .AddTo(disposables);
            view.OnMenuKeyPressed
                .Subscribe(this, static (_, self) => { if (self.IsOpen) self.Close(); else self.Open(); })
                .AddTo(disposables);
            view.OnResumeClicked
                .Subscribe(this, static (_, self) => self.Close())
                .AddTo(disposables);
            view.OnRestartClicked
                .Subscribe(this, static (_, self) => self.Restart())
                .AddTo(disposables);
            view.OnQuitClicked
                .Subscribe(this, static (_, self) => self.Quit())
                .AddTo(disposables);
            view.OnSeVolumeChanged
                .Subscribe(this, static (volume, self) => self.ChangeSeVolume(volume))
                .AddTo(disposables);
        }

        private void Open()
        {
            if (IsOpen) return;

            IsOpen = true;
            audioPlayer.PlaySe(SeType.Select);
            view.Show();
        }

        private void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            view.Hide();
        }

        private void Restart()
        {
            Close();
            sceneLoader.Load(restartSceneName);
        }

        /// <summary>
        /// プレイをやめて、モード選択（時間割）に戻る
        /// </summary>
        private void Quit()
        {
            Close();
            TitleLaunchOptions.OpenModeSelect = true;
            sceneLoader.Load(quitSceneName);
        }

        private void ChangeSeVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            audioPlayer.SetSeVolume(volume);
            volumeRepository.SaveSeVolume(volume);
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
