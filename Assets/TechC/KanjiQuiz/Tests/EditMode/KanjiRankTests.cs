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
        public void 七問中四問の次は一問多く正解すれば上の級()
        {
            Assert.IsTrue(KanjiRank.TryGetNextRank(4, 7, Difficulty.Normal, out int more, out string next));
            Assert.AreEqual(1, more);
            Assert.AreEqual("5級", next);
        }

        [Test]
        public void 級が上がらない正解数は飛ばして数える()
        {
            // 7問中0問（0%）→ 1問（14%）は同じ 8級、2問（28%）で 7級
            Assert.IsTrue(KanjiRank.TryGetNextRank(0, 7, Difficulty.Normal, out int more, out string next));
            Assert.AreEqual(2, more);
            Assert.AreEqual("7級", next);
        }

        [Test]
        public void 全問正解なら次の級はない()
        {
            Assert.IsFalse(KanjiRank.TryGetNextRank(5, 5, Difficulty.Easy, out _, out string next));
            Assert.IsNull(next);
        }

        [Test]
        public void 級の高さで難易度の違う級を比べられる()
        {
            int easyPerfect = KanjiRank.EvaluateLevel(5, 5, Difficulty.Easy);   // 5級
            int normalHalf = KanjiRank.EvaluateLevel(4, 7, Difficulty.Normal);  // 6級
            Assert.Greater(easyPerfect, normalHalf);
            Assert.AreEqual("5級", KanjiRank.NameOf(easyPerfect));
        }

        [TestCase(Difficulty.Easy, "5級")]
        [TestCase(Difficulty.Hard, "初段")]
        public void 難易度ごとの最高の級(Difficulty difficulty, string expected)
        {
            Assert.AreEqual(expected, KanjiRank.TopRank(difficulty));
        }

        [Test]
        public void 正解数が範囲外なら例外を投げる()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => KanjiRank.Evaluate(6, 5, Difficulty.Easy));
            Assert.Throws<ArgumentOutOfRangeException>(() => KanjiRank.Evaluate(0, 0, Difficulty.Easy));
        }
    }
}
