using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 中断メニュー（続ける、最初からやり直す、やめる、効果音の音量）
    /// </summary>
    public interface IPauseMenuView
    {
        /// <summary>[メニュー] ボタンが押されたとき</summary>
        Observable<Unit> OnOpenClicked { get; }

        /// <summary>メニューのキー（Esc）が押されたとき。開いているかどうかに関係なく通知する</summary>
        Observable<Unit> OnMenuKeyPressed { get; }

        /// <summary>[続ける] が押されたとき</summary>
        Observable<Unit> OnResumeClicked { get; }

        /// <summary>[最初からやり直す] が押されたとき</summary>
        Observable<Unit> OnRestartClicked { get; }

        /// <summary>[やめる]（モード選択へ）が押されたとき</summary>
        Observable<Unit> OnQuitClicked { get; }

        /// <summary>効果音の音量のつまみが動かされたとき。値は 0〜1</summary>
        Observable<float> OnSeVolumeChanged { get; }

        /// <summary>メニューを開く。開いている間は後ろの画面のキー操作を止める</summary>
        void Show();

        /// <summary>メニューを閉じる</summary>
        void Hide();

        /// <summary>効果音の音量のつまみの位置を設定する。通知はしない</summary>
        void SetSeVolume(float volume);
    }
}
