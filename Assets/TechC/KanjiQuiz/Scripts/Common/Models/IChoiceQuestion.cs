namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 「N 個の選択肢から1つ選ぶ」形式の問題
    /// 選択肢の並び順は問題を作った時点で確定させ、表示のたびに変えない
    /// </summary>
    public interface IChoiceQuestion
    {
        /// <summary>選択肢の数</summary>
        int ChoiceCount { get; }

        /// <summary>正解の選択肢の番号</summary>
        int CorrectIndex { get; }
    }
}
