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

        private List<StrokeOrderQuestion> Create(IEnumerable<string> kanjiList, Difficulty difficulty, int choiceCount)
        {
            // テスト用のデータ源はすぐに完了するので、同期的に結果を取り出せる
            return factory.CreateAsync(kanjiList, difficulty, choiceCount, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
