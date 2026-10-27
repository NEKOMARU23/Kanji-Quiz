using System;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiRankTests
    {
        [TestCase(Difficulty.Easy, "5級")]
        [TestCase(Difficulty.Normal, "3級")]
        [TestCase(Difficulty.Hard, "初段")]
        public void 全問正解なら難易度ごとの最高の級になる(Difficulty difficulty, string expected)
        {
            Assert.AreEqual(expected, KanjiRank.Evaluate(5, 5, difficulty));
        }

        [TestCase(4, "6級")]
        [TestCase(3, "7級")]
        [TestCase(2, "8級")]
        [TestCase(1, "9級")]
        [TestCase(0, "10級")]
        public void 正解率が20パーセント下がるごとに級が1つ下がる(int correct, string expected)
        {
            Assert.AreEqual(expected, KanjiRank.Evaluate(correct, 5, Difficulty.Easy));
        }

        [Test]
        public void 七問中六問正解は1つ下がる()
        {
            Assert.AreEqual("1級", KanjiRank.Evaluate(6, 7, Difficulty.Hard));
        }

        [Test]
        public void 正解数が範囲外なら例外を投げる()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => KanjiRank.Evaluate(6, 5, Difficulty.Easy));
            Assert.Throws<ArgumentOutOfRangeException>(() => KanjiRank.Evaluate(0, 0, Difficulty.Easy));
        }
    }
}
