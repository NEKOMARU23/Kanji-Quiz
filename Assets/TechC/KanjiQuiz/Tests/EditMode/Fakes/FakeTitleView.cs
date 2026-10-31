using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のタイトル画面。表示の指示を記録し、入力をテストから送れる。演出はすぐに終わる
    /// </summary>
    public class FakeTitleView : ITitleView
    {
        public readonly Subject<Unit> StartClicked = new Subject<Unit>();
        public readonly Subject<Unit> QuitClicked = new Subject<Unit>();

        public Observable<Unit> OnStartClicked => StartClicked;
        public Observable<Unit> OnQuitClicked => QuitClicked;

        public bool IsVisible { get; private set; }
        public int IntroCount { get; private set; }
        public int ExitCount { get; private set; }
        public IReadOnlyList<IReadOnlyList<string>> LastLogoStrokePaths { get; private set; }

        /// <summary>PlayExitAsync が「飛ばされた」と返すかどうか</summary>
        public bool SkipExit { get; set; }

        /// <summary>true の間は PlayIntroAsync が終わらない（演出中の操作を確かめる）</summary>
        public bool HoldIntro { get; set; }

        public void SetVisible(bool visible) => IsVisible = visible;

        public UniTask PlayIntroAsync(IReadOnlyList<IReadOnlyList<string>> logoStrokePaths, CancellationToken ct)
        {
            IntroCount++;
            LastLogoStrokePaths = logoStrokePaths;
            return HoldIntro ? UniTask.Never(ct) : UniTask.CompletedTask;
        }

        public UniTask<bool> PlayExitAsync(CancellationToken ct)
        {
            ExitCount++;
            return UniTask.FromResult(SkipExit);
        }
    }
}
