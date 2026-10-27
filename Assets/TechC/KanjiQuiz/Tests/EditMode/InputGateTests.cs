using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class InputGateTests
    {
        [SetUp]
        public void SetUp() => InputGate.Clear();

        [TearDown]
        public void TearDown() => InputGate.Clear();

        [Test]
        public void 止めた全員が解除するまで止まったままになる()
        {
            var menu = new object();
            var transition = new object();

            InputGate.Block(menu);
            InputGate.Block(transition);
            InputGate.Unblock(menu);
            Assert.IsTrue(InputGate.IsBlocked);

            InputGate.Unblock(transition);
            Assert.IsFalse(InputGate.IsBlocked);
        }

        [Test]
        public void 同じ相手が何度止めても1回の解除で戻る()
        {
            var menu = new object();

            InputGate.Block(menu);
            InputGate.Block(menu);
            InputGate.Unblock(menu);

            Assert.IsFalse(InputGate.IsBlocked);
        }
    }
}
