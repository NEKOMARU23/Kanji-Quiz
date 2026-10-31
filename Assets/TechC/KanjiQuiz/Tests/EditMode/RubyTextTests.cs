using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class RubyTextTests
    {
        private const string Example = "{夏休|なつやす}みに{家族|かぞく}で海へ{泳|およ}ぎに{行|い}った。";

        [Test]
        public void 漢字のままなら読みを外して漢字を出す()
        {
            Assert.AreEqual("夏休みに家族で海へ泳ぎに行った。", RubyText.Render(Example, useHiragana: false));
        }

        [Test]
        public void ひらがなにすると読みをつけた漢字だけひらがなになる()
        {
            Assert.AreEqual("なつやすみにかぞくで海へおよぎにいった。", RubyText.Render(Example, useHiragana: true));
        }

        [Test]
        public void 閉じていない括弧はそのまま出す()
        {
            Assert.AreEqual("{海|うみ", RubyText.Render("{海|うみ", useHiragana: true));
            Assert.AreEqual(string.Empty, RubyText.Render(null, useHiragana: false));
        }

        [Test]
        public void 読みのない漢字を見つける()
        {
            var found = RubyText.FindUnmarkedKanji("{夏休|なつやす}みに家族で海へ行った。", "海");

            CollectionAssert.AreEqual(new[] { "家", "族", "行" }, found);
        }

        [Test]
        public void 読みのない漢字がなければ空()
        {
            Assert.IsEmpty(RubyText.FindUnmarkedKanji(Example, "海"));
        }

        [Test]
        public void 々も漢字として扱う()
        {
            Assert.IsTrue(RubyText.IsKanji("々"));
            Assert.IsFalse(RubyText.IsKanji("あ"));
        }
    }
}
