using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 全モード共通のプレイ画面の枠
    /// 判断は持たず、入力の通知と表示の切り替えだけを行う
    /// キーボードでは Enter・スペース・→ で [次へ]、← で [前へ] を押す
    /// </summary>
    public class QuizFrameView : MonoBehaviour, IQuizFrameView
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        [Tooltip("モード名や難易度を表示するテキスト。不要なら空でよい")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [Tooltip("進行バー。Image Type を Filled にしておく。不要なら空でよい")]
        [SerializeField] private Image progressFill;

        [Header("問題を切り替える演出")]
        [Tooltip("問題を切り替えるときにスライドさせるもの（問題文、選択肢を並べる親など）")]
        [SerializeField] private RectTransform[] transitionTargets;
        [Tooltip("スライドの距離（Canvas の座標で）")]
        [SerializeField] private float transitionDistance = 160f;
        [Tooltip("スライドの秒数")]
        [SerializeField] private float transitionDuration = 0.25f;

        private Vector2[] basePositions;
        private CancellationTokenSource transitionCts;

        public Observable<Unit> OnNextClicked => nextButton.OnClickAsObservable();
        public Observable<Unit> OnPrevClicked => prevButton.OnClickAsObservable();

        private void Awake()
        {
            basePositions = new Vector2[transitionTargets.Length];
            for (int i = 0; i < transitionTargets.Length; i++)
            {
                basePositions[i] = transitionTargets[i].anchoredPosition;
            }
        }

        private void Update()
        {
            if (InputGate.IsBlocked) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                PressIfAvailable(nextButton);
            }
            else if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                PressIfAvailable(prevButton);
            }
        }

        /// <summary>ボタンが表示されていて押せるときだけ、押したことにする</summary>
        private static void PressIfAvailable(Button button)
        {
            if (button.IsActive() && button.IsInteractable()) button.onClick.Invoke();
        }

        private void OnDestroy()
        {
            transitionCts?.Cancel();
            transitionCts?.Dispose();
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        public void SetProgress(int current, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"{current} / {total} {{問|もん}}";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount = total > 0 ? (float)current / total : 0f;
            }
        }

        public void SetNextInteractable(bool interactable)
        {
            nextButton.interactable = interactable;
        }

        public void SetPrevInteractable(bool interactable)
        {
            prevButton.interactable = interactable;
        }

        public void PlayQuestionTransition(int direction)
        {
            if (transitionTargets.Length == 0) return;

            transitionCts?.Cancel();
            transitionCts?.Dispose();
            transitionCts = new CancellationTokenSource();

            var offset = direction == 0
                ? new Vector2(0f, -transitionDistance)
                : new Vector2(Mathf.Sign(direction) * transitionDistance, 0f);
            SlideInAsync(offset, transitionCts.Token).Forget();
        }

        private async UniTaskVoid SlideInAsync(Vector2 offset, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                // 最初に速く、最後にゆっくり止まる（ease out）
                float t = 1f - Mathf.Pow(1f - elapsed / transitionDuration, 3f);
                SetOffset(offset * (1f - t));

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.deltaTime;
            }
            SetOffset(Vector2.zero);
        }

        private void SetOffset(Vector2 offset)
        {
            for (int i = 0; i < transitionTargets.Length; i++)
            {
                transitionTargets[i].anchoredPosition = basePositions[i] + offset;
            }
        }
    }
}
