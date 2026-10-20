using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の選択式の問題
    /// </summary>
    public class FakeChoiceQuestion : IChoiceQuestion
    {
        public int ChoiceCount { get; }
        public int CorrectIndex { get; }

        public FakeChoiceQuestion(int choiceCount, int correctIndex)
        {
            ChoiceCount = choiceCount;
            CorrectIndex = correctIndex;
        }
    }
}
