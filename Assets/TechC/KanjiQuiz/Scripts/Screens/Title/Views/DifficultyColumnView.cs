using System.Text;
using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 難易度の黒板の1列（難易度名、級、出題範囲、自己ベスト、ひとこと、苦手な漢字）
    /// </summary>
    public class DifficultyColumnView : MonoBehaviour
    {
        [Tooltip("列全体のボタン")]
        [SerializeField] private Button button;
        [Tooltip("選んでいない列を薄くする")]
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("選んでいる列の見出しを囲む黄色いチョークの丸")]
        [SerializeField] private GameObject selectedCircle;
        [Tooltip("列の左のチョークの線。一番左の列では隠す")]
        [SerializeField] private GameObject leftLine;
        [SerializeField] private TMP_Text nameText;
        [Tooltip("級のハンコ（枠と文字）。記録がなければ隠す")]
        [SerializeField] private GameObject rankStamp;
        [SerializeField] private TMP_Text rankText;
        [Tooltip("記録がないときに級の場所に出す「まだ」")]
        [SerializeField] private GameObject noRecordText;
        [SerializeField] private TMP_Text rangeText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private TMP_Text weakLabelText;
        [SerializeField] private TMP_Text weakText;
        [Tooltip("苦手な漢字の字の大きさ")]
        [SerializeField] private float weakFontSize = 60f;
        [Tooltip("苦手な漢字がないときの「なし」の字の大きさ")]
        [SerializeField] private float noWeakFontSize = 34f;
        [Tooltip("苦手な漢字をいくつまで書くか")]
        [Min(1)] [SerializeField] private int maxWeakCount = 6;
        [Tooltip("選んでいない列の濃さ")]
        [Range(0f, 1f)] [SerializeField] private float unselectedAlpha = 0.55f;
        [SerializeField] private Color weakColor = new Color(0.95f, 0.66f, 0.63f, 1f);
        [SerializeField] private Color mutedColor = new Color(0.95f, 0.94f, 0.9f, 0.45f);

        /// <summary>列がクリックされたとき</summary>
        public Observable<Unit> OnClicked => button.OnClickAsObservable();

        /// <summary>列の内容を書く</summary>
        /// <param name="isFirst">一番左の列かどうか（左の線を隠す）</param>
        public void Set(DifficultyRecord record, bool isFirst)
        {
            leftLine.SetActive(!isFirst);
            nameText.text = record.Difficulty.ToDisplayName();
            rangeText.text = $"{record.Range}から {record.QuestionCount}問";

            rankStamp.SetActive(record.HasRecord);
            noRecordText.SetActive(!record.HasRecord);
            if (record.HasRecord) rankText.text = record.Rank;

            bestText.text = record.HasRecord
                ? $"ベスト <size=150%>{record.BestCorrectCount} / {record.QuestionCount}</size>"
                : "ベスト <size=150%>―</size>";
            goalText.text = record.Goal;

            bool hasWeak = record.WeakQuestions.Count > 0;
            weakLabelText.text = hasWeak ? "苦手な漢字（次に出ます）" : "苦手な漢字";
            weakLabelText.color = hasWeak ? weakColor : mutedColor;
            weakText.text = hasWeak ? JoinWeak(record) : "なし";
            weakText.color = hasWeak ? weakColor : mutedColor;
            weakText.fontSize = hasWeak ? weakFontSize : noWeakFontSize;
        }

        /// <summary>選んでいる列かどうかを切り替える</summary>
        public void SetSelected(bool selected)
        {
            selectedCircle.SetActive(selected);
            canvasGroup.alpha = selected ? 1f : unselectedAlpha;
        }

        private string JoinWeak(DifficultyRecord record)
        {
            var builder = new StringBuilder();
            int count = Mathf.Min(record.WeakQuestions.Count, maxWeakCount);
            for (int i = 0; i < count; i++)
            {
                if (i > 0) builder.Append('　');
                builder.Append(record.WeakQuestions[i]);
            }
            if (record.WeakQuestions.Count > count) builder.Append(" …");
            return builder.ToString();
        }
    }
}
