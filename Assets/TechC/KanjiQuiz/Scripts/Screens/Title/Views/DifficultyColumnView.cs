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
        [Tooltip("苦手な問題が2字以上（四字熟語など）のときの字の大きさ")]
        [SerializeField] private float longWeakFontSize = 34f;
        [Tooltip("苦手な問題が2字以上（四字熟語など）のとき、いくつまで書くか")]
        [Min(1)] [SerializeField] private int maxLongWeakCount = 4;
        [Tooltip("選んでいない列の濃さ")]
        [Range(0f, 1f)] [SerializeField] private float unselectedAlpha = 0.55f;
        [SerializeField] private Color weakColor = new Color(0.95f, 0.66f, 0.63f, 1f);
        [SerializeField] private Color mutedColor = new Color(0.95f, 0.94f, 0.9f, 0.45f);

        /// <summary>列がクリックされたとき</summary>
        public Observable<Unit> OnClicked => button.OnClickAsObservable();

        /// <summary>列の内容を書く</summary>
        /// <param name="isFirst">一番左の列かどうか（左の線を隠す）</param>
        /// <param name="questionNoun">問題の呼び方（「苦手な○○」に使う。例：漢字、熟語）</param>
        public void Set(DifficultyRecord record, bool isFirst, string questionNoun = "漢字")
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
            weakLabelText.text = hasWeak ? $"苦手な{questionNoun}（次に出ます）" : $"苦手な{questionNoun}";
            weakLabelText.color = hasWeak ? weakColor : mutedColor;
            weakText.text = hasWeak ? JoinWeak(record) : "なし";
            weakText.color = hasWeak ? weakColor : mutedColor;
            weakText.fontSize = !hasWeak ? noWeakFontSize : HasLongWeak(record) ? longWeakFontSize : weakFontSize;
        }

        /// <summary>選んでいる列かどうかを切り替える</summary>
        public void SetSelected(bool selected)
        {
            selectedCircle.SetActive(selected);
            canvasGroup.alpha = selected ? 1f : unselectedAlpha;
        }

        /// <summary>苦手な問題に2字以上のもの（四字熟語など）があるかどうか</summary>
        private static bool HasLongWeak(DifficultyRecord record)
        {
            foreach (var question in record.WeakQuestions)
            {
                if (question.Length > 1) return true;
            }
            return false;
        }

        private string JoinWeak(DifficultyRecord record)
        {
            // 2字以上の問題は字を小さくして数を減らし、1つの問題の途中で改行しないようにする
            bool isLong = HasLongWeak(record);
            var builder = new StringBuilder();
            int count = Mathf.Min(record.WeakQuestions.Count, isLong ? maxLongWeakCount : maxWeakCount);
            for (int i = 0; i < count; i++)
            {
                if (i > 0) builder.Append('　');
                if (isLong) builder.Append("<nobr>").Append(record.WeakQuestions[i]).Append("</nobr>");
                else builder.Append(record.WeakQuestions[i]);
            }
            if (record.WeakQuestions.Count > count) builder.Append(" …");
            return builder.ToString();
        }
    }
}
