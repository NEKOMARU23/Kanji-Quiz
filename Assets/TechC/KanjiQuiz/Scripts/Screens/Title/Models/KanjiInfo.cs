using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳に出す、漢字ごとの情報（読み・意味と例文・使用例の熟語）をまとめて引く
    /// </summary>
    public sealed class KanjiInfo
    {
        private readonly KanjiReadingDictionary readings;
        private readonly KanjiMeaningDictionary meanings;
        private readonly KanjiWordDictionary words;

        public KanjiInfo(KanjiReadingDictionary readings, KanjiMeaningDictionary meanings, KanjiWordDictionary words)
        {
            this.readings = readings ?? throw new ArgumentNullException(nameof(readings));
            this.meanings = meanings ?? throw new ArgumentNullException(nameof(meanings));
            this.words = words ?? throw new ArgumentNullException(nameof(words));
        }

        /// <summary>読み。なければ空文字</summary>
        public string Reading(string kanji) => readings.Get(kanji);

        /// <summary>意味と例文（読みの印 {漢字|よみ} がついたまま）。なければ空</summary>
        public KanjiMeaning Meaning(string kanji) => meanings.Get(kanji);

        /// <summary>使用例の熟語</summary>
        public IReadOnlyList<KanjiWord> Words(string kanji) => words.GetWords(kanji);
    }
}
