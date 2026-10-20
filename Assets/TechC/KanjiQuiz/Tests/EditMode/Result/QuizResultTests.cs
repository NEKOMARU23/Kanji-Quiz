using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class QuizResultTests
    {
        [Test]
        public void 進行状況から問題ごとの結果と正解数を作る()
        {
            using (var session = new QuizSession<string>(new[] { "令", "漢", "字" }))
            {
                session.RecordAnswer(0, true);
                session.MoveNext();
                session.RecordAnswer(1, false);
                session.MoveNext();
                session.RecordAnswer(2, true);

                var result = QuizResult.FromSession("書き順クイズ", "StrokeOrder", session, q => q);

                Assert.AreEqual("書き順クイズ", result.ModeName);
                Assert.AreEqual("StrokeOrder", result.RetrySceneName);
                Assert.AreEqual(3, result.Total);
                Assert.AreEqual(2, result.CorrectCount);
                Assert.AreEqual("漢", result.Items[1].Label);
                Assert.AreEqual(AnswerState.Incorrect, result.Items[1].State);
            }
        }

        [Test]
        public void 未回答の問題は未回答のまま結果に入る()
        {
            using (var session = new QuizSession<string>(new[] { "一", "二" }))
            {
                session.RecordAnswer(0, true);

                var result = QuizResult.FromSession("テスト", "Scene", session, q => q);

                Assert.AreEqual(AnswerState.Unanswered, result.Items[1].State);
                Assert.AreEqual(1, result.CorrectCount);
            }
        }
    }
}
