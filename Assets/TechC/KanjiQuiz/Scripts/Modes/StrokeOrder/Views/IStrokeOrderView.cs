using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズ固有の表示（出題する漢字と、選択肢ごとの書き順の図）
    /// </summary>
    public interface IStrokeOrderView
    {
        /// <summary>出題する漢字を表示する</summary>
        void SetKanji(string kanji);

        /// <summary>出題する漢字が苦手な漢字（前に間違えた漢字）であることを示す印を出すかどうか</summary>
        void SetWeakMarkVisible(bool visible);

        /// <summary>
        /// 選択肢に書き順の図を描く
        /// </summary>
        /// <param name="choiceIndex">選択肢の番号</param>
        /// <param name="strokes">描く順番に並べた画のデータ</param>
        UniTask DrawChoiceAsync(int choiceIndex, IReadOnlyList<StrokeData> strokes, CancellationToken ct);

        /// <summary>
        /// お手本として、正しい書き順で1画ずつ書くアニメーションをキャンセルされるまで繰り返す（振り返り用）
        /// </summary>
        /// <param name="strokes">正しい書き順に並べた画のデータ</param>
        UniTask PlayModelAnimationAsync(IReadOnlyList<StrokeData> strokes, CancellationToken ct);
    }
}
