namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 振り返りのときに選択肢につける印
    /// </summary>
    public enum ChoiceMark
    {
        /// <summary>印なし</summary>
        None,

        /// <summary>正解の選択肢</summary>
        Correct,

        /// <summary>プレイヤーが選んだ不正解の選択肢</summary>
        SelectedWrong
    }
}
