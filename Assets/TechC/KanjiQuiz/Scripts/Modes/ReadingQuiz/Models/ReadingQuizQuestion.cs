using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての1問
    /// 熟語を見せ、正しい読み（1つ、または複数）を選択肢から選ぶ
    /// 選択肢の並びは問題を作った時点で確定していて、表示のたびに変わらない
    /// </summary>
    public sealed class ReadingQuizQuestion
    {
        private readonly List<string> choices;

        /// <summary>熟語（漢字）</summary>
        public string Word { get; }

        /// <summary>選択肢の読み（並びは確定済み）</summary>
        public IReadOnlyList<string> Choices => choices;

        /// <summary>正しい選択肢を立てたビットマスク（ReadingQuizAnswer と同じ形）</summary>
        public int CorrectMask { get; }

        /// <summary>正しい読みの数（1つ、または複数）</summary>
        public int CorrectCount { get; }

        /// <summary>選択肢の数</summary>
        public int ChoiceCount => choices.Count;

        /// <summary>複数回答かどうか</summary>
        public bool IsMultiple => CorrectCount > 1;

        /// <param name="word">熟語</param>
        /// <param name="choices">選択肢の読み（並びは確定済み）</param>
        /// <param name="correctMask">正しい選択肢を立てたビットマスク</param>
        public ReadingQuizQuestion(string word, IEnumerable<string> choices, int correctMask)
        {
            Word = word ?? throw new ArgumentNullException(nameof(word));
            this.choices = new List<string>(choices ?? throw new ArgumentNullException(nameof(choices)));
            if (this.choices.Count < 2 || this.choices.Count > ReadingQuizAnswer.MaxChoiceCount)
            {
                throw new ArgumentException("選択肢の数が正しくありません。", nameof(choices));
            }

            int fullMask = (1 << this.choices.Count) - 1;
            if (correctMask <= 0 || (correctMask & ~fullMask) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(correctMask));
            }
            CorrectMask = correctMask;
            CorrectCount = ReadingQuizAnswer.Decode(correctMask).Count;
        }

        /// <summary>選んだ組み合わせが正解かどうか（選んだ数と正しい数が同じで、すべて一致するとき）</summary>
        public bool IsCorrect(int selectedMask)
        {
            return selectedMask == CorrectMask;
        }
    }
}
