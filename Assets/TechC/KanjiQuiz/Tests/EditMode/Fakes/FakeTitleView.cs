using R3;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のタイトル画面。表示の指示を記録し、入力をテストから送れる
    /// </summary>
    public class FakeTitleView : ITitleView
    {
        public readonly Subject<Unit> StartClicked = new Subject<Unit>();
        public readonly Subject<Unit> QuitClicked = new Subject<Unit>();

        public Observable<Unit> OnStartClicked => StartClicked;
        public Observable<Unit> OnQuitClicked => QuitClicked;

        public bool IsVisible { get; private set; }

        public void SetVisible(bool visible) => IsVisible = visible;
    }
}
