using System.Collections.Generic;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の右のページに出す、1字の内容。ない情報は空文字（または空のリスト）
    /// </summary>
    public sealed class KanjiBookDetail
    {
        /// <summary>漢字</summary>
        public string Kanji { get; }

        /// <summary>読み</summary>
        public string Reading { get; }

        /// <summary>意味（表示の設定に合わせて、ひらがなにしたもの）</summary>
        public string Meaning { get; }

        /// <summary>例文（表示の設定に合わせて、ひらがなにしたもの）</summary>
        public string Example { get; }

        /// <summary>使用例の熟語（例：海水（かいすい））</summary>
        public IReadOnlyList<string> Words { get; }

        /// <summary>この字を正解したモードの名前</summary>
        public IReadOnlyList<string> ModeNames { get; }

        public KanjiBookDetail(
            string kanji, string reading, string meaning, string example,
            IReadOnlyList<string> words, IReadOnlyList<string> modeNames)
        {
            Kanji = kanji ?? string.Empty;
            Reading = reading ?? string.Empty;
            Meaning = meaning ?? string.Empty;
            Example = example ?? string.Empty;
            Words = words ?? new string[0];
            ModeNames = modeNames ?? new string[0];
        }
    }
}
