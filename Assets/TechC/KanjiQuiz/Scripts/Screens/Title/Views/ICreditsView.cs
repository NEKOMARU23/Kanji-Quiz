using R3;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// クレジット画面の表示（教室の掲示板に、使わせてもらったデータやフォントのおたよりを貼る）
    /// </summary>
    public interface ICreditsView
    {
        /// <summary>[← タイトルへ] が押されたとき（Esc を含む）</summary>
        Observable<Unit> OnBackClicked { get; }

        /// <summary>クレジット画面を表示するかどうか</summary>
        void SetVisible(bool visible);
    }
}
