using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 難易度ごとの、空けるマスの数とまぎらわしい字の数
    /// 簡単：1マス・候補4、常識：2マス・候補6、困難：4マス・候補12（Docs/GameSpec.md 2.1）
    /// </summary>
    public readonly struct FourCharIdiomRule
    {
        /// <summary>熟語の字数</summary>
        public const int Length = 4;

        /// <summary>空けるマスの数</summary>
        public int BlankCount { get; }

        /// <summary>候補に混ぜるまぎらわしい字の数</summary>
        public int DecoyCount { get; }

        /// <summary>候補の字の数（空けたマスの正解の字とまぎらわしい字）</summary>
        public int TileCount => BlankCount + DecoyCount;

        public FourCharIdiomRule(int blankCount, int decoyCount)
        {
            BlankCount = blankCount;
            DecoyCount = decoyCount;
        }

        /// <summary>難易度の決まりを返す</summary>
        public static FourCharIdiomRule For(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Normal: return new FourCharIdiomRule(2, 4);
                case Difficulty.Hard: return new FourCharIdiomRule(4, 8);
                default: return new FourCharIdiomRule(1, 3);
            }
        }
    }
}
