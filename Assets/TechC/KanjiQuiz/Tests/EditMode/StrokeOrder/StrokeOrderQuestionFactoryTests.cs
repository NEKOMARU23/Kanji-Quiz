using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.StrokeOrder;
using UnityEngine.TestTools;

namespace TechC.KanjiQuiz.Tests
{
    public class StrokeOrderQuestionFactoryTests
    {
        private StrokeOrderQuestionFactory factory;

        [SetUp]
        public void SetUp()
        {
            UnityEngine.Random.InitState(42);
            var source = new FakeKanjiStrokeSource(new Dictionary<string, int>
            {
                { "一", 1 },
                { "二", 2 },
                { "三", 3 },
                { "令", 5 },
                { "漢", 13 },
            });
            factory = new StrokeOrderQuestionFactory(source);
        }

        [Test]
        public void 正解の選択肢だけが正しい書き順になる()
        {
            var questions = Create(new[] { "令", "漢" }, Difficulty.Easy, 3);

            foreach (var question in questions)
            {
                var correct = WrongOrderGenerator.CreateIdentity(question.Strokes.Count);
                for (int i = 0; i < question.ChoiceCount; i++)
                {
                    bool isCorrectOrder = WrongOrderGenerator.IsSameOrder(question.GetChoiceOrder(i), correct);
                    Assert.AreEqual(i == question.CorrectIndex, isCorrectOrder, $"{question.Kanji} の選択肢 {i}");
                }
            }
        }

        [Test]
        public void 書き順データがない漢字は除外される()
        {
            LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("鬱"));

            var questions = Create(new[] { "令", "鬱", "漢" }, Difficulty.Easy, 3);

            CollectionAssert.AreEqual(new[] { "令", "漢" }, questions.Select(q => q.Kanji));
        }

        [Test]
        public void 重複しない不正解を作れない漢字は除外される()
        {
            // 1画と2画の漢字では、重複しない不正解を2つ作れない
            var questions = Create(new[] { "一", "二", "三" }, Difficulty.Easy, 3);

            CollectionAssert.AreEqual(new[] { "三" }, questions.Select(q => q.Kanji));
        }

        [Test]
        public void 難易度の最小画数に満たない漢字は除外される()
        {
            var questions = Create(new[] { "三", "令", "漢" }, Difficulty.Hard, 3);

            CollectionAssert.AreEqual(new[] { "令", "漢" }, questions.Select(q => q.Kanji));
        }

        [Test]
        public void 選択肢ごとの画のデータは書き順どおりに並ぶ()
        {
            var question = Create(new[] { "漢" }, Difficulty.Easy, 3).Single();

            for (int i = 0; i < question.ChoiceCount; i++)
            {
                var order = question.GetChoiceOrder(i);
                var strokes = question.GetChoiceStrokes(i);
                for (int k = 0; k < order.Count; k++)
                {
                    Assert.AreSame(question.Strokes[order[k]].Points, strokes[k].Points);
                }
            }
        }

        [Test]
        public void ランダムに選んで指定した数の問題を作る()
        {
            var candidates = new[] { "三", "令", "漢" };

            var questions = factory.CreateRandomAsync(candidates, Difficulty.Easy, 3, 2, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(2, questions.Count);
            CollectionAssert.AllItemsAreUnique(questions.Select(q => q.Kanji));
            CollectionAssert.IsSubsetOf(questions.Select(q => q.Kanji), candidates);
        }

        [Test]
        public void ランダムに選んだ漢字で作れないときは別の漢字を選び直す()
        {
            // 「一」「二」は作れない。作れるのは「令」だけ
            var questions = factory.CreateRandomAsync(new[] { "一", "二", "令" }, Difficulty.Easy, 3, 1, CancellationToken.None).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "令" }, questions.Select(q => q.Kanji));
        }

        [Test]
        public void 出題範囲が足りないときは作れた分だけ返す()
        {
            LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("作れませんでした"));

            var questions = factory.CreateRandomAsync(new[] { "一", "令" }, Difficulty.Easy, 3, 5, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, questions.Count);
        }

        [Test]
        public void 並べた順に漢字を選んで問題を作る()
        {
            // 先頭の「一」は作れないので飛ばし、「漢」「令」を選ぶ。「三」は選ばない
            var questions = factory.CreateFromOrderedAsync(new[] { "一", "漢", "令", "三" }, Difficulty.Easy, 3, 2, CancellationToken.None).GetAwaiter().GetResult();

            CollectionAssert.AreEquivalent(new[] { "漢", "令" }, questions.Select(q => q.Kanji));
        }

        private List<StrokeOrderQuestion> Create(IEnumerable<string> kanjiList, Difficulty difficulty, int choiceCount)
        {
            // テスト用のデータ源はすぐに完了するので、同期的に結果を取り出せる
            return factory.CreateAsync(kanjiList, difficulty, choiceCount, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
