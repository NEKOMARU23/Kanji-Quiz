using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Result;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の結果画面。表示された内容を記録し、ボタン操作をメソッドで再現する
    /// </summary>
    public class FakeResultView : IResultView
    {
        private readonly Subject<Unit> onRetryClicked = new Subject<Unit>();

        public Observable<Unit> OnRetryClicked => onRetryClicked;

        public string ModeName { get; private set; }
        public int CorrectCount { get; private set; } = -1;
        public int Total { get; private set; } = -1;
        public IReadOnlyList<QuizResultItem> Items { get; private set; }
        public bool IsNoResultShown { get; private set; }

        public void SetModeName(string modeName) => ModeName = modeName;

        public void SetScore(int correctCount, int total)
        {
            CorrectCount = correctCount;
            Total = total;
        }

        public void SetItems(IReadOnlyList<QuizResultItem> items) => Items = items;

        public void ShowNoResult() => IsNoResultShown = true;

        public void ClickRetry() => onRetryClicked.OnNext(Unit.Default);
    }
}
