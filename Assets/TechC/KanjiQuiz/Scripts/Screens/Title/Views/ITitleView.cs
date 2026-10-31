using R3;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトル画面の表示
    /// </summary>
    public interface ITitleView
    {
        /// <summary>[はじめる] が押されたとき（Enter を含む）</summary>
        Observable<Unit> OnStartClicked { get; }

        /// <summary>[おわる] が押されたとき</summary>
        Observable<Unit> OnQuitClicked { get; }

        /// <summary>タイトル画面を表示するかどうか</summary>
        void SetVisible(bool visible);
    }
}
