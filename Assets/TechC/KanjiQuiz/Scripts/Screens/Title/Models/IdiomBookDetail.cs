namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の四字熟語のページで、右のページに出す熟語の内容
    /// </summary>
    public sealed class IdiomBookDetail
    {
        /// <summary>四字熟語</summary>
        public string Word { get; }

        /// <summary>読み</summary>
        public string Reading { get; }

        /// <summary>意味（表示の設定に合わせて、ひらがなにしたもの）</summary>
        public string Meaning { get; }

        /// <summary>例文（表示の設定に合わせて、ひらがなにしたもの）</summary>
        public string Example { get; }

        public IdiomBookDetail(string word, string reading, string meaning, string example)
        {
            Word = word ?? string.Empty;
            Reading = reading ?? string.Empty;
            Meaning = meaning ?? string.Empty;
            Example = example ?? string.Empty;
        }
    }
}
