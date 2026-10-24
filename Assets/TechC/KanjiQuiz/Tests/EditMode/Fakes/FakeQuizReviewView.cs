using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の振り返りの部品。表示状態を記録し、ボタン操作をメソッドで再現する
    /// </summary>
    public class FakeQuizReviewView : IQuizReviewView
    {
        private readonly Subject<Unit> onBackClicked = new Subject<Unit>();

        public Observable<Unit> OnBackClicked => onBackClicked;

        public bool IsVisible { get; private set; }
        public AnswerState? AnswerState { get; private set; }

        public void SetVisible(bool visible) => IsVisible = visible;
        public void SetAnswerState(AnswerState state) => AnswerState = state;

        public void ClickBack() => onBackClicked.OnNext(Unit.Default);
    }
}
