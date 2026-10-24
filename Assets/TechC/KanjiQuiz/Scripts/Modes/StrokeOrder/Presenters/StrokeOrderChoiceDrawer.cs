using System.Threading;
using Cysharp.Threading.Tasks;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズの問題を描く処理。出題と振り返りの両方の Presenter で使う
    /// </summary>
    public static class StrokeOrderChoiceDrawer
    {
        /// <summary>
        /// 出題する漢字を表示し、すべての選択肢に書き順の図を描く
        /// </summary>
        public static UniTask DrawAsync(IStrokeOrderView view, StrokeOrderQuestion question, CancellationToken ct)
        {
            view.SetKanji(question.Kanji);

            var drawTasks = new UniTask[question.ChoiceCount];
            for (int i = 0; i < question.ChoiceCount; i++)
            {
                drawTasks[i] = view.DrawChoiceAsync(i, question.GetChoiceStrokes(i), ct);
            }
            return UniTask.WhenAll(drawTasks);
        }
    }
}
