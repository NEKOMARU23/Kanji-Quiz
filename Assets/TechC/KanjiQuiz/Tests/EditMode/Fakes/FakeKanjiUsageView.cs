using System.Collections.Generic;
using TechC.KanjiQuiz.Modes.KanjiUsage;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>テスト用の使い分けの画面。画面の状態を記録するだけ</summary>
    public class FakeKanjiUsageView : IKanjiUsageView
    {
        public bool IsWeakMarkVisible { get; private set; }

        /// <summary>最後に出した文（読みの印は処理済み。空いたところは Blank のまま）</summary>
        public string SentenceText { get; private set; }

        /// <summary>最後に出した、空いたところに入れる文字</summary>
        public string BlankText { get; private set; }

        public bool IsAnswer { get; private set; }

        /// <summary>文を出した回数</summary>
        public int SentenceCount { get; private set; }

        public string Hint { get; private set; }

        /// <summary>選択肢の番号ごとの言葉</summary>
        public Dictionary<int, string> Words { get; } = new Dictionary<int, string>();

        /// <summary>選択肢の番号ごとの意味（出さなかったときは null）</summary>
        public Dictionary<int, string> Meanings { get; } = new Dictionary<int, string>();

        public void SetWeakMarkVisible(bool visible) => IsWeakMarkVisible = visible;

        public void ShowSentence(string text, string blankText, bool isAnswer)
        {
            SentenceText = text;
            BlankText = blankText;
            IsAnswer = isAnswer;
            SentenceCount++;
        }

        public void SetHint(string hint) => Hint = hint;

        public void ShowChoice(int choiceIndex, string word, string meaning)
        {
            Words[choiceIndex] = word;
            Meanings[choiceIndex] = meaning;
        }
    }
}
