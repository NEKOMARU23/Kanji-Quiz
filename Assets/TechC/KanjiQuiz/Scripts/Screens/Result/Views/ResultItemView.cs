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

        /// <summary>クリックできるかどうかを切り替える</summary>
        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}
