using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.FourCharIdiom;

namespace TechC.KanjiQuiz.Tests
{
    public class FourCharIdiomPresenterTests
    {
        private FakeQuizFrameView frameView;
        private FakeFourCharIdiomView view;
        private FakeAudioPlayer audio;
        private QuizSession<FourCharIdiomQuestion> session;
        private FourCharIdiomPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            frameView = new FakeQuizFrameView();
            view = new FakeFourCharIdiomView();
            audio = new FakeAudioPlayer();
        }

        [TearDown]
        public void TearDown()
        {
            presenter?.Dispose();
            session?.Dispose();
        }

        /// <summary>1問目：一石二鳥（2字目と4字目が空き）、2問目：自問自答（全部空き）</summary>
        private void CreatePresenter(WeakQuestionList weakList = null, DisplaySettings displaySettings = null)
        {
            session = new QuizSession<FourCharIdiomQuestion>(new[]
            {
                new FourCharIdiomQuestion("一石二鳥", "いっせきにちょう", "ふたつの{得|とく}", "一石二鳥だ。", new[] { 1, 3 }, new[] { "鳥", "岩", "石", "羽" }),
                new FourCharIdiomQuestion("自問自答", "じもんじとう", "{自分|じぶん}で{答|こた}える", "自問自答した。", new[] { 0, 1, 2, 3 }, new[] { "自", "答", "目", "自", "問", "間" }),
            });
            presenter = new FourCharIdiomPresenter(session, frameView, view, audio, weakList, displaySettings);
            presenter.Initialize();
        }

        [Test]
        public void 最初の問題の意味とマスと候補を出す()
        {
            CreatePresenter();

            Assert.AreEqual("ふたつの得", view.Meaning);
            Assert.AreEqual("一？二？", view.Shown);
            Assert.AreEqual(FourCharIdiomCellKind.Fixed, view.Cells[0].Kind);
            Assert.AreEqual(FourCharIdiomCellKind.Blank, view.Cells[1].Kind);
            CollectionAssert.AreEqual(new[] { "鳥", "岩", "石", "羽" }, view.Tiles);
            Assert.IsFalse(frameView.IsNextInteractable);
        }

        [Test]
        public void 候補を押すと前から空いたマスに入り全部埋まると次へを押せる()
        {
            CreatePresenter();

            view.ClickTile("石");
            Assert.AreEqual("一石二？", view.Shown);
            Assert.AreEqual(FourCharIdiomCellKind.Filled, view.Cells[1].Kind);
            Assert.IsTrue(view.UsedTiles[2]);
            Assert.IsFalse(frameView.IsNextInteractable);

            view.ClickTile("鳥");
            Assert.AreEqual("一石二鳥", view.Shown);
            Assert.IsTrue(frameView.IsNextInteractable);
            CollectionAssert.AreEqual(new[] { SeType.Select, SeType.Select }, audio.PlayedSe);
        }

        [Test]
        public void 使った候補はもう一度押しても入らない()
        {
            CreatePresenter();

            view.TileClicked.OnNext(2);
            view.TileClicked.OnNext(2);

            Assert.AreEqual("一石二？", view.Shown);
        }

        [Test]
        public void 埋めたマスを押すと候補に戻る()
        {
            CreatePresenter();
            view.ClickTile("岩");
            view.ClickTile("鳥");

            view.CellClicked.OnNext(1);

            Assert.AreEqual("一？二鳥", view.Shown);
            Assert.IsFalse(view.UsedTiles[1]);
            Assert.IsFalse(frameView.IsNextInteractable);

            // 次に押した候補は、空いた2字目に入る
            view.ClickTile("石");
            Assert.AreEqual("一石二鳥", view.Shown);
        }

        [Test]
        public void 後ろから戻すといちばん後ろの埋めたマスが空く()
        {
            CreatePresenter();
            view.ClickTile("石");
            view.ClickTile("鳥");

            view.CellClicked.OnNext(-1);

            Assert.AreEqual("一石二？", view.Shown);
        }

        [Test]
        public void 次へで正誤を記録し前の問題に戻ると入れた字が戻る()
        {
            CreatePresenter();
            view.ClickTile("岩");
            view.ClickTile("鳥");
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Incorrect, session.Answers[0].State);
            Assert.AreEqual("？？？？", view.Shown);

