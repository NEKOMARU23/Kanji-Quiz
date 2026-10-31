using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class QuizRecordEraserTests
    {
        [Test]
        public void すべてのモードと難易度の自己ベストと苦手な問題を消す()
        {
            var withDifficulty = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrder");
            var withoutDifficulty = QuizModeDefinition.Create("ReplyMessage", "返信文クイズ", "ReplyMessage", hasDifficulty: false);
            var bestRecords = new FakeBestRecordRepository();
            var weakQuestions = new FakeWeakQuestionRepository();
            bestRecords.Save("StrokeOrder_Normal", 5);
            bestRecords.Save("ReplyMessage", 2);
            weakQuestions.Save("StrokeOrder_Hard", new WeakQuestionList(new[] { "鬱" }));
            bestRecords.Save("RealKanji_Easy", 4);

            new QuizRecordEraser(new[] { withDifficulty, withoutDifficulty }, bestRecords, weakQuestions).EraseAll();

            Assert.IsFalse(bestRecords.TryLoad("StrokeOrder_Normal", out _));
            Assert.IsFalse(bestRecords.TryLoad("ReplyMessage", out _));
            Assert.AreEqual(0, weakQuestions.Load("StrokeOrder_Hard").Items.Count);
            Assert.IsTrue(bestRecords.TryLoad("RealKanji_Easy", out _), "一覧にないモードの記録は消さない");

            Object.DestroyImmediate(withDifficulty);
            Object.DestroyImmediate(withoutDifficulty);
        }

        [Test]
        public void 漢字帳の保存先を渡すと漢字帳もモードごとに消す()
        {
            var mode = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrder");
            var kanjiBook = new FakeKanjiBookRepository();
            kanjiBook.Save("StrokeOrder", new[] { "海" });
            kanjiBook.Save("RealKanji", new[] { "山" });

            new QuizRecordEraser(new[] { mode }, new FakeBestRecordRepository(), new FakeWeakQuestionRepository(), kanjiBook).EraseAll();

            Assert.AreEqual(0, kanjiBook.Load("StrokeOrder").Count);
            Assert.AreEqual(1, kanjiBook.Load("RealKanji").Count, "一覧にないモードの漢字帳は消さない");

            Object.DestroyImmediate(mode);
        }
    }
}
