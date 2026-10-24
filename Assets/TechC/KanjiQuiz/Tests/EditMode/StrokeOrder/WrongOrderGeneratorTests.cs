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
        public void Hardでは隣り合う2画だけが入れ替わる()
        {
            Assert.IsTrue(WrongOrderGenerator.TryGenerate(10, Difficulty.Hard, 3, out var orders));

            foreach (var order in orders)
            {
                var moved = Enumerable.Range(0, order.Length).Where(i => order[i] != i).ToList();
                Assert.AreEqual(2, moved.Count, "入れ替わった画の数");
                Assert.AreEqual(1, moved[1] - moved[0], "隣り合っていない");
            }
        }

        [TestCase(3, 2)]
        [TestCase(5, 2)]
        [TestCase(10, 3)]
        [TestCase(13, 4)]
        [TestCase(20, 6)]
        public void Normalで位置を変える画の数は画数の30パーセント最低2画(int strokeCount, int expected)
        {
            Assert.AreEqual(expected, WrongOrderGenerator.NormalChangeCount(strokeCount));
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void Normalでは連続した30パーセントの画がすべて動く(int strokeCount)
        {
            int expected = WrongOrderGenerator.NormalChangeCount(strokeCount);

            for (int round = 0; round < 20; round++)
            {
                Assert.IsTrue(WrongOrderGenerator.TryGenerate(strokeCount, Difficulty.Normal, 2, out var orders));

                foreach (var order in orders)
                {
                    var moved = Enumerable.Range(0, order.Length).Where(i => order[i] != i).ToList();
                    Assert.AreEqual(expected, moved.Count, "位置が変わった画の数");
                    Assert.AreEqual(expected - 1, moved[moved.Count - 1] - moved[0], "動いた画が連続していない");
                }
            }
        }
    }
}
