using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class ShuffleUtilityTests
    {
        [Test]
        public void シャッフルしても要素の種類と数は変わらない()
        {
            var list = Enumerable.Range(0, 20).ToList();

            ShuffleUtility.Shuffle(list);

            CollectionAssert.AreEquivalent(Enumerable.Range(0, 20), list);
        }

        [Test]
        public void ShuffledCopyは元のリストを変更しない()
        {
            var source = new List<int> { 1, 2, 3, 4, 5 };

            var copy = ShuffleUtility.ShuffledCopy(source);

            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, source);
            CollectionAssert.AreEquivalent(source, copy);
        }

        [Test]
        public void 何度もシャッフルすると並びが変わることがある()
        {
            UnityEngine.Random.InitState(12345);
            var original = Enumerable.Range(0, 10).ToList();

            bool changed = false;
            for (int i = 0; i < 10 && !changed; i++)
            {
                changed = !ShuffleUtility.ShuffledCopy(original).SequenceEqual(original);
            }

            Assert.IsTrue(changed);
        }
    }
}
