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

        /// <summary>選択肢を選んだ</summary>
        Select,

        /// <summary>問題を切り替えた（ページをめくる）</summary>
        PageTurn,

        /// <summary>級のハンコを押す</summary>
        Stamp,

        /// <summary>全問正解の花丸を書く</summary>
        Hanamaru,

        /// <summary>自己ベストを更新した</summary>
        NewRecord,
    }
}
