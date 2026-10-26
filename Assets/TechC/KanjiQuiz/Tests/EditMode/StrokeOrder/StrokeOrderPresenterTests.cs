using System.Collections.Generic;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.StrokeOrder;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class StrokeOrderPresenterTests
    {
        private static StrokeOrderQuestion CreateQuestion(string kanji)
        {
            var strokes = new[]
            {
                new StrokeData(new List<Vector3> { Vector3.zero, Vector3.right }),
                new StrokeData(new List<Vector3> { Vector3.zero, Vector3.up }),
            };
            return new StrokeOrderQuestion(kanji, strokes, new[] { new[] { 0, 1 }, new[] { 1, 0 } }, 0);
        }

        [Test]
        public void 苦手な漢字のときだけ苦手の印を出す()
        {
            var view = new FakeStrokeOrderView();
            var frameView = new FakeQuizFrameView();
            var choiceListView = new FakeChoiceListView();
            var weakList = new WeakQuestionList(new[] { "令" });

            using (var session = new QuizSession<StrokeOrderQuestion>(new[] { CreateQuestion("令"), CreateQuestion("漢") }))
            using (var presenter = new StrokeOrderPresenter(session, frameView, choiceListView, view, null, weakList))
            {
                presenter.Initialize();
                Assert.AreEqual("令", view.Kanji);
                Assert.IsTrue(view.IsWeakMarkVisible);

                choiceListView.Select(0);
                frameView.ClickNext();
                Assert.AreEqual("漢", view.Kanji);
                Assert.IsFalse(view.IsWeakMarkVisible);
            }
        }

        [Test]
        public void 振り返りでは苦手の印を出さずにお手本を書く()
        {
            var view = new FakeStrokeOrderView();
            view.SetWeakMarkVisible(true);
            var questions = new[] { CreateQuestion("令") };
            var answers = new[] { AnswerRecord.Create(1, false) };

            using (var presenter = new StrokeOrderReviewPresenter(
                questions, answers, 0, new FakeQuizFrameView(), new FakeChoiceListView(), new FakeQuizReviewView(), view, new FakeSceneLoader()))
            {
                presenter.Initialize();
            }

            Assert.IsFalse(view.IsWeakMarkVisible);
            Assert.AreEqual(1, view.ModelAnimationCount);
        }
    }
}
