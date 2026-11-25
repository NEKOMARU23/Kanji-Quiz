using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.RealKanji;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechC.KanjiQuiz.Tests
{
    public class RealKanjiQuestionFactoryTests
    {
        private FakeKanjiSvgSource source;
        private KanjiWordDictionary words;

        [SetUp]
        public void SetUp()
        {
            Random.InitState(42);
            source = new FakeKanjiSvgSource()
                .Add("海", "海-g1", 10, 10, 10, 90).Add("海", "海-g2", 50, 20, 90, 20).Add("海", "海-g2", 50, 80, 90, 80)
                .Add("池", "池-g1", 10, 10, 10, 90).Add("池", "池-g2", 50, 50, 90, 50)
                .Add("寺", "寺-g1", 20, 30, 80, 30).Add("寺", "寺-g2", 50, 10, 50, 90)
                .Add("清", "清-g1", 10, 10, 10, 90).Add("清", "清-g2", 50, 20, 90, 80)
                .Add("古", "古-g1", 20, 20, 80, 20).Add("古", "古-g2", 30, 50, 70, 90);
            words = KanjiWordDictionary.FromJson(
                @"{ ""id"": ""test"", ""entries"": [ {""kanji"":""海"",""words"":[{""word"":""海外"",""reading"":""かいがい""}]} ] }");
        }

        private static RealKanjiFakeEntry Fake(string part, string donor, string reading, params string[] groups)
        {
            return new RealKanjiFakeEntry { part = part, donor = donor, groups = groups, reading = reading };
        }

        private static RealKanjiEntry Entry(string kanji, string reading, params RealKanjiFakeEntry[] fakes)
        {
            return new RealKanjiEntry
            {
                kanji = kanji,
                reading = reading,
                radical = "氵",
                position = "left",
                radicalGroups = new[] { $"{kanji}-g1" },
                fakes = fakes,
            };
        }

        private static RealKanjiEntry Sea()
        {
            return Entry("海", "カイ",
                Fake("寺", "寺", "ジ"),
                Fake("青", "清", "セイ", "清-g2"),
                Fake("古", "古", "コ"));
        }

        [Test]
        public void 本物1つと偽物3つの4択を作る()
        {
            var factory = new RealKanjiQuestionFactory(source, new[] { Sea() }, words);
            var question = factory.CreateFromOrderedAsync(new[] { "海" }, 4, 1, CancellationToken.None).GetAwaiter().GetResult().Single();

            Assert.AreEqual("海", question.Kanji);
            Assert.AreEqual(4, question.ChoiceCount);
            var correct = question.Choices[question.CorrectIndex];
            Assert.AreEqual("カイ", correct.Reading);
            Assert.AreEqual(RealKanjiQuestionFactory.RealDescription, correct.Description);
            Assert.AreEqual(3, correct.Strokes.Count);

            var fakes = question.Choices.Where((_, i) => i != question.CorrectIndex).ToList();
            CollectionAssert.AreEquivalent(new[] { "ジ", "セイ", "コ" }, fakes.Select(c => c.Reading));
            CollectionAssert.AreEquivalent(new[] { "氵＋寺", "氵＋青（清の{右側|みぎがわ}）", "氵＋古" }, fakes.Select(c => c.Description));
            Assert.AreEqual("海外", question.Words.Single().Word);
        }

        [Test]
        public void 偽物の候補が多いときは毎回その中から選ぶ()
        {
            var entry = Entry("海", "カイ",
                Fake("寺", "寺", "ジ"), Fake("青", "清", "セイ", "清-g2"), Fake("古", "古", "コ"), Fake("也", "池", "チ", "池-g2"));
            var factory = new RealKanjiQuestionFactory(source, new[] { entry });

            var question = factory.CreateFromOrderedAsync(new[] { "海" }, 4, 1, CancellationToken.None).GetAwaiter().GetResult().Single();

            Assert.AreEqual(4, question.ChoiceCount);
            CollectionAssert.AllItemsAreUnique(question.Choices.Select(c => c.Description));
        }

        [Test]
        public void 書き順データがない漢字は除外される()
        {
            LogAssert.Expect(LogType.Warning, new Regex("湖"));
            LogAssert.Expect(LogType.Warning, new Regex("1 問を作れませんでした|2 問を作れませんでした"));
            var factory = new RealKanjiQuestionFactory(source, new[] { Entry("湖", "コ", Fake("寺", "寺", "ジ")), Sea() });

            var questions = factory.CreateFromOrderedAsync(new[] { "湖", "海" }, 4, 2, CancellationToken.None).GetAwaiter().GetResult();

            CollectionAssert.AreEqual(new[] { "海" }, questions.Select(q => q.Kanji));
        }

        [Test]
        public void 偽物を作れる数が足りない漢字は除外される()
        {
            // 「鬱」は書き順データがないので、作れる偽物は2つだけ
            LogAssert.Expect(LogType.Warning, new Regex("偽物を 3 個作れない"));
            LogAssert.Expect(LogType.Warning, new Regex("作れませんでした"));
            var entry = Entry("海", "カイ", Fake("寺", "寺", "ジ"), Fake("古", "古", "コ"), Fake("鬱", "鬱", "ウツ"));
            var factory = new RealKanjiQuestionFactory(source, new[] { entry });

            var questions = factory.CreateFromOrderedAsync(new[] { "海" }, 4, 1, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsEmpty(questions);
        }

        [Test]
        public void 指定した数の問題を作ったらそれ以上は読み込まない()
        {
            var factory = new RealKanjiQuestionFactory(source, new[] { Sea(), Entry("池", "チ", Fake("寺", "寺", "ジ"), Fake("古", "古", "コ"), Fake("青", "清", "セイ", "清-g2")) });

            var questions = factory.CreateFromOrderedAsync(new[] { "海", "池" }, 4, 1, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, questions.Count);
            Assert.IsFalse(source.LoadCounts.ContainsKey("池"));
        }

        [Test]
        public void 出題できる漢字は問題データの順で重複しない()
        {
            var factory = new RealKanjiQuestionFactory(source, new[] { Sea(), Entry("池", "チ"), Sea() });

            CollectionAssert.AreEqual(new[] { "海", "池" }, factory.Candidates);
        }

        [TestCase("left", "{右側|みぎがわ}")]
        [TestCase("top", "{下|した}")]
        [TestCase("kamae", "{内側|うちがわ}")]
        public void 偽物の説明に借りた部品の場所を書く(string position, string expected)
        {
            var entry = Entry("海", "カイ");
            entry.position = position;

            Assert.AreEqual($"氵＋青（清の{expected}）", RealKanjiQuestionFactory.DescribeFake(entry, Fake("青", "清", "セイ", "清-g2")));
            Assert.AreEqual("氵＋寺", RealKanjiQuestionFactory.DescribeFake(entry, Fake("寺", "寺", "ジ")));
        }
    }
}