            frameView.ClickPrev();

            Assert.AreEqual("一岩二鳥", view.Shown);
            Assert.IsTrue(frameView.IsNextInteractable);
        }

        [Test]
        public void 同じ字が2回出る熟語はどちらの候補を使っても正解()
        {
            CreatePresenter();
            view.ClickTile("石");
            view.ClickTile("鳥");
            frameView.ClickNext();

            view.TileClicked.OnNext(3);
            view.ClickTile("問");
            view.TileClicked.OnNext(0);
            view.ClickTile("答");
            frameView.ClickNext();

            Assert.AreEqual(AnswerState.Correct, session.Answers[0].State);
            Assert.AreEqual(AnswerState.Correct, session.Answers[1].State);
        }

        [Test]
        public void 意味の中の空いたマスの字を含む言葉は読みで出し振り返りでは漢字で出す()
        {
            var question = new FourCharIdiomQuestion(
                "老若男女", "ろうにゃくなんにょ", "お{年寄|としよ}りも{若|わか}い{人|ひと}も、{男|おとこ}の{人|ひと}も。", "老若男女が{集|あつ}まる。",
                new[] { 1 }, new[] { "若", "苦", "英", "右" });

            Assert.AreEqual("お年寄りもわかい人も、男の人も。", FourCharIdiomHint.Render(question, useHiragana: false));
            Assert.AreEqual("おとしよりもわかいひとも、おとこのひとも。", FourCharIdiomHint.Render(question, useHiragana: true));

            var reviewView = new FakeQuizReviewView();
            var answers = new[] { AnswerRecord.Create(FourCharIdiomAnswer.Encode(new[] { 0 }), true) };
            using (var review = new FourCharIdiomReviewPresenter(new[] { question }, answers, 0, frameView, reviewView, view, new FakeSceneLoader()))
            {
                review.Initialize();
                Assert.AreEqual("お年寄りも若い人も、男の人も。", view.Meaning);
            }
        }

        [Test]
        public void 苦手な熟語なら苦手の印を出す()
        {
            CreatePresenter(new WeakQuestionList(new[] { "一石二鳥" }));

            Assert.IsTrue(view.IsWeakMarkVisible);
        }

        [Test]
        public void ひらがなで表示の設定なら意味をひらがなで出し切り替えるとすぐ出し直す()
        {
            var settings = new DisplaySettings(new FakeDisplaySettingRepository());
            CreatePresenter(displaySettings: settings);
            Assert.AreEqual("ふたつの得", view.Meaning);

            settings.SetUseHiragana(true);

            Assert.AreEqual("ふたつのとく", view.Meaning);
        }

        [Test]
        public void 振り返りでは入れた字の正誤をマスごとに見せ正解の読みと例文を出す()
        {
            var questions = new[]
            {
                new FourCharIdiomQuestion("一石二鳥", "いっせきにちょう", "ふたつの{得|とく}", "{今日|きょう}は一石二鳥だ。", new[] { 1, 3 }, new[] { "鳥", "岩", "石", "羽" }),
            };
            var answers = new[] { AnswerRecord.Create(FourCharIdiomAnswer.Encode(new[] { 1, 0 }), false) };
            var reviewView = new FakeQuizReviewView();
            using (var review = new FourCharIdiomReviewPresenter(questions, answers, 0, frameView, reviewView, view, new FakeSceneLoader()))
            {
                review.Initialize();

                Assert.IsTrue(reviewView.IsVisible);
                Assert.IsFalse(view.IsInteractable);
                Assert.AreEqual("一岩二鳥", view.Shown);
                Assert.AreEqual(FourCharIdiomCellKind.Incorrect, view.Cells[1].Kind);
                Assert.AreEqual(FourCharIdiomCellKind.Correct, view.Cells[3].Kind);
                Assert.AreEqual("一石二鳥", view.AnswerWord);
                Assert.AreEqual("いっせきにちょう", view.AnswerReading);
                Assert.AreEqual("今日は一石二鳥だ。", view.AnswerExample);
            }
        }
    }
}
