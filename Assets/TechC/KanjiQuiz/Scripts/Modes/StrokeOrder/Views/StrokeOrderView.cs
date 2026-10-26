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
        [Tooltip("苦手な漢字のときに出す「苦手」の印。不要なら空でよい")]
        [SerializeField] private GameObject weakMark;
        [Tooltip("振り返りで、お手本の書き順を書くアニメーション。振り返りのときだけ表示される場所に置く。不要なら空でよい")]
        [SerializeField] private StrokeAnimationView modelAnimation;

        public void SetKanji(string kanji)
        {
            if (kanjiText != null)
            {
                kanjiText.text = kanji;
            }
        }

        public void SetWeakMarkVisible(bool visible)
        {
            if (weakMark != null) weakMark.SetActive(visible);
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

        public UniTask PlayModelAnimationAsync(IReadOnlyList<StrokeData> strokes, CancellationToken ct)
        {
            if (modelAnimation == null) return UniTask.CompletedTask;

            return modelAnimation.PlayAsync(strokes, ct);
        }
    }
}
