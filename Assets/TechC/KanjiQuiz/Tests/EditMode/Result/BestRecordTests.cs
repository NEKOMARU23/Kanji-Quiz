using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class BestRecordTests
    {
        private const string Key = "StrokeOrder_Easy";

        private static QuizResult CreateResult(int correct, int total)
        {
            var items = new QuizResultItem[total];
            for (int i = 0; i < total; i++)
            {
                items[i] = new QuizResultItem("一", i < correct ? AnswerState.Correct : AnswerState.Incorrect);
            }
            return new QuizResult("テスト", "Scene", items, null, Difficulty.Easy, Key);
        }

        [Test]
        public void 初めての記録は1問以上正解なら更新になる()
        {
            var repository = new FakeBestRecordRepository();

            var update = CreateResult(3, 5).UpdateBestRecord(repository);

            Assert.IsTrue(update.IsNewBest);
            Assert.AreEqual(3, update.BestCorrectCount);
            Assert.IsTrue(repository.TryLoad(Key, out int saved));
            Assert.AreEqual(3, saved);
        }

        [Test]
        public void 初めての記録でも0問正解なら更新にはしない()
        {
            var update = CreateResult(0, 5).UpdateBestRecord(new FakeBestRecordRepository());

            Assert.IsFalse(update.IsNewBest);
            Assert.AreEqual(0, update.BestCorrectCount);
        }

        [Test]
        public void 前の記録を超えたときだけ更新して保存する()
        {
            var repository = new FakeBestRecordRepository();
            repository.Save(Key, 3);

            var same = CreateResult(3, 5).UpdateBestRecord(repository);
            Assert.IsFalse(same.IsNewBest, "同じ正解数は更新ではない");
            Assert.AreEqual(3, same.BestCorrectCount);

            var better = CreateResult(4, 5).UpdateBestRecord(repository);
            Assert.IsTrue(better.IsNewBest);
            Assert.AreEqual(4, better.BestCorrectCount);
            Assert.IsTrue(repository.TryLoad(Key, out int saved));
            Assert.AreEqual(4, saved);
        }

        [Test]
        public void 同じ結果で2回調べても保存し直さない()
        {
            var repository = new FakeBestRecordRepository();
            var result = CreateResult(5, 5);

            var first = result.UpdateBestRecord(repository);
            var second = result.UpdateBestRecord(repository);

            Assert.AreEqual(1, repository.SaveCount);
            Assert.IsTrue(second.IsNewBest, "1回目の内容を返す");
            Assert.AreEqual(first.BestCorrectCount, second.BestCorrectCount);
        }

        [Test]
        public void 全問正解かどうかと級を結果から求める()
        {
            Assert.IsTrue(CreateResult(5, 5).IsPerfect);
            Assert.IsFalse(CreateResult(4, 5).IsPerfect);
            Assert.AreEqual("6級", CreateResult(4, 5).Rank);
        }
    }
}
