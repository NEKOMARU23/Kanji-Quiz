using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズ固有の表示
    /// 選択肢プレハブの中にある CumulativeStrokeRenderer に書き順の図を描かせる
    /// </summary>
    public class StrokeOrderView : MonoBehaviour, IStrokeOrderView
    {
        [SerializeField] private ChoiceListView choiceListView;
        [Tooltip("出題する漢字を表示するテキスト。日本語フォントが必要。不要なら空でよい")]
        [SerializeField] private TMP_Text kanjiText;

        public void SetKanji(string kanji)
        {
            if (kanjiText != null)
            {
                kanjiText.text = kanji;
            }
        }

        public UniTask DrawChoiceAsync(int choiceIndex, IReadOnlyList<StrokeData> strokes, CancellationToken ct)
        {
            var item = choiceListView.Items[choiceIndex];
            var strokeRenderer = item.GetComponentInChildren<CumulativeStrokeRenderer>(true);
            if (strokeRenderer == null)
            {
                Debug.LogError($"[StrokeOrderView] 選択肢 {choiceIndex} に CumulativeStrokeRenderer がありません。");
                return UniTask.CompletedTask;
            }

            return strokeRenderer.DrawAsync(strokes, ct);
        }
    }
}
