using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiBookTests
    {
        private static QuizResult Result(params (string kanji, AnswerState state)[] items)
        {
            var list = new QuizResultItem[items.Length];
            for (int i = 0; i < items.Length; i++) list[i] = new QuizResultItem(items[i].kanji, items[i].state);
            return new QuizResult("テスト", "Scene", list);
        }

        [Test]
        public void 正解した字だけをそのモードの漢字帳に足して保存する()
        {
            var repository = new FakeKanjiBookRepository();

            int added = KanjiBook.Collect(repository, "StrokeOrder",
                Result(("海", AnswerState.Correct), ("空", AnswerState.Incorrect), ("山", AnswerState.Correct), ("川", AnswerState.Unanswered)));

            CollectionAssert.AreEquivalent(new[] { "海", "山" }, repository.Load("StrokeOrder"));
            CollectionAssert.IsEmpty(repository.Load("RealKanji"));
            Assert.AreEqual(2, added);
        }

        [Test]
        public void すでに集めた字は足さず新しい字がなければ保存しない()
        {
            var repository = new FakeKanjiBookRepository();
            KanjiBook.Collect(repository, "StrokeOrder", Result(("海", AnswerState.Correct)));

            int added = KanjiBook.Collect(repository, "StrokeOrder", Result(("海", AnswerState.Correct), ("空", AnswerState.Incorrect)));

            Assert.AreEqual(0, added);
            Assert.AreEqual(1, repository.SaveCount);
            Assert.AreEqual(1, repository.Load("StrokeOrder").Count);
        }

        [Test]
        public void どのモードで集めたかと集めた数を調べる()
        {
            var repository = new FakeKanjiBookRepository();
            KanjiBook.Collect(repository, "RealKanji", Result(("海", AnswerState.Correct)));
            KanjiBook.Collect(repository, "StrokeOrder", Result(("海", AnswerState.Correct), ("山", AnswerState.Correct)));

            var book = KanjiBook.Load(repository, new[] { "StrokeOrder", "RealKanji" });

            Assert.IsTrue(book.IsCollected("海"));
            Assert.IsFalse(book.IsCollected("空"));
            CollectionAssert.AreEqual(new[] { "StrokeOrder", "RealKanji" }, book.ModesOf("海"));
            CollectionAssert.AreEqual(new[] { "StrokeOrder" }, book.ModesOf("山"));
            Assert.AreEqual(2, book.CountCollected(new[] { "海", "空", "山" }));
        }

        [Test]
        public void 読みの辞書は問題データの読みを引きないときは空文字を返す()
        {
            var dictionary = KanjiReadingDictionary.FromJson(
                "{\"id\":\"A\",\"questions\":[{\"kanji\":\"悪\",\"reading\":\"アク\",\"radical\":\"心\"},{\"kanji\":\"海\",\"reading\":\"カイ\"}]}",
                "{\"questions\":[{\"kanji\":\"海\",\"reading\":\"うみ\"}]}");

            Assert.AreEqual(2, dictionary.Count);
            Assert.AreEqual("アク", dictionary.Get("悪"));
            Assert.AreEqual("カイ", dictionary.Get("海"), "同じ字は最初のものを使う");
            Assert.AreEqual(string.Empty, dictionary.Get("空"));
        }

        [Test]
        public void 漢字帳の保存は四字熟語も1つずつ区切って戻せる()
        {
            string saved = PlayerPrefsKanjiBookRepository.Serialize(new[] { "一石二鳥", "海", "十人十色" });

            CollectionAssert.AreEqual(new[] { "一石二鳥", "海", "十人十色" }, PlayerPrefsKanjiBookRepository.Deserialize(saved));
        }

        [Test]
        public void 熟語1つだけの保存も4字に分かれない()
        {
            string saved = PlayerPrefsKanjiBookRepository.Serialize(new[] { "一石二鳥" });

            CollectionAssert.AreEqual(new[] { "一石二鳥" }, PlayerPrefsKanjiBookRepository.Deserialize(saved));
        }

        [Test]
        public void 前の形式で保存した漢字帳も1字ずつ読める()
        {
            CollectionAssert.AreEqual(new[] { "海", "空", "二" }, PlayerPrefsKanjiBookRepository.Deserialize("海空二"));
            CollectionAssert.IsEmpty(PlayerPrefsKanjiBookRepository.Deserialize(string.Empty));
        }
    }
}
