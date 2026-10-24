namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// プレイ画面を開くときの設定（難易度など）
    /// モード選択画面が設定し、各モードの EntryPoint が読む
    /// 設定されていなければ、EntryPoint の Inspector の値を使う
    /// </summary>
    public static class QuizLaunchOptions
    {
        /// <summary>選んだ難易度。まだ選んでいなければ null</summary>
        public static Difficulty? Difficulty { get; set; }

        /// <summary>設定を消す（テスト用）</summary>
        public static void Clear()
        {
            Difficulty = null;
        }
    }
}
