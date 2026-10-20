using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の選択式 Presenter。選択肢の描画は何もせず、呼ばれた回数だけ数える
    /// </summary>
    public class TestChoiceQuizPresenter : ChoiceQuizPresenter<FakeChoiceQuestion>
    {
        public int ShowCount { get; private set; }

        public TestChoiceQuizPresenter(QuizSession<FakeChoiceQuestion> session, IQuizFrameView frameView, IChoiceListView choiceListView)
            : base(session, frameView, choiceListView)
        {
        }

        protected override UniTask ShowChoicesAsync(FakeChoiceQuestion question, CancellationToken ct)
        {
            ShowCount++;
            return UniTask.CompletedTask;
        }
    }
}
