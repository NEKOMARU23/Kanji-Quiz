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
        [SerializeField] private Color correctColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField] private Color incorrectColor = new Color(0.2f, 0.35f, 0.8f);

        public Observable<Unit> OnRetryClicked => retryButton.OnClickAsObservable();

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
            }
        }

        public void ShowNoResult()
        {
            modeNameText.text = string.Empty;
            scoreText.text = "結果がありません";
            retryButton.interactable = false;
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
