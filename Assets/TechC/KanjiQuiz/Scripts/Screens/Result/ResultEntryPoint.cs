using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面のシーンの組み立て役
    /// </summary>
    public class ResultEntryPoint : MonoBehaviour
    {
        [SerializeField] private ResultView resultView;

        private ResultPresenter presenter;

        private void Start()
        {
            IAudioPlayer audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;

            // 振り返りから戻ってきたときは演出をせずにすぐ表示する
            bool playReveal = QuizResultStore.IsRevealPending;
            QuizResultStore.MarkRevealed();

            ISceneTransition transition = SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null;
            presenter = new ResultPresenter(
                QuizResultStore.Last, resultView, new SceneLoader(transition), audioPlayer, new PlayerPrefsBestRecordRepository(), playReveal, transition);
            presenter.Initialize();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }
    }
}
