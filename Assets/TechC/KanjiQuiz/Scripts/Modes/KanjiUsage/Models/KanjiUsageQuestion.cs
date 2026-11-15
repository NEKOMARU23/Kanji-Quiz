using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分けの1問
    /// 文の空いたところに合う言葉を、同じ読みの言葉の候補から1つ選ぶ
    /// 選択肢の並びは問題を作った時点で確定していて、表示のたびに変わらない
    /// </summary>
    public sealed class KanjiUsageQuestion : IChoiceQuestion
    {
        /// <summary>文の中の、空いたところを表す文字</summary>
        public const string Blank = "＿";

        private readonly List<KanjiUsageChoice> choices;

        /// <summary>空いたところの読み（例：かいとう）</summary>
        public string Reading { get; }

        /// <summary>文。空いたところは Blank が1つ。漢字には {漢字|よみ} で読みがついている</summary>
        public string Sentence { get; }

        /// <summary>選択肢（言葉と意味）</summary>
        public IReadOnlyList<KanjiUsageChoice> Choices => choices;

        public int ChoiceCount => choices.Count;

        public int CorrectIndex { get; }

        /// <summary>正解の言葉</summary>
        public string Answer => choices[CorrectIndex].Text;

        /// <param name="reading">空いたところの読み</param>
        /// <param name="sentence">文（空いたところは Blank が1つ）</param>
        /// <param name="choices">選択肢（並びは確定済み）</param>
        /// <param name="correctIndex">正解の選択肢の番号</param>
        public KanjiUsageQuestion(string reading, string sentence, IEnumerable<KanjiUsageChoice> choices, int correctIndex)
        {
            Reading = reading ?? throw new ArgumentNullException(nameof(reading));
            Sentence = sentence ?? throw new ArgumentNullException(nameof(sentence));
            this.choices = new List<KanjiUsageChoice>(choices ?? throw new ArgumentNullException(nameof(choices)));
            if (this.choices.Count < 2) throw new ArgumentException("選択肢は2つ以上必要です。", nameof(choices));

            if (correctIndex < 0 || correctIndex >= this.choices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(correctIndex));
            }
            CorrectIndex = correctIndex;
        }
    }
}
