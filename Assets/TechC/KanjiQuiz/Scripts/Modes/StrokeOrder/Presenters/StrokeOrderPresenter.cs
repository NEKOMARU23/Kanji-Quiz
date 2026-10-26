using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズの Presenter
    /// 進行と判定は ChoiceQuizPresenter に任せ、選択肢ごとの書き順の図を描かせ、苦手な漢字に印をつける
    /// </summary>
    public sealed class StrokeOrderPresenter : ChoiceQuizPresenter<StrokeOrderQuestion>
    {
        private readonly IStrokeOrderView strokeOrderView;
        private readonly WeakQuestionList weakList;

        public StrokeOrderPresenter(
            QuizSession<StrokeOrderQuestion> session,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IStrokeOrderView strokeOrderView,
            IAudioPlayer audioPlayer = null,
            WeakQuestionList weakList = null)
            : base(session, frameView, choiceListView, audioPlayer)
        {
            this.strokeOrderView = strokeOrderView ?? throw new ArgumentNullException(nameof(strokeOrderView));
            this.weakList = weakList;
        }

        protected override UniTask ShowChoicesAsync(StrokeOrderQuestion question, CancellationToken ct)
        {
            strokeOrderView.SetWeakMarkVisible(weakList != null && weakList.Contains(question.Kanji));
            return StrokeOrderChoiceDrawer.DrawAsync(strokeOrderView, question, ct);
        }
    }
}
