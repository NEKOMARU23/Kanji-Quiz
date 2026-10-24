using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 全モード共通のプレイ画面の枠（進行表示、次へ・前へボタン）
    /// </summary>
    public interface IQuizFrameView
    {
        /// <summary>[次へ] が押されたとき</summary>
        Observable<Unit> OnNextClicked { get; }

        /// <summary>[前へ] が押されたとき</summary>
        Observable<Unit> OnPrevClicked { get; }

        /// <summary>画面の上に出すタイトル（モード名や難易度）を表示する</summary>
        void SetTitle(string title);

        /// <summary>
        /// 進行状況を表示する
        /// </summary>
        /// <param name="current">現在の問題番号（1 始まり）</param>
        /// <param name="total">問題の数</param>
        void SetProgress(int current, int total);

        /// <summary>[次へ] を押せるかどうかを切り替える</summary>
        void SetNextInteractable(bool interactable);

        /// <summary>[前へ] を押せるかどうかを切り替える</summary>
        void SetPrevInteractable(bool interactable);
    }
}
