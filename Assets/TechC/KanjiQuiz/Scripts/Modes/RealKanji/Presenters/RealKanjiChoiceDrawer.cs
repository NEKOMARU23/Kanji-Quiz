namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの問題を描く処理。出題と振り返りの両方の Presenter で使う
    /// </summary>
    public static class RealKanjiChoiceDrawer
    {
        /// <summary>
        /// すべての選択肢に字と読みを描く
        /// </summary>
        /// <param name="showDescription">振り返りの説明（偽物の部品の組み合わせなど）も出すかどうか</param>
        public static void Draw(IRealKanjiView view, RealKanjiQuestion question, bool showDescription)
        {
            for (int i = 0; i < question.ChoiceCount; i++)
            {
                view.ShowChoice(i, question.Choices[i], showDescription);
            }
        }
    }
}
