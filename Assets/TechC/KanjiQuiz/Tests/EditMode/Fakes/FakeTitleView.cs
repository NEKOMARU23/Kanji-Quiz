using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
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
        public readonly Subject<Unit> SettingsClicked = new Subject<Unit>();
        public readonly Subject<Unit> KanjiBookClicked = new Subject<Unit>();
        public readonly Subject<Unit> CreditsClicked = new Subject<Unit>();
        public readonly Subject<SeType> EffectSound = new Subject<SeType>();
        public readonly Subject<bool> ChalkWriting = new Subject<bool>();

        public Observable<Unit> OnStartClicked => StartClicked;
        public Observable<Unit> OnQuitClicked => QuitClicked;
        public Observable<Unit> OnSettingsClicked => SettingsClicked;
        public Observable<Unit> OnKanjiBookClicked => KanjiBookClicked;
        public Observable<Unit> OnCreditsClicked => CreditsClicked;
        public Observable<SeType> OnEffectSound => EffectSound;
        public Observable<bool> OnChalkWriting => ChalkWriting;

        public bool IsVisible { get; private set; }
        public int IntroCount { get; private set; }
        public int ExitCount { get; private set; }
        public int ShowWithoutIntroCount { get; private set; }
        public TitleMenuItem LastExitPressed { get; private set; }
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

        public void ShowWithoutIntro(IReadOnlyList<IReadOnlyList<string>> logoStrokePaths)
        {
            IsVisible = true;
            ShowWithoutIntroCount++;
            LastLogoStrokePaths = logoStrokePaths;
        }

        public UniTask<bool> PlayExitAsync(TitleMenuItem pressed, CancellationToken ct)
        {
            ExitCount++;
            LastExitPressed = pressed;
            return UniTask.FromResult(SkipExit);
        }
    }
}
