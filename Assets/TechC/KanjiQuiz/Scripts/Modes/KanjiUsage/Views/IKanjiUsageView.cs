namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分け固有の表示（文、案内、選択肢の言葉と意味、苦手の印）
    /// 選択肢を押す操作は、共通の IChoiceListView が受け取る
    /// </summary>
    public interface IKanjiUsageView
    {
        /// <summary>出題している言葉が苦手な言葉（前に間違えた言葉）であることを示す印を出すかどうか</summary>
        void SetWeakMarkVisible(bool visible);

        /// <summary>
        /// 文を表示する。空いたところ（KanjiUsageQuestion.Blank）は blankText に置き換えて出す
        /// </summary>
        /// <param name="text">表示する文（読みの印は処理済み。空いたところは Blank のまま）</param>
        /// <param name="blankText">空いたところに出す文字（出題では（読み）、振り返りでは正解の言葉）</param>
        /// <param name="isAnswer">blankText が正解の言葉かどうか（色を変える）</param>
        void ShowSentence(string text, string blankText, bool isAnswer);

        /// <summary>文の下に出す案内を設定する</summary>
        void SetHint(string hint);

        /// <summary>
        /// 選択肢に言葉を表示する
        /// </summary>
        /// <param name="choiceIndex">選択肢の番号</param>
        /// <param name="word">言葉</param>
        /// <param name="meaning">意味（表示する文）。null のときは出さない</param>
        void ShowChoice(int choiceIndex, string word, string meaning);
    }
}
