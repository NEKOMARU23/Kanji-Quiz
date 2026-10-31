namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字1字の意味と例文。どちらも読みをつけた文（RubyText の書き方）で持つ
    /// </summary>
    public readonly struct KanjiMeaning
    {
        /// <summary>意味（読みをつけた文）</summary>
        public string Meaning { get; }

        /// <summary>例文（読みをつけた文。その漢字には読みをつけない）</summary>
        public string Example { get; }

        /// <summary>意味も例文もないかどうか</summary>
        public bool IsEmpty => string.IsNullOrEmpty(Meaning) && string.IsNullOrEmpty(Example);

        public KanjiMeaning(string meaning, string example)
        {
            Meaning = meaning ?? string.Empty;
            Example = example ?? string.Empty;
        }
    }
}
