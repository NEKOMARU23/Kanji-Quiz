using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面の表示
    /// </summary>
    public class ResultView : MonoBehaviour, IResultView
    {
        [SerializeField] private TMP_Text modeNameText;
        [SerializeField] private TMP_Text scoreText;
        [Tooltip("問題ごとの結果を並べる親")]
        [SerializeField] private RectTransform itemContainer;
        [Tooltip("問題1つ分の結果を表示するテキストのプレハブ")]
        [SerializeField] private TMP_Text itemPrefab;
        [SerializeField] private Button retryButton;
        [Tooltip("[問題を振り返る] ボタン")]
        [SerializeField] private Button reviewButton;
        [Tooltip("「問題を押すと振り返れます」の案内。不要なら空でよい")]
        [SerializeField] private TMP_Text reviewHintText;
        [Tooltip("演出中のクリックを受け取る、画面全体を覆う透明なボタン")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Color correctColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField] private Color incorrectColor = new Color(0.2f, 0.35f, 0.8f);
        [Tooltip("○× を出す前の文字の色")]
        [SerializeField] private Color hiddenColor = new Color(0.4f, 0.4f, 0.4f);

        [Header("演出")]
        [Tooltip("○× を出すときに、この倍率から元の大きさに縮める")]
        [SerializeField] private float revealStartScale = 1.6f;
        [Tooltip("○× を出すアニメーションの秒数")]
        [SerializeField] private float revealDuration = 0.2f;
        [Tooltip("1問の ○× を出してから次の問題までの秒数")]
        [SerializeField] private float revealInterval = 0.5f;
        [Tooltip("点数のルーレットを回す秒数")]
        [SerializeField] private float rouletteDuration = 1.5f;
        [Tooltip("ルーレットの数字を切り替える間隔（秒）")]
        [SerializeField] private float rouletteStep = 0.06f;
        [Tooltip("点数が出たときに、この倍率から元の大きさに縮める")]
        [SerializeField] private float scoreStartScale = 1.8f;
        [Tooltip("点数が出るアニメーションの秒数")]
        [SerializeField] private float scoreDuration = 0.25f;

        private readonly Subject<int> onItemClicked = new Subject<int>();
        private readonly List<TMP_Text> itemTexts = new List<TMP_Text>();
        private IReadOnlyList<QuizResultItem> items;
        private bool isReviewAvailable;
        private bool isMenuVisible = true;

        public Observable<Unit> OnRetryClicked => retryButton.OnClickAsObservable();
        public Observable<Unit> OnReviewClicked => reviewButton.OnClickAsObservable();
        public Observable<int> OnItemClicked => onItemClicked;
        public Observable<Unit> OnSkipRequested => skipButton.OnClickAsObservable();

        private void Awake()
        {
            onItemClicked.AddTo(this);
        }

        public void SetModeName(string modeName)
        {
            modeNameText.text = modeName;
        }

        public void SetItems(IReadOnlyList<QuizResultItem> items)
        {
            this.items = items;
            itemTexts.Clear();
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < items.Count; i++)
            {
                var text = Instantiate(itemPrefab, itemContainer);
                // 印の幅だけ空けておき、○× を出したときに文字の位置がずれないようにする
                text.text = $"{FormatLabel(i)}<alpha=#00>○";
                text.color = hiddenColor;
                itemTexts.Add(text);

                if (text.TryGetComponent<Button>(out var button))
                {
                    int index = i;
                    button.onClick.AddListener(() => onItemClicked.OnNext(index));
                }
            }
            RefreshItemInteractable();
            scoreText.text = string.Empty;
        }

        public async UniTask RevealItemAsync(int index, CancellationToken ct)
        {
            ShowItemMark(index);
            await ScaleAsync(itemTexts[index].transform, revealStartScale, revealDuration, ct);
            await UniTask.Delay(System.TimeSpan.FromSeconds(revealInterval), cancellationToken: ct);
        }

        public void RevealAllItems()
        {
            for (int i = 0; i < itemTexts.Count; i++)
            {
                ShowItemMark(i);
                itemTexts[i].transform.localScale = Vector3.one;
            }
        }

        public async UniTask PlayScoreRouletteAsync(int total, CancellationToken ct)
        {
            float elapsed = 0f;
            int previous = -1;
            while (elapsed < rouletteDuration)
            {
                // 同じ数字が続くと止まって見えるので、前と違う数字にする
                int value = Random.Range(0, total + 1);
                if (total > 0 && value == previous) value = (value + 1) % (total + 1);
                previous = value;
                scoreText.text = FormatScore(value, total);

                await UniTask.Delay(System.TimeSpan.FromSeconds(rouletteStep), cancellationToken: ct);
                elapsed += rouletteStep;
            }
        }

        public void ShowScore(int correctCount, int total, bool animate)
        {
            scoreText.text = FormatScore(correctCount, total);
            scoreText.transform.localScale = Vector3.one;
            if (animate)
            {
                ScaleAsync(scoreText.transform, scoreStartScale, scoreDuration, destroyCancellationToken).Forget();
            }
        }

        public void SetMenuVisible(bool visible)
        {
            isMenuVisible = visible;
            retryButton.gameObject.SetActive(visible);
            reviewButton.gameObject.SetActive(visible);
            skipButton.gameObject.SetActive(!visible);
            RefreshReviewHint();
            RefreshItemInteractable();
        }

        public void SetReviewAvailable(bool available)
        {
            isReviewAvailable = available;
            reviewButton.interactable = available;
            RefreshReviewHint();
            RefreshItemInteractable();
        }

        public void ShowNoResult()
        {
            modeNameText.text = string.Empty;
            scoreText.text = "結果がありません";
            retryButton.interactable = false;
            reviewButton.interactable = false;
            skipButton.gameObject.SetActive(false);
            if (reviewHintText != null) reviewHintText.gameObject.SetActive(false);
        }

        private void ShowItemMark(int index)
        {
            var state = items[index].State;
            itemTexts[index].text = $"{FormatLabel(index)}{ToMark(state)}";
            itemTexts[index].color = state == AnswerState.Correct ? correctColor : incorrectColor;
        }

        private void RefreshReviewHint()
        {
            if (reviewHintText != null) reviewHintText.gameObject.SetActive(isReviewAvailable && isMenuVisible);
        }

        private void RefreshItemInteractable()
        {
            foreach (var text in itemTexts)
            {
                if (text.TryGetComponent<Button>(out var button)) button.interactable = isReviewAvailable && isMenuVisible;
            }
        }

        private string FormatLabel(int index)
        {
            return $"{index + 1}. {items[index].Label}　";
        }

        private static string FormatScore(int correctCount, int total)
        {
            return $"{correctCount} / {total} 問 正解";
        }

        /// <summary>
        /// 指定の倍率から元の大きさ（1倍）まで縮める
        /// </summary>
        private static async UniTask ScaleAsync(Transform target, float startScale, float duration, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // 最初に速く、最後にゆっくり縮める（ease out）
                float t = elapsed / duration;
                float eased = 1f - (1f - t) * (1f - t);
                target.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, eased);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.deltaTime;
            }
            target.localScale = Vector3.one;
        }

        private static string ToMark(AnswerState state)
        {
            switch (state)
            {
                case AnswerState.Correct: return "○";
                case AnswerState.Incorrect: return "×";
                default: return "－";
            }
        }
    }
}
