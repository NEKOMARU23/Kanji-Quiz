using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の振り返りの Presenter。選択肢の描画は何もせず、描いた問題を記録する
    /// </summary>
    public class TestChoiceQuizReviewPresenter : ChoiceQuizReviewPresenter<FakeChoiceQuestion>
    {
        public List<FakeChoiceQuestion> ShownQuestions { get; } = new List<FakeChoiceQuestion>();

        public TestChoiceQuizReviewPresenter(
            IReadOnlyList<FakeChoiceQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IQuizReviewView reviewView,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer = null)
            : base(questions, answers, startIndex, frameView, choiceListView, reviewView, sceneLoader, "Result", audioPlayer)
        {
        }

        protected override UniTask ShowChoicesAsync(FakeChoiceQuestion question, CancellationToken ct)
        {
            ShownQuestions.Add(question);
            return UniTask.CompletedTask;
        }
    }
}
