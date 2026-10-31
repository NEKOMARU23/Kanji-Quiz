using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 時間割の1行（時限、モード名、一番上の級）
    /// </summary>
    public class TimetableRowView : MonoBehaviour
    {
        [Tooltip("行全体のボタン")]
        [SerializeField] private Button button;
        [Tooltip("行の背景。選んでいる行だけ色を変える")]
        [SerializeField] private Image background;
        [Tooltip("選んでいる行の枠")]
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private TMP_Text periodText;
        [SerializeField] private TMP_Text nameText;
        [Tooltip("一番上の級のハンコ（枠と文字）。記録がなければ隠す")]
        [SerializeField] private GameObject rankStamp;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color selectedColor = new Color(0.78f, 0.29f, 0.19f, 0.1f);
        [SerializeField] private Color nameColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        [SerializeField] private Color unavailableColor = new Color(0.54f, 0.52f, 0.47f, 1f);

        /// <summary>行がクリックされたとき</summary>
        public Observable<Unit> OnClicked => button.OnClickAsObservable();

        /// <summary>行の内容を書く</summary>
        public void Set(ModeSummary mode)
        {
            periodText.text = $"{mode.Period}時間目";
            if (mode.IsAvailable)
            {
                nameText.text = mode.Definition.DisplayName;
                nameText.color = nameColor;
            }
            else
            {
                // まだ作っていないモードは「自習」にする
                nameText.text = $"自習<size=60%>（{mode.Definition.DisplayName} 準備中）</size>";
                nameText.color = unavailableColor;
            }

            bool hasRank = mode.BestRank != null;
            rankStamp.SetActive(hasRank);
            if (hasRank) rankText.text = mode.BestRank;
            SetSelected(false);
        }

        /// <summary>選んでいる行かどうかを切り替える</summary>
        public void SetSelected(bool selected)
        {
            background.color = selected ? selectedColor : normalColor;
            if (selectedFrame != null) selectedFrame.SetActive(selected);
        }
    }
}
