using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの Presenter
    /// 進行と判定は ChoiceQuizPresenter に任せ、選択肢の字と読みを描かせ、苦手な漢字に印をつける
    /// </summary>
    public sealed class RealKanjiPresenter : ChoiceQuizPresenter<RealKanjiQuestion>
    {
        private readonly IRealKanjiView realKanjiView;
        private readonly WeakQuestionList weakList;

        public RealKanjiPresenter(
            QuizSession<RealKanjiQuestion> session,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IRealKanjiView realKanjiView,
            IAudioPlayer audioPlayer = null,
            WeakQuestionList weakList = null)
            : base(session, frameView, choiceListView, audioPlayer)
        {
            this.realKanjiView = realKanjiView ?? throw new ArgumentNullException(nameof(realKanjiView));
            this.weakList = weakList;
        }

        protected override UniTask ShowChoicesAsync(RealKanjiQuestion question, CancellationToken ct)
        {
            realKanjiView.SetWeakMarkVisible(weakList != null && weakList.Contains(question.Kanji));
            RealKanjiChoiceDrawer.Draw(realKanjiView, question, false);
            return UniTask.CompletedTask;
        }
    }
}
