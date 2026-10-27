namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字の使用例（熟語）1つ
    /// </summary>
    public readonly struct KanjiWord
    {
        /// <summary>言葉（例：海外）</summary>
        public string Word { get; }

        /// <summary>読み（例：かいがい）</summary>
        public string Reading { get; }

        public KanjiWord(string word, string reading)
        {
            Word = word;
            Reading = reading;
        }

        /// <summary>表示用の文字列（例：海外（かいがい））</summary>
        public override string ToString()
        {
            return $"{Word}（{Reading}）";
        }
    }
}
