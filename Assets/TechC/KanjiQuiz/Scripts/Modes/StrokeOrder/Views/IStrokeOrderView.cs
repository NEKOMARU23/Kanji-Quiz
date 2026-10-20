using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズ固有の表示（出題する漢字と、選択肢ごとの書き順の図）
    /// </summary>
    public interface IStrokeOrderView
    {
        /// <summary>出題する漢字を表示する</summary>
        void SetKanji(string kanji);

        /// <summary>
        /// 選択肢に書き順の図を描く
        /// </summary>
        /// <param name="choiceIndex">選択肢の番号</param>
        /// <param name="strokes">描く順番に並べた画のデータ</param>
        UniTask DrawChoiceAsync(int choiceIndex, IReadOnlyList<StrokeData> strokes, CancellationToken ct);
    }
}
