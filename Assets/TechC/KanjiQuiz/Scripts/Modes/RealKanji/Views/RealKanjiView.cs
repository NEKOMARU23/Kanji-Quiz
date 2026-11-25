using System.Collections.Generic;
using System.Text;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがし固有の表示
    /// 選択肢プレハブの中にある RealKanjiChoiceContentView に字と読みを描かせる
    /// </summary>
    public class RealKanjiView : MonoBehaviour, IRealKanjiView
    {
        [SerializeField] private ChoiceListView choiceListView;
        [Tooltip("苦手な漢字のときに出す「苦手」の印。不要なら空でよい")]
        [SerializeField] private GameObject weakMark;
        [Tooltip("振り返りで、本物の漢字の読みと使用例を出すテキスト。振り返りのときだけ表示される場所に置く")]
        [SerializeField] private TMP_Text answerInfoText;
        [Tooltip("例文の中の本物の漢字の色")]
        [SerializeField] private Color highlightColor = new Color(0.78f, 0.29f, 0.19f, 1f);
        [Tooltip("問題文。振り返りでは、同じ場所に読みと使用例を出すので隠す。不要なら空でよい")]
        [SerializeField] private GameObject questionText;

        public void SetWeakMarkVisible(bool visible)
        {
            if (weakMark != null) weakMark.SetActive(visible);
        }

        public void ShowChoice(int choiceIndex, RealKanjiChoice choice, bool showDescription)
        {
            var item = choiceListView.Items[choiceIndex];
            var content = item.GetComponentInChildren<RealKanjiChoiceContentView>(true);
            if (content == null)
            {
                Debug.LogError($"[RealKanjiView] 選択肢 {choiceIndex} に RealKanjiChoiceContentView がありません。");
                return;
            }

            content.Show(choice.Strokes, choice.Reading, showDescription ? choice.Description : null);
        }

        public void ShowAnswerInfo(string kanji, string reading, string meaning, string example, IReadOnlyList<KanjiWord> words)
        {
            if (questionText != null) questionText.SetActive(false);
            if (answerInfoText == null) return;

            // 1行目：読みと使用例、2行目：意味、3行目：例文（選択肢の上に収まるように3行にする）
            var text = new StringBuilder();
            text.Append($"{{本物|ほんもの}}は「{kanji}」（{reading}）");
            if (words.Count > 0)
            {
                text.Append("　{使用例|しようれい}：");
                for (int i = 0; i < words.Count; i++)
                {
                    if (i > 0) text.Append("、");
                    text.Append(words[i]);
                }
            }
            if (!string.IsNullOrEmpty(meaning)) text.Append("\n{意味|いみ}：").Append(meaning);
            if (!string.IsNullOrEmpty(example))
            {
                // 例文の中の本物の漢字を強調する
                string color = ColorUtility.ToHtmlStringRGB(highlightColor);
                text.Append("\n{例文|れいぶん}：").Append(example.Replace(kanji, $"<color=#{color}>{kanji}</color>"));
            }
            answerInfoText.text = text.ToString();
        }
    }
}
