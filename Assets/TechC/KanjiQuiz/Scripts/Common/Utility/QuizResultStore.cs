namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// シーンをまたいでプレイの結果を渡すための置き場所
    /// プレイ画面が結果を置き、結果画面が読む
    /// </summary>
    public static class QuizResultStore
    {
        /// <summary>直前のプレイの結果。まだプレイしていなければ null</summary>
        public static QuizResult Last { get; private set; }

        /// <summary>結果を置く</summary>
        public static void Set(QuizResult result)
        {
            Last = result;
        }

        /// <summary>置いた結果を消す（テスト用）</summary>
        public static void Clear()
        {
            Last = null;
        }
    }
}
