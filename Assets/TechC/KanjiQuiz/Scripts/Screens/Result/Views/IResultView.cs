using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面の表示
    /// </summary>
    public interface IResultView
    {
        /// <summary>[もう一度] が押されたとき</summary>
        Observable<Unit> OnRetryClicked { get; }

        /// <summary>[問題を振り返る] が押されたとき</summary>
        Observable<Unit> OnReviewClicked { get; }

        /// <summary>問題ごとの結果がクリックされたとき。値は問題の番号（0 始まり）</summary>
        Observable<int> OnItemClicked { get; }

        /// <summary>モードの名前を表示する</summary>
        void SetModeName(string modeName);

        /// <summary>正解数を表示する</summary>
        void SetScore(int correctCount, int total);

        /// <summary>問題ごとの結果を一覧で表示する</summary>
        void SetItems(IReadOnlyList<QuizResultItem> items);

        /// <summary>問題を振り返れるかどうか（クリックできるか、案内を出すか）を切り替える</summary>
        void SetReviewAvailable(bool available);

        /// <summary>結果がないこと（直接このシーンを開いた場合など）を表示する</summary>
        void ShowNoResult();
    }
}
