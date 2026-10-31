using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトル画面の表示（仮。レイアウトとロゴの演出はあとで決める）
    /// </summary>
    public class TitleView : MonoBehaviour, ITitleView
    {
        [Tooltip("タイトル画面全体（モード選択画面と切り替える）")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        private readonly Subject<Unit> startKeyPressed = new Subject<Unit>();

        public Observable<Unit> OnStartClicked => startButton.OnClickAsObservable().Merge(startKeyPressed);
        public Observable<Unit> OnQuitClicked => quitButton.OnClickAsObservable();

        private void Awake()
        {
            startKeyPressed.AddTo(this);
        }

        private void Update()
        {
            if (!root.activeInHierarchy || InputGate.IsBlocked) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                startKeyPressed.OnNext(Unit.Default);
            }
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }
    }
}
