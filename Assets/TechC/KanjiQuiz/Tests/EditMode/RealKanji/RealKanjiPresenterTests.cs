using System.Collections.Generic;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.RealKanji;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class RealKanjiPresenterTests
    {
        private static RealKanjiQuestion CreateQuestion(string kanji)
        {
            var strokes = new[] { new StrokeData(new List<Vector3> { Vector3.zero, Vector3.right }) };
            var choices = new[]
            {
                new RealKanjiChoice(strokes, "カイ", RealKanjiQuestionFactory.RealDescription),
                new RealKanjiChoice(strokes, "ジ", "氵＋寺"),
            };
            var words = new[] { new KanjiWord("海外", "かいがい") };
            return new RealKanjiQuestion(kanji, "カイ", words, choices, 0);
        }

        [Test]
        public void 出題では読みだけを出し苦手な漢字のときだけ苦手の印を出す()
        {
            var view = new FakeRealKanjiView();
            var frameView = new FakeQuizFrameView();
            var choiceListView = new FakeChoiceListView();
            var weakList = new WeakQuestionList(new[] { "海" });

            using (var session = new QuizSession<RealKanjiQuestion>(new[] { CreateQuestion("海"), CreateQuestion("池") }))
            using (var presenter = new RealKanjiPresenter(session, frameView, choiceListView, view, null, weakList))
            {
                presenter.Initialize();
                Assert.IsTrue(view.IsWeakMarkVisible);
                Assert.AreEqual("カイ", view.Readings[0]);
                Assert.AreEqual("ジ", view.Readings[1]);
                Assert.IsNull(view.Descriptions[0]);
                Assert.IsNull(view.Descriptions[1]);
                Assert.IsNull(view.AnswerKanji);

                choiceListView.Select(0);
                frameView.ClickNext();
                Assert.IsFalse(view.IsWeakMarkVisible);
            }
        }

        [Test]
        public void 振り返りでは説明と本物の読みと使用例を出し苦手の印は出さない()
        {
            var view = new FakeRealKanjiView();
            view.SetWeakMarkVisible(true);
            var questions = new[] { CreateQuestion("海") };
            var answers = new[] { AnswerRecord.Create(1, false) };

            using (var presenter = new RealKanjiReviewPresenter(
                questions, answers, 0, new FakeQuizFrameView(), new FakeChoiceListView(), new FakeQuizReviewView(), view, new FakeSceneLoader()))
            {
                presenter.Initialize();
            }

            Assert.IsFalse(view.IsWeakMarkVisible);
            Assert.AreEqual(RealKanjiQuestionFactory.RealDescription, view.Descriptions[0]);
            Assert.AreEqual("氵＋寺", view.Descriptions[1]);
            Assert.AreEqual("海", view.AnswerKanji);
            Assert.AreEqual("海外", view.AnswerWords[0].Word);
        }
    }
}
