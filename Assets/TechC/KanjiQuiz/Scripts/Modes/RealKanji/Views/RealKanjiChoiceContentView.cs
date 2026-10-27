using System.Collections.Generic;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの選択肢1つの中身（線で描いた字、その下の読み、振り返りの説明）
    /// 選択肢プレハブ（RealKanjiChoice）の ContentRoot の下に置く
    /// </summary>
    public class RealKanjiChoiceContentView : MonoBehaviour
    {
        [SerializeField] private KanjiGlyphRenderer glyphRenderer;
        [Tooltip("字の下に出す読み")]
        [SerializeField] private TMP_Text readingText;
        [Tooltip("振り返りで出す説明（本物は漢字、偽物は部品の組み合わせ）。不要なら空でよい")]
        [SerializeField] private TMP_Text descriptionText;

        /// <summary>
        /// 字と読みを表示する
        /// </summary>
        /// <param name="description">振り返りの説明。null なら隠す</param>
        public void Show(IReadOnlyList<StrokeData> strokes, string reading, string description)
        {
            glyphRenderer.Draw(strokes);
            readingText.text = reading;

            if (descriptionText == null) return;
            descriptionText.gameObject.SetActive(description != null);
            descriptionText.text = description ?? string.Empty;
        }
    }
}
