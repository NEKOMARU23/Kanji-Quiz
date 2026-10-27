using System.Collections.Generic;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.RealKanji;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の本物の漢字さがしの表示。表示した内容を記録する
    /// </summary>
    public class FakeRealKanjiView : IRealKanjiView
    {
        public bool IsWeakMarkVisible { get; private set; }

        /// <summary>選択肢ごとに表示した読み</summary>
        public Dictionary<int, string> Readings { get; } = new Dictionary<int, string>();

        /// <summary>選択肢ごとに表示した説明。説明を出さなかったときは null</summary>
        public Dictionary<int, string> Descriptions { get; } = new Dictionary<int, string>();

        /// <summary>振り返りで表示した本物の漢字。表示していなければ null</summary>
        public string AnswerKanji { get; private set; }

        public IReadOnlyList<KanjiWord> AnswerWords { get; private set; }

        public void SetWeakMarkVisible(bool visible) => IsWeakMarkVisible = visible;

        public void ShowChoice(int choiceIndex, RealKanjiChoice choice, bool showDescription)
        {
            Readings[choiceIndex] = choice.Reading;
            Descriptions[choiceIndex] = showDescription ? choice.Description : null;
        }

        public void ShowAnswerInfo(string kanji, string reading, IReadOnlyList<KanjiWord> words)
        {
            AnswerKanji = kanji;
            AnswerWords = words;
        }
    }
}
