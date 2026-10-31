using R3;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のクレジット画面。表示の指示を記録し、入力をテストから送れる
    /// </summary>
    public class FakeCreditsView : ICreditsView
    {
        public readonly Subject<Unit> BackClicked = new Subject<Unit>();

        public Observable<Unit> OnBackClicked => BackClicked;

        public bool IsVisible { get; private set; }

        public void SetVisible(bool visible) => IsVisible = visible;
    }
}
