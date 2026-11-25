using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 問題の振り返りのときだけ表示する部品
    /// シーンでは非表示にしておき、振り返りのときに Presenter が表示する
    /// </summary>
    public class QuizReviewView : MonoBehaviour, IQuizReviewView
    {
        [SerializeField] private Button backButton;
        [Tooltip("表示中の問題の正誤を表示するテキスト。不要なら空でよい")]
        [SerializeField] private TMP_Text answerStateText;
        [SerializeField] private Color correctColor = new Color(0.2f, 0.55f, 0.25f, 1f);
        [SerializeField] private Color incorrectColor = new Color(0.78f, 0.29f, 0.19f, 1f);

        public Observable<Unit> OnBackClicked => backButton.OnClickAsObservable();

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetAnswerState(AnswerState state)
        {
            if (answerStateText == null) return;

            switch (state)
            {
                case AnswerState.Correct:
                    answerStateText.text = "{振|ふ}り{返|かえ}り：{正解|せいかい}";
                    answerStateText.color = correctColor;
                    break;
                case AnswerState.Incorrect:
                    answerStateText.text = "{振|ふ}り{返|かえ}り：{不正解|ふせいかい}";
                    answerStateText.color = incorrectColor;
                    break;
                default:
                    answerStateText.text = "{振|ふ}り{返|かえ}り：{未回答|みかいとう}";
                    answerStateText.color = incorrectColor;
                    break;
            }
        }
    }
}
