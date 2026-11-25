namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 難易度の表示用の処理
    /// </summary>
    public static class DifficultyExtensions
    {
        /// <summary>
        /// 画面に表示する難易度の名前
        /// </summary>
        public static string ToDisplayName(this Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Normal: return "常識";
                case Difficulty.Hard: return "困難";
                default: return "簡単";
            }
        }

        /// <summary>
        /// 画面に表示する難易度の名前に、読みの印（{漢字|よみ}）をつけたもの
        /// ひらがなで表示する設定のとき、画面の文字が自動でひらがなになる（RubyLabelView）
        /// </summary>
        public static string ToMarkedName(this Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Normal: return "{常識|じょうしき}";
                case Difficulty.Hard: return "{困難|こんなん}";
                default: return "{簡単|かんたん}";
            }
        }
    }
}
