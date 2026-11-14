using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.KanjiUsage;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiUsageReviewPresenterTests
    {
        private static KanjiUsageQuestion Question() => new KanjiUsageQuestion(
            "かいとう", "この{問題|もんだい}の＿を書く。",
            new[]
            {
                new KanjiUsageChoice("回答", "{質問|しつもん}に{答|こた}えること。"),
                new KanjiUsageChoice("解答", "{問題|もんだい}をといて{答|こた}えること。"),
                new KanjiUsageChoice("解凍", "こおったものをとかすこと。"),
            }, 1);

        [Test]
        public void 振り返りでは正解を入れた文と候補それぞれの意味を出す()
        {
            var view = new FakeKanjiUsageView();
            view.SetWeakMarkVisible(true);
            var choiceListView = new FakeChoiceListView();
            var questions = new[] { Question() };
            var answers = new[] { AnswerRecord.Create(0, false) };

            using (var presenter = new KanjiUsageReviewPresenter(
                questions, answers, 0, new FakeQuizFrameView(), choiceListView, new FakeQuizReviewView(), view, new FakeSceneLoader()))
            {
                presenter.Initialize();
            }

            Assert.IsFalse(view.IsWeakMarkVisible, "振り返りでは苦手の印を出さない");
            Assert.AreEqual("この問題の＿を書く。", view.SentenceText);
            Assert.AreEqual("解答", view.BlankText, "空いたところには正解の言葉を入れる");
            Assert.IsTrue(view.IsAnswer);
            Assert.AreEqual("正解：解答", view.Hint);
            Assert.AreEqual("質問に答えること。", view.Meanings[0]);
            Assert.AreEqual("問題をといて答えること。", view.Meanings[1]);
            Assert.AreEqual("こおったものをとかすこと。", view.Meanings[2]);
            Assert.AreEqual(1, choiceListView.MarkedCorrectIndex, "正解の選択肢に印がつく");
            Assert.AreEqual(0, choiceListView.MarkedSelectedIndex, "選んだ選択肢に印がつく");
            Assert.IsFalse(choiceListView.IsInteractable, "振り返りでは選択肢を押せない");
        }

        [Test]
        public void ひらがなの設定に切り替えると文と意味をすぐ出し直し正解の言葉は漢字のまま()
        {
            var view = new FakeKanjiUsageView();
            var settings = new DisplaySettings(new FakeDisplaySettingRepository());
            var questions = new[] { Question() };
            var answers = new[] { AnswerRecord.Create(1, true) };

            using (var presenter = new KanjiUsageReviewPresenter(
                questions, answers, 0, new FakeQuizFrameView(), new FakeChoiceListView(), new FakeQuizReviewView(), view,
                new FakeSceneLoader(), null, settings))
            {
                presenter.Initialize();
                Assert.AreEqual("この問題の＿を書く。", view.SentenceText);
                Assert.AreEqual("質問に答えること。", view.Meanings[0]);

                settings.SetUseHiragana(true);
                Assert.AreEqual("このもんだいの＿を書く。", view.SentenceText);
                Assert.AreEqual("しつもんにこたえること。", view.Meanings[0]);
                Assert.AreEqual("解答", view.BlankText, "正解の言葉は漢字のまま");
                Assert.AreEqual("回答", view.Words[0], "選択肢の言葉は漢字のまま");
            }

            // 終わったあとに設定を変えても、出し直さない
            int count = view.SentenceCount;
            settings.SetUseHiragana(false);
            Assert.AreEqual(count, view.SentenceCount);
        }

        [Test]
        public void 問題を移ると次の問題の文と意味を出す()
        {
            var view = new FakeKanjiUsageView();
            var frameView = new FakeQuizFrameView();
            var second = new KanjiUsageQuestion(
                "あつい", "{夏|なつ}は＿。",
                new[] { new KanjiUsageChoice("暑い", "気温が高い"), new KanjiUsageChoice("熱い", "温度が高い"), new KanjiUsageChoice("厚い", "はばが大きい") }, 0);
            var questions = new[] { Question(), second };
            var answers = new[] { AnswerRecord.Create(1, true), AnswerRecord.Create(2, false) };

            using (var presenter = new KanjiUsageReviewPresenter(
                questions, answers, 0, frameView, new FakeChoiceListView(), new FakeQuizReviewView(), view, new FakeSceneLoader()))
            {
                presenter.Initialize();
                Assert.AreEqual("解答", view.BlankText);

                frameView.ClickNext();
                Assert.AreEqual(1, presenter.CurrentIndex);
                Assert.AreEqual("暑い", view.BlankText);
                Assert.AreEqual("夏は＿。", view.SentenceText);
                Assert.AreEqual("気温が高い", view.Meanings[0]);
            }
        }

        [Test]
        public void 結果に戻るボタンで結果の画面を開く()
        {
            var sceneLoader = new FakeSceneLoader();
            var reviewView = new FakeQuizReviewView();

            using (var presenter = new KanjiUsageReviewPresenter(
                new[] { Question() }, new[] { AnswerRecord.Create(1, true) }, 0, new FakeQuizFrameView(), new FakeChoiceListView(),
                reviewView, new FakeKanjiUsageView(), sceneLoader))
            {
                presenter.Initialize();
                Assert.IsTrue(reviewView.IsVisible);

                reviewView.ClickBack();
                CollectionAssert.AreEqual(new[] { SceneNames.Result }, sceneLoader.LoadedScenes);
            }
        }
    }
}
