using System;
using R3;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 中断メニューの Presenter
    /// [メニュー] ボタンか Esc で開き、続ける・最初からやり直す・効果音の音量の変更を受け付ける
    /// </summary>
    public sealed class PauseMenuPresenter : IDisposable
    {
        private readonly IPauseMenuView view;
        private readonly ISceneLoader sceneLoader;
        private readonly IAudioPlayer audioPlayer;
        private readonly IVolumeRepository volumeRepository;
        private readonly string restartSceneName;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        /// <summary>メニューを開いているかどうか</summary>
        public bool IsOpen { get; private set; }

        /// <param name="restartSceneName">[最初からやり直す] で読み込むシーンの名前</param>
        public PauseMenuPresenter(
            IPauseMenuView view,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer,
            IVolumeRepository volumeRepository,
            string restartSceneName)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.volumeRepository = volumeRepository ?? throw new ArgumentNullException(nameof(volumeRepository));
            this.restartSceneName = restartSceneName ?? throw new ArgumentNullException(nameof(restartSceneName));
        }

        /// <summary>
        /// 保存した音量をつまみに反映し、入力の購読を始める
        /// </summary>
        public void Initialize()
        {
            view.SetSeVolume(volumeRepository.LoadSeVolume());

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
