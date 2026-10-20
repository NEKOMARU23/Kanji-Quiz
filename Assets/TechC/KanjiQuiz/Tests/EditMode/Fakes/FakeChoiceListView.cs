using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の選択肢 View。選択操作をメソッドで再現し、表示の状態を記録する
    /// </summary>
    public class FakeChoiceListView : IChoiceListView
    {
        private readonly Subject<int> onChoiceSelected = new Subject<int>();

        public Observable<int> OnChoiceSelected => onChoiceSelected;

        public int ChoiceCount { get; private set; }
        public int SelectedIndex { get; private set; } = -1;

        public void SetChoiceCount(int count) => ChoiceCount = count;
        public void SetSelected(int index) => SelectedIndex = index;

        public void Select(int index)
        {
            SelectedIndex = index;
            onChoiceSelected.OnNext(index);
        }
    }
}
