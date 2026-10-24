namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 効果音の種類
    /// </summary>
    public enum SeType
    {
        /// <summary>正解（○）</summary>
        Correct,

        /// <summary>不正解（×）</summary>
        Incorrect,

        /// <summary>点数が決まるまでのルーレット（ループ再生）</summary>
        Roulette,

        /// <summary>点数の発表（ジャン）</summary>
        ScoreReveal,
    }
}
