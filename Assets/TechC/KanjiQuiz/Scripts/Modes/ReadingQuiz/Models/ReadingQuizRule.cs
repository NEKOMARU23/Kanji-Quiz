using System;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての選択肢の数（2026-11-07 に決定）
    /// 正しい読みが1つのとき：簡単 3・常識 4・困難 4
    /// 正しい読みが複数のとき：それより 2 つ多い（簡単 5・常識 6・困難 6）
    /// </summary>
    public static class ReadingQuizRule
    {
        /// <summary>1問の選択肢の数</summary>
        /// <param name="difficulty">難易度</param>
        /// <param name="correctCount">正しい読みの数（1以上）</param>
        public static int ChoiceCount(Difficulty difficulty, int correctCount)
        {
            if (correctCount < 1) throw new ArgumentOutOfRangeException(nameof(correctCount));

            int single = difficulty == Difficulty.Easy ? 3 : 4;
            return correctCount > 1 ? single + 2 : single;
        }
    }
}
