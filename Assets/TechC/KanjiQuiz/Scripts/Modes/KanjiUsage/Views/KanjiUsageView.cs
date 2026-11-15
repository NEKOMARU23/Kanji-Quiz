using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分け固有の表示
    /// 文の空いたところを色つきの文字に置き換えて出し、選択肢プレハブの中の KanjiUsageChoiceContentView に言葉を描かせる
    /// </summary>
    public class KanjiUsageView : MonoBehaviour, IKanjiUsageView
    {
        [SerializeField] private ChoiceListView choiceListView;
        [Tooltip("文を出すテキスト")]
        [SerializeField] private TMP_Text sentenceText;
        [Tooltip("文の下の案内")]
        [SerializeField] private TMP_Text hintText;
        [Tooltip("苦手な言葉のときに出す「苦手」の印。不要なら空でよい")]
        [SerializeField] private GameObject weakMark;
        [Tooltip("出題で、空いたところに出す（読み）の色")]
        [SerializeField] private Color blankColor = new Color(0.71f, 0.40f, 0.11f, 1f);
        [Tooltip("振り返りで、空いたところに入れた正解の言葉の色")]
        [SerializeField] private Color answerColor = new Color(0.11f, 0.48f, 0.23f, 1f);

        public void SetWeakMarkVisible(bool visible)
        {
            if (weakMark != null) weakMark.SetActive(visible);
        }

        public void ShowSentence(string text, string blankText, bool isAnswer)
        {
            string color = ColorUtility.ToHtmlStringRGB(isAnswer ? answerColor : blankColor);
            string filled = isAnswer ? $"<u><b>{blankText}</b></u>" : $"<b>{blankText}</b>";
            sentenceText.text = text.Replace(KanjiUsageQuestion.Blank, $"<color=#{color}>{filled}</color>");
        }

        public void SetHint(string hint)
        {
            if (hintText != null) hintText.text = hint;
        }

        public void ShowChoice(int choiceIndex, string word, string meaning)
        {
            var item = choiceListView.Items[choiceIndex];
            var content = item.GetComponentInChildren<KanjiUsageChoiceContentView>(true);
            if (content == null)
            {
                Debug.LogError($"[KanjiUsageView] 選択肢 {choiceIndex} に KanjiUsageChoiceContentView がありません。");
                return;
            }

            content.Show(word, meaning);
        }
    }
}
