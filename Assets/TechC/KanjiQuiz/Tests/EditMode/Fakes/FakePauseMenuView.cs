using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の中断メニュー。表示の状態を記録し、操作をメソッドで再現する
    /// </summary>
    public class FakePauseMenuView : IPauseMenuView
    {
        private readonly Subject<Unit> onOpenClicked = new Subject<Unit>();
        private readonly Subject<Unit> onMenuKeyPressed = new Subject<Unit>();
        private readonly Subject<Unit> onResumeClicked = new Subject<Unit>();
        private readonly Subject<Unit> onRestartClicked = new Subject<Unit>();
        private readonly Subject<Unit> onQuitClicked = new Subject<Unit>();
        private readonly Subject<float> onSeVolumeChanged = new Subject<float>();

        public Observable<Unit> OnOpenClicked => onOpenClicked;
        public Observable<Unit> OnMenuKeyPressed => onMenuKeyPressed;
        public Observable<Unit> OnResumeClicked => onResumeClicked;
        public Observable<Unit> OnRestartClicked => onRestartClicked;
        public Observable<Unit> OnQuitClicked => onQuitClicked;
        public Observable<float> OnSeVolumeChanged => onSeVolumeChanged;
        public Observable<bool> OnHiraganaChanged => onHiraganaChanged;
        private readonly Subject<bool> onHiraganaChanged = new Subject<bool>();
        public bool? HiraganaShown { get; private set; }
        public void SetHiragana(bool useHiragana) => HiraganaShown = useHiragana;
        public void ToggleHiragana(bool value) => onHiraganaChanged.OnNext(value);

        public bool IsShown { get; private set; }
        public float SliderVolume { get; private set; } = -1f;

        public void Show() => IsShown = true;
        public void Hide() => IsShown = false;
        public void SetSeVolume(float volume) => SliderVolume = volume;

        public void ClickOpen() => onOpenClicked.OnNext(Unit.Default);
        public void PressMenuKey() => onMenuKeyPressed.OnNext(Unit.Default);
        public void ClickResume() => onResumeClicked.OnNext(Unit.Default);
        public void ClickRestart() => onRestartClicked.OnNext(Unit.Default);
        public void ClickQuit() => onQuitClicked.OnNext(Unit.Default);
        public void MoveVolume(float volume) => onSeVolumeChanged.OnNext(volume);
    }
}
