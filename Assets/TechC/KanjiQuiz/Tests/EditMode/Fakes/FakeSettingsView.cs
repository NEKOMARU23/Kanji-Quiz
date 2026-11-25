using R3;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の設定画面。表示の指示を記録し、入力をテストから送れる
    /// </summary>
    public class FakeSettingsView : ISettingsView
    {
        public readonly Subject<Unit> BackClicked = new Subject<Unit>();
        public readonly Subject<float> SeVolumeChanged = new Subject<float>();
        public readonly Subject<float> BgmVolumeChanged = new Subject<float>();
        public readonly Subject<bool> HiraganaChanged = new Subject<bool>();
        public readonly Subject<Unit> EraseClicked = new Subject<Unit>();
        public readonly Subject<Unit> EraseConfirmed = new Subject<Unit>();
        public readonly Subject<Unit> EraseCancelled = new Subject<Unit>();

        public Observable<Unit> OnBackClicked => BackClicked;
        public Observable<float> OnSeVolumeChanged => SeVolumeChanged;
        public Observable<float> OnBgmVolumeChanged => BgmVolumeChanged;
        public Observable<bool> OnHiraganaChanged => HiraganaChanged;
        public Observable<Unit> OnEraseClicked => EraseClicked;
        public Observable<Unit> OnEraseConfirmed => EraseConfirmed;
        public Observable<Unit> OnEraseCancelled => EraseCancelled;

        public bool IsVisible { get; private set; }
        public float SeVolume { get; private set; }
        public float BgmVolume { get; private set; }
        public bool UseHiragana { get; private set; }
        public bool IsEraseConfirmVisible { get; private set; }
        public bool IsErasedMessageVisible { get; private set; }

        public void SetVisible(bool visible) => IsVisible = visible;
        public void SetSeVolume(float volume) => SeVolume = volume;
        public void SetBgmVolume(float volume) => BgmVolume = volume;
        public void SetHiragana(bool useHiragana) => UseHiragana = useHiragana;
        public void SetEraseConfirmVisible(bool visible) => IsEraseConfirmVisible = visible;
        public void SetErasedMessageVisible(bool visible) => IsErasedMessageVisible = visible;
    }
}
