using System.Linq;
using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.KanjiUsage;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiUsagePresenterTests
    {
        // 1問目：正解は「回答」（番号0）、3択
        // 2問目：正解は「解答」（番号1）、4択
        private static KanjiUsageQuestion Question1() => new KanjiUsageQuestion(
            "かいとう", "この{問題|もんだい}の＿を書く。",
            new[]
            {
                new KanjiUsageChoice("回答", "{質問|しつもん}に{答|こた}えること。"),
                new KanjiUsageChoice("解答", "{問題|もんだい}をといて{答|こた}えること。"),
                new KanjiUsageChoice("解凍", "こおったものをとかすこと。"),
            }, 0);

        private static KanjiUsageQuestion Question2() => new KanjiUsageQuestion(
            "かいとう", "{算数|さんすう}の＿を{見|み}る。",
            new[]
            {
                new KanjiUsageChoice("回答", "意味A"),
                new KanjiUsageChoice("解答", "意味B"),
                new KanjiUsageChoice("解凍", "意味C"),
                new KanjiUsageChoice("会頭", "意味D"),
            }, 1);

        [Test]
        public void 出題では文の空いたところに読みを出し選択肢の言葉を出す()
        {
            var frameView = new FakeQuizFrameView();
            var choiceListView = new FakeChoiceListView();
            var view = new FakeKanjiUsageView();

            using (var session = new QuizSession<KanjiUsageQuestion>(new[] { Question1(), Question2() }))
            using (var presenter = new KanjiUsagePresenter(session, frameView, choiceListView, view))
            {
                presenter.Initialize();

                Assert.AreEqual("この問題の＿を書く。", view.SentenceText, "読みの印は処理して、空いたところの文字は残す");
                Assert.AreEqual("（かいとう）", view.BlankText);
                Assert.IsFalse(view.IsAnswer);
                Assert.AreEqual(3, choiceListView.ChoiceCount);
                CollectionAssert.AreEqual(new[] { "回答", "解答", "解凍" }, new[] { view.Words[0], view.Words[1], view.Words[2] });
                Assert.IsTrue(view.Meanings.Values.All(m => m == null), "出題では意味を出さない");
                Assert.IsNotEmpty(view.Hint);
            }
        }

        [Test]
        public void 選択肢を選ぶと次へを押せて選んだ番号で正誤が決まる()
        {
            var frameView = new FakeQuizFrameView();
            var choiceListView = new FakeChoiceListView();
            var view = new FakeKanjiUsageView();

            using (var session = new QuizSession<KanjiUsageQuestion>(new[] { Question1(), Question2() }))
            using (var presenter = new KanjiUsagePresenter(session, frameView, choiceListView, view))
            {
                bool finished = false;
                presenter.OnFinished.Subscribe(_ => finished = true);
                presenter.Initialize();
                Assert.IsFalse(frameView.IsNextInteractable, "選ぶまで次へを押せない");

                choiceListView.Select(0);
                Assert.IsTrue(frameView.IsNextInteractable, "選択肢を押すと次へが押せる");
                frameView.ClickNext();
                Assert.AreEqual(4, choiceListView.ChoiceCount, "2問目の選択肢の数");
                Assert.AreEqual("算数の＿を見る。", view.SentenceText);

                choiceListView.Select(3);
                frameView.ClickNext();

                Assert.IsTrue(finished);
                Assert.AreEqual(AnswerState.Correct, session.Answers[0].State);
                Assert.AreEqual(AnswerState.Incorrect, session.Answers[1].State);
            }
        }

        [Test]
        public void 苦手な言葉のときだけ苦手の印を出す()
        {
            var frameView = new FakeQuizFrameView();
            var choiceListView = new FakeChoiceListView();
            var view = new FakeKanjiUsageView();
            var weakList = new WeakQuestionList(new[] { "回答" });

            using (var session = new QuizSession<KanjiUsageQuestion>(new[] { Question1(), Question2() }))
            using (var presenter = new KanjiUsagePresenter(session, frameView, choiceListView, view, null, weakList))
            {
                presenter.Initialize();
                Assert.IsTrue(view.IsWeakMarkVisible, "1問目の正解は苦手な言葉");

                choiceListView.Select(0);
                frameView.ClickNext();
                Assert.IsFalse(view.IsWeakMarkVisible, "2問目の正解（解答）は苦手ではない");
            }
        }

        [Test]
        public void ひらがなの設定に切り替えると今の問題の文をすぐ出し直す()
        {
            var view = new FakeKanjiUsageView();
            var settings = new DisplaySettings(new FakeDisplaySettingRepository());

            using (var session = new QuizSession<KanjiUsageQuestion>(new[] { Question1(), Question2() }))
            using (var presenter = new KanjiUsagePresenter(
                session, new FakeQuizFrameView(), new FakeChoiceListView(), view, null, null, settings))
            {
                presenter.Initialize();
                Assert.AreEqual("この問題の＿を書く。", view.SentenceText);
                int count = view.SentenceCount;

                settings.SetUseHiragana(true);
                Assert.AreEqual("このもんだいの＿を書く。", view.SentenceText);
                Assert.AreEqual(count + 1, view.SentenceCount);
                Assert.AreEqual("回答", view.Words[0], "選択肢の言葉は漢字のまま");
            }
        }

        [Test]
        public void 終わったあとに設定を変えても文を出し直さない()
        {
            var view = new FakeKanjiUsageView();
            var settings = new DisplaySettings(new FakeDisplaySettingRepository());

            using (var session = new QuizSession<KanjiUsageQuestion>(new[] { Question1() }))
            {
                using (var presenter = new KanjiUsagePresenter(
                    session, new FakeQuizFrameView(), new FakeChoiceListView(), view, null, null, settings))
                {
                    presenter.Initialize();
                }

                int count = view.SentenceCount;
                settings.SetUseHiragana(true);
                Assert.AreEqual(count, view.SentenceCount);
            }
        }
    }
}
