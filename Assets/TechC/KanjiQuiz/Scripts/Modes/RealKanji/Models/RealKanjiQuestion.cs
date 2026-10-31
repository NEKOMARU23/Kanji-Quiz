using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの1問
    /// 本物1つと偽物を並べる。選択肢の並びは問題を作った時点で確定していて、表示のたびに変わらない
    /// </summary>
    public sealed class RealKanjiQuestion : IChoiceQuestion
    {
        private readonly List<RealKanjiChoice> choices;

        /// <summary>本物の漢字</summary>
        public string Kanji { get; }

        /// <summary>本物の読み</summary>
        public string Reading { get; }

        /// <summary>本物の使用例（振り返りで出す）。なければ空</summary>
        public IReadOnlyList<KanjiWord> Words { get; }

        /// <summary>本物の意味と例文（振り返りで出す）。なければ空</summary>
        public KanjiMeaning Meaning { get; }

        /// <summary>選択肢</summary>
        public IReadOnlyList<RealKanjiChoice> Choices => choices;

        public int ChoiceCount => choices.Count;

        public int CorrectIndex { get; }

        public RealKanjiQuestion(
            string kanji, string reading, IReadOnlyList<KanjiWord> words, IEnumerable<RealKanjiChoice> choices, int correctIndex,
            KanjiMeaning meaning = default)
        {
            Kanji = kanji ?? throw new ArgumentNullException(nameof(kanji));
            Reading = reading ?? string.Empty;
            Words = words ?? Array.Empty<KanjiWord>();
            Meaning = meaning;
            this.choices = new List<RealKanjiChoice>(choices ?? throw new ArgumentNullException(nameof(choices)));

            if (correctIndex < 0 || correctIndex >= this.choices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(correctIndex));
            }
            CorrectIndex = correctIndex;
        }
    }
}
