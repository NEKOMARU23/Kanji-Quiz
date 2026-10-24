using TechC.KanjiQuiz.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.DebugTools
{
    /// <summary>
    /// テスト用：難易度を切り替えて、今のシーンを最初からやり直すボタン
    /// 普段は隠しておき、F1 キーで表示と非表示を切り替える
    /// モード選択画面ができるまでの仮の機能。エディタと開発ビルドでだけ使える
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DebugDifficultySwitcher : MonoBehaviour
    {
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        private CanvasGroup canvasGroup;

        /// <summary>シーンを読み込み直しても、表示状態を引き継ぐ</summary>
        private static bool isVisible;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            SetVisible(isVisible);

            easyButton.onClick.AddListener(() => Switch(Difficulty.Easy));
            normalButton.onClick.AddListener(() => Switch(Difficulty.Normal));
            hardButton.onClick.AddListener(() => Switch(Difficulty.Hard));
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                SetVisible(!isVisible);
            }
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private static void Switch(Difficulty difficulty)
        {
            QuizLaunchOptions.Difficulty = difficulty;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
