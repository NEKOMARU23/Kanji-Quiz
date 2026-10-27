using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.StrokeOrder;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の書き順クイズの表示。表示した内容を記録し、描画はすぐに終わる
    /// </summary>
    public class FakeStrokeOrderView : IStrokeOrderView
    {
        public string Kanji { get; private set; }
        public bool IsWeakMarkVisible { get; private set; }

        /// <summary>お手本のアニメーションを始めた回数</summary>
        public int ModelAnimationCount { get; private set; }

        public void SetKanji(string kanji) => Kanji = kanji;

        public void SetWeakMarkVisible(bool visible) => IsWeakMarkVisible = visible;

        public UniTask DrawChoiceAsync(int choiceIndex, IReadOnlyList<StrokeData> strokes, CancellationToken ct) => UniTask.CompletedTask;

        public UniTask PlayModelAnimationAsync(IReadOnlyList<StrokeData> strokes, CancellationToken ct)
        {
            ModelAnimationCount++;
            return UniTask.CompletedTask;
        }
    }
}
