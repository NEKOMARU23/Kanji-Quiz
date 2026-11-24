using System.Collections.Generic;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Title;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class ModeSummaryTests
    {
        private readonly List<QuizModeDefinition> created = new List<QuizModeDefinition>();

        [TearDown]
        public void TearDown()
        {
            foreach (var mode in created) Object.DestroyImmediate(mode);
            created.Clear();
        }

        private QuizModeDefinition CreateMode(string id, bool isAvailable = true, bool hasDifficulty = true)
        {
            var mode = QuizModeDefinition.Create(id, id + "の名前", id, hasDifficulty, isAvailable: isAvailable);
            created.Add(mode);
            return mode;
        }

        [Test]
        public void 記録がなければ級はなく全問正解の級を目標にする()
        {
            var record = new DifficultyRecord(Difficulty.Hard, "常用漢字", 7, null, null);

            Assert.IsFalse(record.HasRecord);
            Assert.IsNull(record.Rank);
            Assert.AreEqual("{全問正解|ぜんもんせいかい}で {初段|しょだん}", record.Goal);
            Assert.AreEqual(0, record.WeakQuestions.Count);
        }

        [Test]
        public void 記録があれば級と次の級までの問題数を出す()
        {
            var record = new DifficultyRecord(Difficulty.Normal, "常用漢字", 7, 4, null);

            Assert.AreEqual("6級", record.Rank);
            Assert.AreEqual("あと1{問|もん}で {5級|ごきゅう}！", record.Goal);
        }

        [Test]
        public void 全問正解なら全問正解と出す()
        {
            var record = new DifficultyRecord(Difficulty.Easy, "小学漢字", 5, 5, null);

            Assert.AreEqual("5級", record.Rank);
            Assert.AreEqual("{全問正解|ぜんもんせいかい}！", record.Goal);
        }

        [Test]
        public void 問題数を超える古い記録は問題数にそろえる()
        {
            var record = new DifficultyRecord(Difficulty.Easy, "小学漢字", 5, 7, null);

            Assert.AreEqual(5, record.BestCorrectCount);
        }

        [Test]
        public void 苦手な問題は新しい順に並べる()
        {
            var record = new DifficultyRecord(Difficulty.Easy, "小学漢字", 5, null, new[] { "古", "中", "新" });

            CollectionAssert.AreEqual(new[] { "新", "中", "古" }, record.WeakQuestions);
        }

        [Test]
        public void 保存先から難易度ごとの記録を読み一番上の級を求める()
        {
            var mode = CreateMode("StrokeOrder");
            var best = new FakeBestRecordRepository();
            best.Save(mode.RecordKey(Difficulty.Easy), 5);   // 5級
            best.Save(mode.RecordKey(Difficulty.Normal), 4); // 6級
            var weak = new FakeWeakQuestionRepository();
            weak.Save(mode.RecordKey(Difficulty.Normal), new WeakQuestionList(new[] { "援", "施" }));

            var summary = ModeSummary.Load(new[] { mode }, best, weak)[0];

            Assert.AreEqual(1, summary.Period);
            Assert.AreEqual(3, summary.Records.Count);
            Assert.AreEqual("5級", summary.BestRank);
            Assert.AreEqual("{小学漢字|しょうがくかんじ}", summary.Records[0].Range);
            Assert.AreEqual(7, summary.Records[1].QuestionCount);
            CollectionAssert.AreEqual(new[] { "施", "援" }, summary.Records[1].WeakQuestions);
            Assert.IsFalse(summary.Records[2].HasRecord);
        }

        [Test]
        public void 記録が1つもなければ一番上の級はない()
        {
            var summary = ModeSummary.Load(
                new[] { CreateMode("RealKanji") }, new FakeBestRecordRepository(), new FakeWeakQuestionRepository())[0];

            Assert.IsNull(summary.BestRank);
        }

        [Test]
        public void 時限はモードの一覧の順につける()
        {
            var summaries = ModeSummary.Load(
                new[] { CreateMode("A"), null, CreateMode("B", isAvailable: false) },
                new FakeBestRecordRepository(), new FakeWeakQuestionRepository());

            Assert.AreEqual(2, summaries.Count);
            Assert.AreEqual(2, summaries[1].Period);
            Assert.IsFalse(summaries[1].IsAvailable);
        }

        [Test]
        public void 難易度がないモードは記録が1つだけ()
        {
            var summary = ModeSummary.Load(
                new[] { CreateMode("Single", hasDifficulty: false) },
                new FakeBestRecordRepository(), new FakeWeakQuestionRepository())[0];

            Assert.AreEqual(1, summary.Records.Count);
        }
    }
}
