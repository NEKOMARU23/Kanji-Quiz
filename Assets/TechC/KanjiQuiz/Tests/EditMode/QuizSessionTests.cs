using System;
using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class QuizSessionTests
    {
        private QuizSession<string> session;

        [SetUp]
        public void SetUp()
        {
            session = new QuizSession<string>(new[] { "一", "二", "三" });
        }

        [TearDown]
        public void TearDown()
        {
            session.Dispose();
        }

        [Test]
        public void 問題が空のときは例外になる()
        {
            Assert.Throws<ArgumentException>(() => new QuizSession<string>(Array.Empty<string>()));
        }

        [Test]
        public void 開始時は最初の問題で全問未回答()
        {
            Assert.AreEqual(0, session.CurrentIndex.CurrentValue);
            Assert.AreEqual("一", session.Current);
            Assert.IsTrue(session.IsFirst);
            Assert.IsFalse(session.IsLast);
            Assert.AreEqual(0, session.Score.CurrentValue);
            foreach (var answer in session.Answers)
            {
                Assert.AreEqual(AnswerState.Unanswered, answer.State);
            }
        }

        [Test]
        public void 正解を記録するとスコアが増える()
        {
            session.RecordAnswer(2, true);

            Assert.AreEqual(1, session.Score.CurrentValue);
            Assert.AreEqual(AnswerState.Correct, session.CurrentAnswer.State);
            Assert.AreEqual(2, session.CurrentAnswer.SelectedIndex);
        }

        [Test]
        public void 不正解を記録してもスコアは増えない()
        {
            session.RecordAnswer(1, false);

            Assert.AreEqual(0, session.Score.CurrentValue);
            Assert.AreEqual(AnswerState.Incorrect, session.CurrentAnswer.State);
        }

        [Test]
        public void 回答し直すと記録とスコアが上書きされる()
        {
            session.RecordAnswer(0, true);
            session.RecordAnswer(1, false);

            Assert.AreEqual(0, session.Score.CurrentValue);
            Assert.AreEqual(1, session.CurrentAnswer.SelectedIndex);
        }

        [Test]
        public void 最後の問題ではMoveNextできない()
        {
            Assert.IsTrue(session.MoveNext());
            Assert.IsTrue(session.MoveNext());
            Assert.IsTrue(session.IsLast);
            Assert.IsFalse(session.MoveNext());
            Assert.AreEqual(2, session.CurrentIndex.CurrentValue);
        }

        [Test]
        public void 最初の問題ではMovePrevできない()
        {
            Assert.IsFalse(session.MovePrev());
            Assert.AreEqual(0, session.CurrentIndex.CurrentValue);
        }

        [Test]
        public void 現在の問題番号の変化が通知される()
        {
            int notified = -1;
            using (session.CurrentIndex.Subscribe(x => notified = x))
            {
                session.MoveNext();
            }

            Assert.AreEqual(1, notified);
        }
    }
}
