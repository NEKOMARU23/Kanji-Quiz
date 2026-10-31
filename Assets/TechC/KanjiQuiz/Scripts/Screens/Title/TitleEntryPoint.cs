using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// Title シーン（タイトル画面とモード選択画面）の組み立て役
    /// </summary>
    public class TitleEntryPoint : MonoBehaviour
    {
        [SerializeField] private TitleView titleView;
        [SerializeField] private ModeSelectView modeSelectView;
        [Tooltip("時間割に並べるモードの一覧（Data/Modes/ModeCatalog）")]
        [SerializeField] private QuizModeCatalog modeCatalog;
        [Tooltip("黒板にチョークで書くロゴの字")]
        [SerializeField] private string logoText = "漢字クイズ";

        private TitlePresenter presenter;

        private void Start()
        {
            IAudioPlayer audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;
            ISceneTransition transition = SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null;

            // 前のプレイの難易度が残らないように消しておく（モード選択で選び直す）
            QuizLaunchOptions.Clear();

            var modes = ModeSummary.Load(
                modeCatalog.Modes, new PlayerPrefsBestRecordRepository(), new PlayerPrefsWeakQuestionRepository());
            var modeSelect = new ModeSelectPresenter(modes, modeSelectView, new SceneLoader(transition), audioPlayer);
            presenter = new TitlePresenter(
                titleView, modeSelect, new ApplicationQuitter(), audioPlayer, new StreamingAssetsKanjiStrokeSource(),
                logoText, TitleLaunchOptions.TakeOpenModeSelect());
            presenter.Initialize();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }
    }
}
