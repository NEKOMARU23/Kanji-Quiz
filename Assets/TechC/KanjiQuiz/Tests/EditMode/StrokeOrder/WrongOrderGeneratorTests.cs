using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.StrokeOrder;

namespace TechC.KanjiQuiz.Tests
{
    public class WrongOrderGeneratorTests
    {
        [SetUp]
        public void SetUp()
        {
            UnityEngine.Random.InitState(1234);
        }

        [TestCase(Difficulty.Easy, 3)]
        [TestCase(Difficulty.Easy, 13)]
        [TestCase(Difficulty.Normal, 3)]
        [TestCase(Difficulty.Normal, 8)]
        [TestCase(Difficulty.Hard, 5)]
        [TestCase(Difficulty.Hard, 13)]
        public void 不正解は正解とも互いとも重複しない(Difficulty difficulty, int strokeCount)
        {
            var correct = WrongOrderGenerator.CreateIdentity(strokeCount);

            // 乱数の偏りに左右されないよう、何度も作って確かめる
            for (int round = 0; round < 50; round++)
            {
                Assert.IsTrue(WrongOrderGenerator.TryGenerate(strokeCount, difficulty, 2, out var orders));
                Assert.AreEqual(2, orders.Count);

                foreach (var order in orders)
                {
                    Assert.IsFalse(WrongOrderGenerator.IsSameOrder(order, correct), "正解と同じ書き順が含まれています");
                    CollectionAssert.AreEquivalent(correct, order, "画が欠けたり重複したりしています");
                }
                Assert.IsFalse(WrongOrderGenerator.IsSameOrder(orders[0], orders[1]), "不正解同士が重複しています");
            }
        }

        [Test]
        public void 一画の漢字では不正解を作れない()
        {
            Assert.IsFalse(WrongOrderGenerator.TryGenerate(1, Difficulty.Easy, 2, out _));
        }

        [Test]
        public void 二画の漢字では重複しない不正解を2つ作れない()
        {
            // 2画の並べ方は2通りしかなく、不正解は1通りしかない
            Assert.IsTrue(WrongOrderGenerator.TryGenerate(2, Difficulty.Easy, 1, out _));
            Assert.IsFalse(WrongOrderGenerator.TryGenerate(2, Difficulty.Easy, 2, out _));
        }

        [TestCase(Difficulty.Normal, 2)]
        [TestCase(Difficulty.Hard, 4)]
        public void 難易度の最小画数より少ない漢字では作れない(Difficulty difficulty, int strokeCount)
        {
            Assert.IsFalse(WrongOrderGenerator.TryGenerate(strokeCount, difficulty, 1, out _));
        }

        [Test]
        public void Hardでは2画だけが入れ替わる()
        {
            Assert.IsTrue(WrongOrderGenerator.TryGenerate(10, Difficulty.Hard, 3, out var orders));

            foreach (var order in orders)
            {
                int moved = order.Where((value, index) => value != index).Count();
                Assert.AreEqual(2, moved);
            }
        }

        [Test]
        public void Normalでは3画以下だけが動く()
        {
            Assert.IsTrue(WrongOrderGenerator.TryGenerate(10, Difficulty.Normal, 3, out var orders));

            foreach (var order in orders)
            {
                int moved = order.Where((value, index) => value != index).Count();
                Assert.That(moved, Is.InRange(2, 3));
            }
        }
    }
}
