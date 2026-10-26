using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズの問題の振り返りの Presenter
    /// 進行と印は ChoiceQuizReviewPresenter に任せ、書き順の図とお手本のアニメーションを描かせる
    /// </summary>
    public sealed class StrokeOrderReviewPresenter : ChoiceQuizReviewPresenter<StrokeOrderQuestion>
    {
        private readonly IStrokeOrderView strokeOrderView;

        public StrokeOrderReviewPresenter(
            IReadOnlyList<StrokeOrderQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IQuizReviewView reviewView,
            IStrokeOrderView strokeOrderView,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer = null)
            : base(questions, answers, startIndex, frameView, choiceListView, reviewView, sceneLoader, SceneNames.Result, audioPlayer)
        {
            this.strokeOrderView = strokeOrderView ?? throw new ArgumentNullException(nameof(strokeOrderView));
        }

        protected override UniTask ShowChoicesAsync(StrokeOrderQuestion question, CancellationToken ct)
        {
            strokeOrderView.SetWeakMarkVisible(false);

            // お手本はキャンセルされる（別の問題へ移る）まで繰り返すので、待たない
            strokeOrderView.PlayModelAnimationAsync(question.Strokes, ct).Forget();
            return StrokeOrderChoiceDrawer.DrawAsync(strokeOrderView, question, ct);
        }
    }
}
