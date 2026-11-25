using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class RubyLabelFitTests
    {
        [TestCase(60f, 21f)]
        [TestCase(38f, 13.3f)]
        [TestCase(26f, 10f)]
        public void 下限は元の大きさの35パーセント_ただし10未満にはしない(float original, float expected)
        {
            Assert.AreEqual(expected, RubyLabelFit.MinSize(original), 0.001f);
        }

        [Test]
        public void 小さい文字は下限が10になる_ただし元より大きくはならない()
        {
            Assert.AreEqual(10f, RubyLabelFit.MinSize(18f), 0.001f);
            Assert.AreEqual(10f, RubyLabelFit.MinSize(23f), 0.001f);
            Assert.AreEqual(8f, RubyLabelFit.MinSize(8f), 0.001f);
        }

        [TestCase(20f, 8f)]
        [TestCase(38f, 15.2f)]
        [TestCase(0f, 0f)]
        public void 左右の余白は元の大きさの4割(float original, float expected)
        {
            Assert.AreEqual(expected, RubyLabelFit.SidePadding(original), 0.001f);
        }

        [Test]
        public void 大きさが0以下なら0()
        {
            Assert.AreEqual(0f, RubyLabelFit.MinSize(0f));
            Assert.AreEqual(0f, RubyLabelFit.MinSize(-3f));
        }
    }
}
