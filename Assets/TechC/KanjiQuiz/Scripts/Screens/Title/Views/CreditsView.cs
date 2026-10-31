using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// クレジット画面の表示
    /// 教室の掲示板に「ありがとう」の紙と、使わせてもらったデータやフォントごとのおたよりを貼る（おたよりの文はシーンに置く）
    /// 開くと掲示板が少し下りてくる。[← タイトルへ] か Esc で閉じる
    /// </summary>
    public class CreditsView : MonoBehaviour, ICreditsView
    {
        [Tooltip("クレジット画面の中身（画面いっぱい、タイトルより手前に置く）")]
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Button backButton;
        [Tooltip("開くときに下ろす掲示板")]
        [SerializeField] private RectTransform board;
        [Tooltip("開くときの秒数")]
        [Min(0.01f)] [SerializeField] private float openSeconds = 0.25f;
        [Tooltip("開くときに掲示板が下りてくる距離（px）")]
        [SerializeField] private float openDropDistance = 40f;

        private readonly Subject<Unit> escapePressed = new Subject<Unit>();
        private Vector2 boardPosition;
        private float openElapsed;

        public Observable<Unit> OnBackClicked => backButton.OnClickAsObservable().Merge(escapePressed);

        private void Awake()
        {
            escapePressed.AddTo(this);
            if (board != null) boardPosition = board.anchoredPosition;
        }

        private void Update()
        {
            if (!root.gameObject.activeInHierarchy) return;

            if (openElapsed < openSeconds)
            {
                openElapsed += Time.unscaledDeltaTime;
                ApplyOpen(Mathf.Clamp01(openElapsed / openSeconds));
            }

            if (InputGate.IsBlocked) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) escapePressed.OnNext(Unit.Default);
        }

        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
            openElapsed = 0f;
            ApplyOpen(visible ? 0f : 1f);
        }

        private void ApplyOpen(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            root.alpha = eased;
            if (board != null) board.anchoredPosition = boardPosition + Vector2.up * (openDropDistance * (1f - eased));
        }
    }
}
