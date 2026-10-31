using UnityEngine;
using UnityEngine.SceneManagement;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 中断メニューの組み立て役
    /// 中断メニューのプレハブに入れておくので、プレハブを置くだけで、どのシーンでも中断メニューが使える
    /// [最初からやり直す] では、今のシーンを読み込み直す
    /// </summary>
    public class PauseMenuEntryPoint : MonoBehaviour
    {
        [SerializeField] private PauseMenuView pauseMenuView;

        private PauseMenuPresenter presenter;

        private void Start()
        {
            IAudioPlayer audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;
            ISceneTransition transition = SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null;

            presenter = new PauseMenuPresenter(
                pauseMenuView,
                new SceneLoader(transition),
                audioPlayer,
                new PlayerPrefsVolumeRepository(),
                gameObject.scene.name,
                SceneNames.Title,
                DisplaySettings.Shared);
            presenter.Initialize();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }
    }
}
