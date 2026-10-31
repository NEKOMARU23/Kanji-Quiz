using R3;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトルから開く設定画面の表示（タイトルの黒板に板書する）
    /// </summary>
    public interface ISettingsView
    {
        /// <summary>[← タイトルへ] が押されたとき（Esc を含む）</summary>
        Observable<Unit> OnBackClicked { get; }

        /// <summary>効果音の音量のつまみが動いたとき（0〜1）</summary>
        Observable<float> OnSeVolumeChanged { get; }

        /// <summary>「出題の字のほかは ひらがなで表示」の しない／する が選ばれたとき</summary>
        Observable<bool> OnHiraganaChanged { get; }

        /// <summary>記録の [消す] が押されたとき</summary>
        Observable<Unit> OnEraseClicked { get; }

        /// <summary>確認で [消す] が押されたとき</summary>
        Observable<Unit> OnEraseConfirmed { get; }

        /// <summary>確認で [やめる] が押されたとき</summary>
        Observable<Unit> OnEraseCancelled { get; }

        /// <summary>設定画面を表示するかどうか</summary>
        void SetVisible(bool visible);

        /// <summary>効果音の音量のつまみを合わせる（入力の通知は出さない）</summary>
        void SetSeVolume(float volume);

        /// <summary>しない／する の丸を合わせる（入力の通知は出さない）</summary>
        void SetHiragana(bool useHiragana);

        /// <summary>記録を消すかの確認を出すかどうか</summary>
        void SetEraseConfirmVisible(bool visible);

        /// <summary>記録を消したことを出すかどうか（出さないときは、何を消すかの説明に戻す）</summary>
        void SetErasedMessageVisible(bool visible);
    }
}
