using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがし固有の表示（選択肢の字と読み、苦手の印、振り返りの説明）
    /// </summary>
    public interface IRealKanjiView
    {
        /// <summary>出題している漢字が苦手な漢字（前に間違えた漢字）であることを示す印を出すかどうか</summary>
        void SetWeakMarkVisible(bool visible);

        /// <summary>
        /// 選択肢に字と読みを表示する
        /// </summary>
        /// <param name="choiceIndex">選択肢の番号</param>
        /// <param name="showDescription">振り返りの説明（偽物の部品の組み合わせなど）も出すかどうか</param>
        void ShowChoice(int choiceIndex, RealKanjiChoice choice, bool showDescription);

        /// <summary>
        /// 振り返りで、本物の漢字の読み・意味・例文・使用例を表示する
        /// </summary>
        /// <param name="meaning">意味（表示する文。なければ空）</param>
        /// <param name="example">例文（表示する文。なければ空）。本物の漢字を強調して出す</param>
        void ShowAnswerInfo(string kanji, string reading, string meaning, string example, IReadOnlyList<KanjiWord> words);
    }
}
