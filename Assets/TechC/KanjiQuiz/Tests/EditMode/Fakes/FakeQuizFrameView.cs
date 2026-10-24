using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のプレイ画面の枠。ボタン操作をメソッドで再現し、表示の状態を記録する
    /// </summary>
    public class FakeQuizFrameView : IQuizFrameView
    {
        private readonly Subject<Unit> onNextClicked = new Subject<Unit>();
        private readonly Subject<Unit> onPrevClicked = new Subject<Unit>();

        public Observable<Unit> OnNextClicked => onNextClicked;
        public Observable<Unit> OnPrevClicked => onPrevClicked;

        public string Title { get; private set; }
        public int ProgressCurrent { get; private set; }
        public int ProgressTotal { get; private set; }
        public bool IsNextInteractable { get; private set; }
        public bool IsPrevInteractable { get; private set; }

        public void SetTitle(string title) => Title = title;

        public void SetProgress(int current, int total)
        {
            ProgressCurrent = current;
            ProgressTotal = total;
        }

        public void SetNextInteractable(bool interactable) => IsNextInteractable = interactable;
        public void SetPrevInteractable(bool interactable) => IsPrevInteractable = interactable;

        public void ClickNext() => onNextClicked.OnNext(Unit.Default);
        public void ClickPrev() => onPrevClicked.OnNext(Unit.Default);
    }
}
