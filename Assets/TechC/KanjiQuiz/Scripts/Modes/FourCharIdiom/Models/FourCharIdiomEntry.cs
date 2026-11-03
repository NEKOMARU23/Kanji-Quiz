using System;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 四字熟語づくりの問題データ1件（Data/FourCharIdiom の JSON の questions の要素）
    /// Tools/generate_four_char_idioms.py が作る
    /// </summary>
    [Serializable]
    public sealed class FourCharIdiomEntry
    {
        /// <summary>四字熟語（例：一石二鳥）</summary>
        public string word;

        /// <summary>読み（ひらがな）</summary>
        public string reading;

        /// <summary>意味。漢字には {漢字|よみ} で読みがつく</summary>
        public string meaning;

        /// <summary>例文。熟語のほかの漢字には {漢字|よみ} で読みがつく</summary>
        public string example;

        /// <summary>まぎらわしい字。熟語の位置ごとに、その位置に入れるまぎらわしい字をつなげた文字列（4つ）</summary>
        public string[] confusers;
    }
}
