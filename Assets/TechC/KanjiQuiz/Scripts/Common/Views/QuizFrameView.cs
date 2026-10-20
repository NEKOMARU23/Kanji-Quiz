using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 全モード共通のプレイ画面の枠
    /// 判断は持たず、入力の通知と表示の切り替えだけを行う
    /// </summary>
    public class QuizFrameView : MonoBehaviour, IQuizFrameView
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private TMP_Text progressText;
        [Tooltip("進行バー。Image Type を Filled にしておく。不要なら空でよい")]
        [SerializeField] private Image progressFill;

        public Observable<Unit> OnNextClicked => nextButton.OnClickAsObservable();
        public Observable<Unit> OnPrevClicked => prevButton.OnClickAsObservable();

        public void SetProgress(int current, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"{current} / {total} 問";
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
    }
}
