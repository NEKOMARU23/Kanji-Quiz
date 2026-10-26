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

        /// <summary>
        /// 結果発表の演出をまだ見せていないかどうか
        /// 結果を置いたときに true になり、振り返りから結果画面に戻ったときは false のまま
        /// </summary>
        public static bool IsRevealPending { get; private set; }

        /// <summary>結果を置く</summary>
        public static void Set(QuizResult result)
        {
            Last = result;
            IsRevealPending = result != null;
        }

        /// <summary>結果発表の演出を見せたことを記録する</summary>
        public static void MarkRevealed()
        {
            IsRevealPending = false;
        }

        /// <summary>置いた結果を消す（テスト用）</summary>
        public static void Clear()
        {
            Last = null;
            IsRevealPending = false;
        }
    }
}
