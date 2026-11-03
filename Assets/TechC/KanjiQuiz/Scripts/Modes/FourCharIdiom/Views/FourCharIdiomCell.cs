namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>熟語のマス1つの見せ方</summary>
    public enum FourCharIdiomCellKind
    {
        /// <summary>はじめから字が入っているマス</summary>
        Fixed,

        /// <summary>空いているマス</summary>
        Blank,

        /// <summary>プレイヤーが字を入れたマス</summary>
        Filled,

        /// <summary>振り返りで、正しい字が入っていたマス</summary>
        Correct,

        /// <summary>振り返りで、まちがった字が入っていた（または空いたままの）マス</summary>
        Incorrect,
    }

    /// <summary>
    /// 熟語のマス1つの表示内容
    /// </summary>
    public readonly struct FourCharIdiomCell
    {
        /// <summary>マスに出す字。空いているマスは null</summary>
        public string Character { get; }

        /// <summary>マスの見せ方</summary>
        public FourCharIdiomCellKind Kind { get; }

        public FourCharIdiomCell(string character, FourCharIdiomCellKind kind)
        {
            Character = character;
            Kind = kind;
        }
    }
}
