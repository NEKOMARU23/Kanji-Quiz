using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 問題の振り返りのときだけ表示する部品（結果に戻るボタン、その問題の正誤）
    /// </summary>
    public interface IQuizReviewView
    {
        /// <summary>[結果に戻る] が押されたとき</summary>
        Observable<Unit> OnBackClicked { get; }

        /// <summary>表示するかどうかを切り替える</summary>
        void SetVisible(bool visible);

        /// <summary>表示中の問題の正誤を表示する</summary>
        void SetAnswerState(AnswerState state);
    }
}
