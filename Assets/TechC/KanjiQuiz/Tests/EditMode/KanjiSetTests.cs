using System;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiSetTests
    {
        [Test]
        public void JSONから漢字を1文字ずつ読み込む()
        {
            var set = KanjiSet.FromJson(@"{ ""id"": ""Elementary"", ""title"": ""小学漢字"", ""source"": ""test"", ""kanji"": ""一右雨"" }");

            Assert.AreEqual("Elementary", set.Id);
            Assert.AreEqual("小学漢字", set.Title);
            CollectionAssert.AreEqual(new[] { "一", "右", "雨" }, set.Kanji);
        }

        [Test]
        public void サロゲートペアの漢字も1文字として扱う()
        {
            // 「𠮟」（U+20B9F）は C# の string では2つの char になる
            CollectionAssert.AreEqual(new[] { "叱", "𠮟", "責" }, KanjiSet.SplitCharacters("叱𠮟責"));
        }

        [Test]
        public void 空白と改行は無視する()
        {
            CollectionAssert.AreEqual(new[] { "一", "二" }, KanjiSet.SplitCharacters("一 \n二"));
        }

        [Test]
        public void 形式が正しくないJSONは例外になる()
        {
            Assert.Throws<FormatException>(() => KanjiSet.FromJson(@"{ ""title"": ""idがない"" }"));
        }

        [TestCase(Difficulty.Easy, "簡単")]
        [TestCase(Difficulty.Normal, "常識")]
        [TestCase(Difficulty.Hard, "困難")]
        public void 難易度の表示名(Difficulty difficulty, string expected)
        {
            Assert.AreEqual(expected, difficulty.ToDisplayName());
        }
    }
}
