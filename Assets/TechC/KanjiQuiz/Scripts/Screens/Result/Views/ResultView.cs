using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [Tooltip("問題1つ分の結果のプレハブ")]
        [SerializeField] private ResultItemView itemPrefab;
        [SerializeField] private Button retryButton;
        [Tooltip("[問題を振り返る] ボタン")]
        [SerializeField] private Button reviewButton;
        [Tooltip("「問題を押すと振り返れます」の案内。不要なら空でよい")]
        [SerializeField] private TMP_Text reviewHintText;
        [Tooltip("演出中のクリックを受け取る、画面全体を覆う透明なボタン")]
        [SerializeField] private Button skipButton;

        [Header("級・花丸・自己ベスト")]
        [Tooltip("級のハンコ（枠の画像と、中の文字の親）")]
        [SerializeField] private RectTransform rankStamp;
        [SerializeField] private TMP_Text rankText;
        [Tooltip("全問正解の花丸。Image Type を Filled（Radial 360）にしておく")]
        [SerializeField] private Image hanamaruImage;
        [Tooltip("「自己ベスト 4 / 5 問」の表示")]
        [SerializeField] private TMP_Text bestRecordText;
        [Tooltip("「自己ベスト更新！」の表示")]
        [SerializeField] private RectTransform newRecordBadge;

        [Header("演出")]
        [Tooltip("○× を出すときに、この倍率から元の大きさに縮める（ハンコを押すように）")]
        [SerializeField] private float revealStartScale = 2.2f;
        [Tooltip("○× を出すアニメーションの秒数")]
        [SerializeField] private float revealDuration = 0.15f;
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
        [Tooltip("演出と演出の間（秒）")]
        [SerializeField] private float beatDuration = 0.4f;
        [Tooltip("級のハンコを押すときに、この倍率から元の大きさに縮める")]
        [SerializeField] private float rankStartScale = 3f;
        [Tooltip("級のハンコを押すアニメーションの秒数")]
        [SerializeField] private float rankDuration = 0.18f;
        [Tooltip("花丸を書く秒数")]
        [SerializeField] private float hanamaruDuration = 1.1f;

        private readonly Subject<int> onItemClicked = new Subject<int>();
        private readonly List<ResultItemView> itemViews = new List<ResultItemView>();
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
            HideExtras();
        }

        private void Update()
        {
            // 演出中は Enter・スペースでも飛ばせる
            if (!skipButton.gameObject.activeInHierarchy || InputGate.IsBlocked) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                skipButton.onClick.Invoke();
            }
        }

        public void SetModeName(string modeName)
        {
            modeNameText.text = modeName;
        }

        public void SetItems(IReadOnlyList<QuizResultItem> items)
        {
            this.items = items;
            itemViews.Clear();
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < items.Count; i++)
            {
                var itemView = Instantiate(itemPrefab, itemContainer);
                itemView.SetLabel($"{i + 1}. {items[i].Label}");
                itemView.OnClicked
                    .Subscribe((index: i, subject: onItemClicked), static (_, state) => state.subject.OnNext(state.index))
                    .AddTo(itemView);
                itemViews.Add(itemView);
            }
            RefreshItemInteractable();
            scoreText.text = string.Empty;
            HideExtras();
        }

        public async UniTask RevealItemAsync(int index, CancellationToken ct)
        {
            itemViews[index].ShowMark(items[index].State);
            await ScaleAsync(itemViews[index].MarkTransform, revealStartScale, revealDuration, ct);
            await UniTask.Delay(System.TimeSpan.FromSeconds(revealInterval), cancellationToken: ct);
        }

        public void RevealAllItems()
        {
            for (int i = 0; i < itemViews.Count; i++)
            {
                itemViews[i].ShowMark(items[i].State);
                itemViews[i].MarkTransform.localScale = Vector3.one;
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

        public UniTask WaitBeatAsync(CancellationToken ct)
        {
            return UniTask.Delay(System.TimeSpan.FromSeconds(beatDuration), cancellationToken: ct);
        }

        public UniTask PlayRankStampAsync(string rank, CancellationToken ct)
        {
            ShowRank(rank);
            return ScaleAsync(rankStamp, rankStartScale, rankDuration, ct);
        }

        public void ShowRank(string rank)
        {
            rankText.text = rank;
            rankStamp.localScale = Vector3.one;
            rankStamp.gameObject.SetActive(true);
        }

        public async UniTask PlayHanamaruAsync(CancellationToken ct)
        {
            hanamaruImage.fillAmount = 0f;
            hanamaruImage.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < hanamaruDuration)
            {
                hanamaruImage.fillAmount = elapsed / hanamaruDuration;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.deltaTime;
            }
            hanamaruImage.fillAmount = 1f;
        }

        public void ShowHanamaru()
        {
            hanamaruImage.fillAmount = 1f;
            hanamaruImage.gameObject.SetActive(true);
        }

        public void ShowBestRecord(int bestCorrectCount, int total, bool isNewBest, bool animate)
        {
            bestRecordText.text = $"自己ベスト　{bestCorrectCount} / {total} 問";
            bestRecordText.gameObject.SetActive(true);
            newRecordBadge.gameObject.SetActive(isNewBest);
            newRecordBadge.localScale = Vector3.one;
            if (isNewBest && animate)
            {
                ScaleAsync(newRecordBadge, scoreStartScale, scoreDuration, destroyCancellationToken).Forget();
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

        private void HideExtras()
        {
            rankStamp.gameObject.SetActive(false);
            hanamaruImage.gameObject.SetActive(false);
            bestRecordText.gameObject.SetActive(false);
            newRecordBadge.gameObject.SetActive(false);
        }

        private void RefreshReviewHint()
        {
            if (reviewHintText != null) reviewHintText.gameObject.SetActive(isReviewAvailable && isMenuVisible);
        }

        private void RefreshItemInteractable()
        {
            foreach (var itemView in itemViews)
            {
                itemView.SetInteractable(isReviewAvailable && isMenuVisible);
            }
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
    }
}
