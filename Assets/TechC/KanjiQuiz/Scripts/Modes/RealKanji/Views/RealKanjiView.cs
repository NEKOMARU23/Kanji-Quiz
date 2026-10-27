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

        public void ShowAnswerInfo(string kanji, string reading, IReadOnlyList<KanjiWord> words)
        {
            if (answerInfoText == null) return;

            var text = new StringBuilder();
            text.Append($"本物は「{kanji}」（{reading}）");
            if (words.Count > 0)
            {
                text.Append("\n使用例：");
                for (int i = 0; i < words.Count; i++)
                {
                    if (i > 0) text.Append("、");
                    text.Append(words[i]);
                }
            }
            answerInfoText.text = text.ToString();
        }
    }
}
