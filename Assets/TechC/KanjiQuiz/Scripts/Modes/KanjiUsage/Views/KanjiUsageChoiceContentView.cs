using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分けの選択肢1つの中身（言葉、振り返りで出す意味）
    /// 選択肢プレハブ（KanjiUsageChoice）の ContentRoot の下に置く
    /// </summary>
    public class KanjiUsageChoiceContentView : MonoBehaviour
    {
        [Tooltip("選択肢の言葉")]
        [SerializeField] private TMP_Text wordText;
        [Tooltip("振り返りで出す意味。不要なら空でよい")]
        [SerializeField] private TMP_Text meaningText;

        /// <summary>
        /// 言葉を表示する
        /// </summary>
        /// <param name="meaning">意味。null なら隠す</param>
        public void Show(string word, string meaning)
        {
            wordText.text = word;

            if (meaningText == null) return;
            bool hasMeaning = !string.IsNullOrEmpty(meaning);
            meaningText.gameObject.SetActive(hasMeaning);
            meaningText.text = hasMeaning ? meaning : string.Empty;
        }
    }
}
