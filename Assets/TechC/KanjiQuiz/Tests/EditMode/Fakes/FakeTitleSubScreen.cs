using R3;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のメニューから開く画面。開いたことを記録し、閉じたことをテストから送れる
    /// </summary>
    public class FakeTitleSubScreen : ITitleSubScreen
    {
        public readonly Subject<Unit> Closed = new Subject<Unit>();

        public Observable<Unit> OnClosed => Closed;
        public bool IsInitialized { get; private set; }
        public int OpenCount { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Initialize() => IsInitialized = true;
        public void Open() => OpenCount++;
        public void Dispose() => IsDisposed = true;
    }
}
