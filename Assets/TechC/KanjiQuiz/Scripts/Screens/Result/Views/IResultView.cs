using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        /// <summary>結果発表の演出中に画面がクリックされたとき（演出を飛ばす）</summary>
        Observable<Unit> OnSkipRequested { get; }

        /// <summary>モードの名前を表示する</summary>
        void SetModeName(string modeName);

        /// <summary>
        /// 問題ごとの結果を一覧で表示する。○× はまだ見せない
        /// </summary>
        void SetItems(IReadOnlyList<QuizResultItem> items);

        /// <summary>
        /// 1問分の ○× を演出つきで見せる。次の問題を見せるまでの間も含めて待つ
        /// </summary>
        /// <param name="index">問題の番号（0 始まり）</param>
        UniTask RevealItemAsync(int index, CancellationToken ct);

        /// <summary>すべての問題の ○× を演出なしで見せる</summary>
        void RevealAllItems();

        /// <summary>
        /// 点数が決まるまで、数字をルーレットのように回す
        /// </summary>
        /// <param name="total">問題の数（回す数字の上限）</param>
        UniTask PlayScoreRouletteAsync(int total, CancellationToken ct);

        /// <summary>正解数を表示する</summary>
        /// <param name="animate">数字が出る演出をするかどうか</param>
        void ShowScore(int correctCount, int total, bool animate);

        /// <summary>
        /// ボタンと案内を表示するかどうかを切り替える。演出中は隠す
        /// </summary>
        void SetMenuVisible(bool visible);

        /// <summary>問題を振り返れるかどうか（クリックできるか、案内を出すか）を切り替える</summary>
        void SetReviewAvailable(bool available);

        /// <summary>結果がないこと（直接このシーンを開いた場合など）を表示する</summary>
        void ShowNoResult();
    }
}
