using NUnit.Framework;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiBookRateTests
    {
        [TestCase(0, 100, "0%")]
        [TestCase(50, 100, "50%")]
        [TestCase(100, 100, "100%")]
        [TestCase(1, 3, "33.3%")]
        [TestCase(999, 1000, "99.9%")]
        [TestCase(22, 14000, "0.1%")]
        [TestCase(0, 0, "0%")]
        public void コンプ率は小数第1位で切り捨てて出す(int collected, int total, string expected)
        {
            Assert.AreEqual(expected, KanjiBookView.FormatRate(collected, total));
        }
    }
}
