using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.FourCharIdiom;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechC.KanjiQuiz.Tests
{
    public class FourCharIdiomModelTests
    {
        private static FourCharIdiomEntry Entry(string word, params string[] confusers)
        {
            return new FourCharIdiomEntry
            {
                word = word,
                reading = "よみ",
                meaning = "{意味|いみ}",
                example = word + "だ。",
                confusers = confusers,
            };
        }

        [Test]
        public void 入れた候補の番号をまとめて元に戻せる()
        {
            var filled = new[] { 11, -1, 0, 5 };

            int value = FourCharIdiomAnswer.Encode(filled);

            CollectionAssert.AreEqual(filled, FourCharIdiomAnswer.Decode(value, 4));
        }

        [Test]
        public void 未回答の記録からはどのマスも空になる()
        {
            CollectionAssert.AreEqual(new[] { -1, -1 }, FourCharIdiomAnswer.Decode(-1, 2));
            CollectionAssert.AreEqual(new[] { -1 }, FourCharIdiomAnswer.Decode(0, 1));
        }

        [Test]
        public void 難易度ごとに空けるマスとまぎらわしい字の数が決まる()
        {
            Assert.AreEqual((1, 4), (FourCharIdiomRule.For(Difficulty.Easy).BlankCount, FourCharIdiomRule.For(Difficulty.Easy).TileCount));
            Assert.AreEqual((2, 6), (FourCharIdiomRule.For(Difficulty.Normal).BlankCount, FourCharIdiomRule.For(Difficulty.Normal).TileCount));
            Assert.AreEqual((4, 12), (FourCharIdiomRule.For(Difficulty.Hard).BlankCount, FourCharIdiomRule.For(Difficulty.Hard).TileCount));
        }

        [Test]
        public void 空いたマスを全部埋めて熟語と同じなら正解()
        {
            var question = new FourCharIdiomQuestion("一石二鳥", "", "", "", new[] { 1, 3 }, new[] { "鳥", "岩", "石", "羽" });

            Assert.IsFalse(question.IsComplete(new[] { 2, -1 }));
            Assert.IsTrue(question.IsCorrect(new[] { 2, 0 }));
            Assert.IsFalse(question.IsCorrect(new[] { 1, 0 }));
            CollectionAssert.AreEqual(new[] { "一", "石", "二", null }, question.Compose(new[] { 2, -1 }));
        }

        [Test]
        public void 同じ字の候補が2つあればどちらを使っても正解()
        {
            var question = new FourCharIdiomQuestion("自問自答", "", "", "", new[] { 0, 1, 2, 3 }, new[] { "自", "答", "自", "問" });

            Assert.IsTrue(question.IsCorrect(new[] { 0, 3, 2, 1 }));
            Assert.IsTrue(question.IsCorrect(new[] { 2, 3, 0, 1 }));
        }

        [TestCase(Difficulty.Easy)]
        [TestCase(Difficulty.Normal)]
        [TestCase(Difficulty.Hard)]
        public void 空けた位置の正解の字とまぎらわしい字を候補にする(Difficulty difficulty)
        {
            var rule = FourCharIdiomRule.For(difficulty);
            var factory = new FourCharIdiomQuestionFactory(new[] { Entry("一石二鳥", "十七百", "岩右砂", "三五上", "島羽馬") });

            var question = factory.CreateFromOrdered(factory.Candidates, rule, 1)[0];

            Assert.AreEqual(rule.BlankCount, question.BlankPositions.Count);
            Assert.AreEqual(rule.TileCount, question.Tiles.Count);
            var confusers = new List<string>();
            foreach (int position in question.BlankPositions)
            {
                CollectionAssert.Contains(question.Tiles, question.Characters[position]);
                foreach (var ch in new[] { "十七百", "岩右砂", "三五上", "島羽馬" }[position]) confusers.Add(ch.ToString());
            }
            foreach (var tile in question.Tiles)
            {
                Assert.IsTrue(question.Characters.Contains(tile) || confusers.Contains(tile), $"候補「{tile}」が、空けた位置のまぎらわしい字ではない");
            }
            CollectionAssert.AllItemsAreUnique(question.Tiles);
        }

        [Test]
        public void まぎらわしい字が足りない熟語は出題しない()
        {
            var factory = new FourCharIdiomQuestionFactory(new[]
            {
                Entry("一石二鳥", "十", "岩", "三", "島"),
                Entry("十人十色", "千百万", "入大八", "二五九", "角形式"),
            });

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("一石二鳥"));
            var questions = factory.CreateFromOrdered(factory.Candidates, FourCharIdiomRule.For(Difficulty.Easy), 1);

            Assert.AreEqual(1, questions.Count);
            Assert.AreEqual("十人十色", questions[0].Word);
        }

        [TestCase("Easy")]
        [TestCase("Normal")]
        [TestCase("Hard")]
        public void 同梱の問題データからどの難易度の決まりでも問題を作れる(string level)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/TechC/KanjiQuiz/Data/FourCharIdiom/FourCharIdiom_{level}.json");
            Assert.IsNotNull(asset, level);
            var entries = new LocalQuestionRepository<FourCharIdiomEntry>(asset.text).LoadAll();
            Assert.GreaterOrEqual(entries.Count, 50);

            var factory = new FourCharIdiomQuestionFactory(entries);
            foreach (var difficulty in new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard })
            {
                var questions = factory.CreateFromOrdered(factory.Candidates, FourCharIdiomRule.For(difficulty), entries.Count);
                Assert.AreEqual(entries.Count, questions.Count, difficulty.ToString());
            }
            foreach (var entry in entries)
            {
                Assert.IsNotEmpty(entry.reading, entry.word);
                Assert.IsNotEmpty(entry.meaning, entry.word);
                StringAssert.Contains(entry.word, entry.example);
            }
        }
    }
}
