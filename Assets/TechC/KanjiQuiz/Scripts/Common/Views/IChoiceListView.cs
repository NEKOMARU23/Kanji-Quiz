using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 「N 個の選択肢から1つ選ぶ」UI
    /// 選択肢の中身（文字、画像、書き順の図など）はモードごとの View が描く
    /// </summary>
    public interface IChoiceListView
    {
        /// <summary>選択肢が選ばれたとき。値は選ばれた選択肢の番号</summary>
        Observable<int> OnChoiceSelected { get; }

        /// <summary>表示する選択肢の数を設定する</summary>
        void SetChoiceCount(int count);

        /// <summary>
        /// 選択状態を設定する。OnChoiceSelected は発行しない
        /// </summary>
        /// <param name="index">選択する番号。-1 のときはどれも選ばない</param>
        void SetSelected(int index);
    }
}
