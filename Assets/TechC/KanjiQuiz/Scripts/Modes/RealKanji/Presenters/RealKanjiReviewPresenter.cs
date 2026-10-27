using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの問題の振り返りの Presenter
    /// 進行と印は ChoiceQuizReviewPresenter に任せ、選択肢の字と説明、本物の読みと使用例を出させる
    /// </summary>
    public sealed class RealKanjiReviewPresenter : ChoiceQuizReviewPresenter<RealKanjiQuestion>
    {
        private readonly IRealKanjiView realKanjiView;

        public RealKanjiReviewPresenter(
            IReadOnlyList<RealKanjiQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IQuizReviewView reviewView,
            IRealKanjiView realKanjiView,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer = null)
            : base(questions, answers, startIndex, frameView, choiceListView, reviewView, sceneLoader, SceneNames.Result, audioPlayer)
        {
            this.realKanjiView = realKanjiView ?? throw new ArgumentNullException(nameof(realKanjiView));
        }

        protected override UniTask ShowChoicesAsync(RealKanjiQuestion question, CancellationToken ct)
        {
            realKanjiView.SetWeakMarkVisible(false);
            realKanjiView.ShowAnswerInfo(question.Kanji, question.Reading, question.Words);
            RealKanjiChoiceDrawer.Draw(realKanjiView, question, true);
            return UniTask.CompletedTask;
        }
    }
}
