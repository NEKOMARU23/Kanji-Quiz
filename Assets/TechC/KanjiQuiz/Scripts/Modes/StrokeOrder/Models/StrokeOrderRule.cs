using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズの難易度ごとのルール
    /// 出題範囲（漢字セット）は StrokeOrderEntryPoint で難易度ごとに設定する
    /// </summary>
    public static class StrokeOrderRule
    {
        /// <summary>
        /// 1回のプレイの問題数
        /// </summary>
        public static int QuestionCount(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Normal: return 7;
                case Difficulty.Hard: return 7;
                default: return 5;
            }
        }

        /// <summary>
        /// 1回のプレイで優先して出す苦手な漢字の数の上限（問題数の半分）
        /// 苦手な漢字ばかりにならないように、残りは新しい漢字にする
        /// </summary>
        public static int WeakQuestionCount(Difficulty difficulty)
        {
            return QuestionCount(difficulty) / 2;
        }
    }
}
