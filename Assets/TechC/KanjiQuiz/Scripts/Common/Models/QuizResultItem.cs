namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 結果画面に表示する1問分の結果
    /// </summary>
    public readonly struct QuizResultItem
    {
        /// <summary>問題を表す短い文字列（書き順クイズなら漢字）</summary>
        public string Label { get; }

        /// <summary>回答の状態</summary>
        public AnswerState State { get; }

        public QuizResultItem(string label, AnswerState state)
        {
            Label = label;
            State = state;
        }
    }
}
