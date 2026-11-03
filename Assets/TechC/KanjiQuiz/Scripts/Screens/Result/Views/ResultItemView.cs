using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面の、問題1つ分の行（「1. 令」と ○×）
    /// ○ と × はハンコの画像で表示する（未回答は × と同じ扱い）
    /// </summary>
    public class ResultItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [Tooltip("正解のときに出すハンコの画像")]
        [SerializeField] private Image correctStamp;
        [Tooltip("不正解（と未回答）のときに出すハンコの画像")]
        [SerializeField] private Image incorrectStamp;
        [SerializeField] private Button button;
        [SerializeField] private Color correctColor = new Color(0.8f, 0.18f, 0.15f);
        [SerializeField] private Color incorrectColor = new Color(0.2f, 0.35f, 0.8f);
        [Tooltip("○× を出す前の文字の色")]
        [SerializeField] private Color hiddenColor = new Color(0.4f, 0.4f, 0.4f);
        [Tooltip("文字が長くて ○× を右へずらすときの、文字と ○× の間（px）")]
        [SerializeField] private float stampGap = 12f;

        private float stampX;
        private bool hasStampX;

        /// <summary>行がクリックされたとき</summary>
        public Observable<Unit> OnClicked => button.OnClickAsObservable();

        /// <summary>○× を出す場所。演出で大きさを変える</summary>
        public Transform MarkTransform { get; private set; }

        /// <summary>問題を表す文字を表示し、○× を隠す</summary>
        public void SetLabel(string label)
        {
            labelText.text = label;
            labelText.color = hiddenColor;
            correctStamp.gameObject.SetActive(false);
            incorrectStamp.gameObject.SetActive(false);
            MarkTransform = correctStamp.transform;
            PlaceStamps();
        }

        /// <summary>○× を出す</summary>
        public void ShowMark(AnswerState state)
        {
            bool isCorrect = state == AnswerState.Correct;
            correctStamp.gameObject.SetActive(isCorrect);
            incorrectStamp.gameObject.SetActive(!isCorrect);
            labelText.color = isCorrect ? correctColor : incorrectColor;
            MarkTransform = isCorrect ? correctStamp.transform : incorrectStamp.transform;
        }

        /// <summary>
        /// ○× を文字のすぐ右に置く。文字が短いとき（漢字1字）はプレハブで置いた位置のまま、
        /// 長いとき（四字熟語）は文字に重ならないように右へずらす
        /// </summary>
        private void PlaceStamps()
        {
            if (!hasStampX)
            {
                stampX = ((RectTransform)correctStamp.transform).anchoredPosition.x;
                hasStampX = true;
            }
            float textEnd = labelText.GetPreferredValues(labelText.text).x;
            foreach (var stamp in new[] { correctStamp, incorrectStamp })
            {
                var rect = (RectTransform)stamp.transform;
                float x = Mathf.Max(stampX, textEnd + stampGap + rect.rect.width * 0.5f);
                rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            }
        }

        /// <summary>クリックできるかどうかを切り替える</summary>
        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}
