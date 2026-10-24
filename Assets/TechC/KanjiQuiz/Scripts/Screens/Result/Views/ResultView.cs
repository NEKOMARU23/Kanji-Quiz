using System.Collections.Generic;
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
        [SerializeField] private Color correctColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField] private Color incorrectColor = new Color(0.2f, 0.35f, 0.8f);

        private readonly Subject<int> onItemClicked = new Subject<int>();
        private bool isReviewAvailable;

        public Observable<Unit> OnRetryClicked => retryButton.OnClickAsObservable();
        public Observable<Unit> OnReviewClicked => reviewButton.OnClickAsObservable();
        public Observable<int> OnItemClicked => onItemClicked;

        private void Awake()
        {
            onItemClicked.AddTo(this);
        }

        public void SetModeName(string modeName)
        {
            modeNameText.text = modeName;
        }

        public void SetScore(int correctCount, int total)
        {
            scoreText.text = $"{correctCount} / {total} 問 正解";
        }

        public void SetItems(IReadOnlyList<QuizResultItem> items)
        {
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < items.Count; i++)
            {
                var text = Instantiate(itemPrefab, itemContainer);
                text.text = $"{i + 1}. {items[i].Label}　{ToMark(items[i].State)}";
                text.color = items[i].State == AnswerState.Correct ? correctColor : incorrectColor;

                if (text.TryGetComponent<Button>(out var button))
                {
                    int index = i;
                    button.interactable = isReviewAvailable;
                    button.onClick.AddListener(() => onItemClicked.OnNext(index));
                }
            }
        }

        public void SetReviewAvailable(bool available)
        {
            isReviewAvailable = available;
            reviewButton.interactable = available;
            if (reviewHintText != null) reviewHintText.gameObject.SetActive(available);

            foreach (Transform child in itemContainer)
            {
                if (child.TryGetComponent<Button>(out var button)) button.interactable = available;
            }
        }

        public void ShowNoResult()
        {
            modeNameText.text = string.Empty;
            scoreText.text = "結果がありません";
            retryButton.interactable = false;
            reviewButton.interactable = false;
            if (reviewHintText != null) reviewHintText.gameObject.SetActive(false);
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
