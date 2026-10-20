using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズの Presenter
    /// 進行と判定は ChoiceQuizPresenter に任せ、選択肢ごとの書き順の図を描かせるだけを行う
    /// </summary>
    public sealed class StrokeOrderPresenter : ChoiceQuizPresenter<StrokeOrderQuestion>
    {
        private readonly IStrokeOrderView strokeOrderView;

        public StrokeOrderPresenter(
            QuizSession<StrokeOrderQuestion> session,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IStrokeOrderView strokeOrderView)
            : base(session, frameView, choiceListView)
        {
            this.strokeOrderView = strokeOrderView ?? throw new ArgumentNullException(nameof(strokeOrderView));
        }

        protected override UniTask ShowChoicesAsync(StrokeOrderQuestion question, CancellationToken ct)
        {
            strokeOrderView.SetKanji(question.Kanji);

            var drawTasks = new UniTask[question.ChoiceCount];
            for (int i = 0; i < question.ChoiceCount; i++)
            {
                drawTasks[i] = strokeOrderView.DrawChoiceAsync(i, question.GetChoiceStrokes(i), ct);
            }
            return UniTask.WhenAll(drawTasks);
        }
    }
}
