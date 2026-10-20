namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 1問分の回答の記録
    /// </summary>
    public readonly struct AnswerRecord
    {
        /// <summary>未回答を表す記録</summary>
        public static readonly AnswerRecord Unanswered = new AnswerRecord(AnswerState.Unanswered, -1);

        /// <summary>回答の状態</summary>
        public AnswerState State { get; }

        /// <summary>選んだ選択肢の番号。未回答のときは -1</summary>
        public int SelectedIndex { get; }

        /// <summary>回答済みかどうか</summary>
        public bool IsAnswered => State != AnswerState.Unanswered;

        public AnswerRecord(AnswerState state, int selectedIndex)
        {
            State = state;
            SelectedIndex = selectedIndex;
        }

        /// <summary>
        /// 選んだ番号と正誤から回答の記録を作る
        /// </summary>
        public static AnswerRecord Create(int selectedIndex, bool isCorrect)
        {
            return new AnswerRecord(isCorrect ? AnswerState.Correct : AnswerState.Incorrect, selectedIndex);
        }
    }
}
