using System.Collections.Generic;
using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Title;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiBookPresenterTests
    {
        private QuizModeDefinition strokeOrder;
        private QuizModeDefinition realKanji;
        private FakeKanjiBookView view;
        private FakeKanjiBookRepository repository;
        private FakeDisplaySettingRepository displayRepository;
        private DisplaySettings displaySettings;
        private int infoLoadCount;
        private KanjiBookPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            strokeOrder = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrder");
            realKanji = QuizModeDefinition.Create("RealKanji", "本物の漢字さがし", "RealKanji");
            view = new FakeKanjiBookView { CellsPerPage = 4 };
            repository = new FakeKanjiBookRepository();
            repository.Save("StrokeOrder", new[] { "二", "五" });
            repository.Save("RealKanji", new[] { "二" });
            displayRepository = new FakeDisplaySettingRepository();
            displaySettings = new DisplaySettings(displayRepository);
            infoLoadCount = 0;

            var sections = new[]
            {
                new KanjiSet("A", "小学漢字", new[] { "一", "二", "三", "四", "五", "六" }),
                new KanjiSet("B", "常用漢字", new[] { "海", "空" }),
            };
            var modes = new[] { strokeOrder, realKanji };
            presenter = new KanjiBookPresenter(
                view, new FakeAudioPlayer(), sections, modes,
                () => KanjiBook.Load(repository, new[] { "StrokeOrder", "RealKanji" }),
                LoadInfo, new FakeKanjiStrokeSource(new Dictionary<string, int> { { "二", 2 }, { "五", 4 } }), displaySettings);
            presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            presenter.Dispose();
            Object.DestroyImmediate(strokeOrder);
            Object.DestroyImmediate(realKanji);
        }

        private KanjiInfo LoadInfo()
        {
            infoLoadCount++;
            return new KanjiInfo(
                KanjiReadingDictionary.FromJson("{\"questions\":[{\"kanji\":\"二\",\"reading\":\"ニ\"}]}"),
                KanjiMeaningDictionary.FromJson(
                    "{\"entries\":[{\"kanji\":\"二\",\"meaning\":\"{数|かず}の ふたつ\",\"example\":\"二{人|り}で{遊|あそ}ぶ。\"}]}"),
                KanjiWordDictionary.FromJson(
                    "{\"entries\":[{\"kanji\":\"二\",\"words\":[{\"word\":\"二月\",\"reading\":\"にがつ\"},{\"word\":\"二人\",\"reading\":\"ふたり\"},{\"word\":\"二度\",\"reading\":\"にど\"},{\"word\":\"二重\",\"reading\":\"にじゅう\"}]}]}"));
        }

        [Test]
        public void 開くと最初のタブの最初のページに集めた字を書き空のマスを残す()
        {
            presenter.Open();

            Assert.IsTrue(view.IsVisible);
            CollectionAssert.AreEqual(new[] { "小学漢字", "常用漢字" }, view.TabTitles);
            Assert.AreEqual(0, view.SelectedTab);
            Assert.AreEqual(2, view.CollectedCount);
            Assert.AreEqual(6, view.TotalCount);
            CollectionAssert.AreEqual(new[] { "", "二", "", "" }, view.Cells);
            Assert.AreEqual(0, view.PageIndex);
            Assert.AreEqual(2, view.PageCount);
        }

        [Test]
        public void 開くとそのページで最初に集めた字を選んで内容と書き順を出す()
        {
            presenter.Open();

            Assert.AreEqual("二", presenter.SelectedKanji);
            Assert.AreEqual(1, view.SelectedCell);
            Assert.AreEqual("二", view.Detail.Kanji);
            Assert.AreEqual("ニ", view.Detail.Reading);
            Assert.AreEqual("数の ふたつ", view.Detail.Meaning);
            Assert.AreEqual("二人で遊ぶ。", view.Detail.Example);
            Assert.AreEqual(KanjiBookPresenter.MaxWords, view.Detail.Words.Count);
            Assert.AreEqual("二月（にがつ）", view.Detail.Words[0]);
            CollectionAssert.AreEqual(new[] { "書き順クイズ", "本物の漢字さがし" }, view.Detail.ModeNames);
            Assert.AreEqual(2, view.StrokePaths.Count);
        }

        [Test]
        public void ひらがなで表示なら意味と例文の字をひらがなにし切り替えるとすぐ出し直す()
        {
            presenter.Open();

            displaySettings.SetUseHiragana(true);

            Assert.AreEqual("かずの ふたつ", view.Detail.Meaning);
            Assert.AreEqual("二りであそぶ。", view.Detail.Example);
        }

        [Test]
        public void 次のページでは範囲の外のマスを出さず選んだ字の枠を外す()
        {
            presenter.Open();

            view.PageMoved.OnNext(1);

            Assert.AreEqual(1, presenter.PageIndex);
            CollectionAssert.AreEqual(new[] { "五", "", null, null }, view.Cells);
            Assert.AreEqual(-1, view.SelectedCell);
            Assert.AreEqual("二", view.Detail.Kanji, "選んだ字の内容は残す");
        }

        [Test]
        public void ページは最初より前と最後より後へはめくれない()
        {
            presenter.Open();

            view.PageMoved.OnNext(-1);
            Assert.AreEqual(0, presenter.PageIndex);

            view.PageMoved.OnNext(1);
            view.PageMoved.OnNext(1);
            Assert.AreEqual(1, presenter.PageIndex);
        }

        [Test]
        public void 集めた字のマスを押すとその字を選びまだの字のマスは選べない()
        {
            presenter.Open();
            view.PageMoved.OnNext(1);

            view.CellClicked.OnNext(1);
            Assert.AreEqual("二", presenter.SelectedKanji);

            view.CellClicked.OnNext(0);
            Assert.AreEqual("五", presenter.SelectedKanji);
            Assert.AreEqual(0, view.SelectedCell);
            CollectionAssert.AreEqual(new[] { "書き順クイズ" }, view.Detail.ModeNames);
            Assert.AreEqual(string.Empty, view.Detail.Reading, "情報のない字は空にする");
        }

        [Test]
        public void タブを切り替えると最初のページに戻り集めた字がなければ案内を出す()
        {
            presenter.Open();
            view.PageMoved.OnNext(1);

            view.TabClicked.OnNext(1);

            Assert.AreEqual(1, view.SelectedTab);
            Assert.AreEqual(0, presenter.PageIndex);
            Assert.AreEqual(0, view.CollectedCount);
            Assert.AreEqual(2, view.TotalCount);
            CollectionAssert.AreEqual(new[] { "", "", null, null }, view.Cells);
            Assert.IsNull(presenter.SelectedKanji);
            Assert.IsNull(view.Detail);
            Assert.AreEqual(0, view.StrokePaths.Count);
        }

        [Test]
        public void 開くたびに漢字帳を読み直し字の情報は1回だけ読む()
        {
            presenter.Open();
            view.BackClicked.OnNext(Unit.Default);
            repository.Save("StrokeOrder", new[] { "一", "二", "五" });

            presenter.Open();

            Assert.AreEqual(3, view.CollectedCount);
            Assert.AreEqual("一", presenter.SelectedKanji);
            Assert.AreEqual(1, infoLoadCount);
        }

        [Test]
        public void タイトルへで閉じて閉じたことを知らせる()
        {
            int closedCount = 0;
            presenter.OnClosed.Subscribe(_ => closedCount++);
            presenter.Open();

            view.BackClicked.OnNext(Unit.Default);

            Assert.IsFalse(view.IsVisible);
            Assert.AreEqual(1, closedCount);
        }

        /// <summary>四字熟語のタブつきの漢字帳（熟語は3つ、1ページ2つ。一石二鳥と温故知新を集めている）</summary>
        private KanjiBookPresenter CreateIdiomPresenter()
        {
            repository.Save("FourCharIdiom", new[] { "一石二鳥", "温故知新" });
            var idioms = IdiomBookEntry.FromJson(
                "{\"questions\":[" +
                "{\"word\":\"一石二鳥\",\"reading\":\"いっせきにちょう\",\"meaning\":\"ふたつの{得|とく}\",\"example\":\"{今日|きょう}は一石二鳥だ。\"}," +
                "{\"word\":\"十人十色\",\"reading\":\"じゅうにんといろ\",\"meaning\":\"それぞれちがう\",\"example\":\"十人十色だ。\"}," +
                "{\"word\":\"温故知新\",\"reading\":\"おんこちしん\",\"meaning\":\"{昔|むかし}に{学|まな}ぶ\",\"example\":\"温故知新の{心|こころ}。\"}]}");
            var idiomPresenter = new KanjiBookPresenter(
                view, new FakeAudioPlayer(), new[] { new KanjiSet("A", "小学漢字", new[] { "一", "二" }) }, new[] { strokeOrder },
                () => KanjiBook.Load(repository, new[] { "StrokeOrder", "FourCharIdiom" }),
                LoadInfo, new FakeKanjiStrokeSource(new Dictionary<string, int>()), displaySettings,
                new[] { new WordBookSection(KanjiBookPresenter.IdiomSectionTitle, "FourCharIdiom", idioms, true) });
            idiomPresenter.Initialize();
            return idiomPresenter;
        }

        [Test]
        public void 四字熟語のデータがあれば最後に四字熟語のタブを足す()
        {
            using (var idiomPresenter = CreateIdiomPresenter())
            {
                idiomPresenter.Open();

                CollectionAssert.AreEqual(new[] { "小学漢字", KanjiBookPresenter.IdiomSectionTitle }, view.TabTitles);
                Assert.IsFalse(view.IsIdiomMode);
            }
        }

        [Test]
        public void 四字熟語のタブでは集めた熟語を一覧に書き最初の熟語の意味を出す()
        {
            using (var idiomPresenter = CreateIdiomPresenter())
            {
                view.IdiomsPerPage = 2;
                idiomPresenter.Open();
                view.TabClicked.OnNext(1);

                Assert.IsTrue(view.IsIdiomMode);
                Assert.AreEqual(KanjiBookPresenter.IdiomSectionTitle, view.SummaryTitle);
                Assert.AreEqual(2, view.CollectedCount);
                Assert.AreEqual(3, view.TotalCount);
                CollectionAssert.AreEqual(new[] { "一石二鳥", "" }, view.Idioms);
                Assert.AreEqual(2, view.PageCount);
                Assert.AreEqual("一石二鳥", idiomPresenter.SelectedWord);
                Assert.AreEqual(0, view.SelectedIdiom);
                Assert.AreEqual("いっせきにちょう", view.IdiomDetail.Reading);
                Assert.AreEqual("ふたつの得", view.IdiomDetail.Meaning);
                Assert.AreEqual("今日は一石二鳥だ。", view.IdiomDetail.Example);
            }
        }

        [Test]
        public void まだ集めていない熟語は選べず次のページの熟語は選べる()
        {
            using (var idiomPresenter = CreateIdiomPresenter())
            {
                view.IdiomsPerPage = 2;
                idiomPresenter.Open();
                view.TabClicked.OnNext(1);

                view.IdiomClicked.OnNext(1);
                Assert.AreEqual("一石二鳥", idiomPresenter.SelectedWord);

                view.PageMoved.OnNext(1);
                CollectionAssert.AreEqual(new[] { "温故知新", null }, view.Idioms);
                view.IdiomClicked.OnNext(0);
                Assert.AreEqual("温故知新", idiomPresenter.SelectedWord);
                Assert.AreEqual("昔に学ぶ", view.IdiomDetail.Meaning);
            }
        }

        [Test]
        public void 四字熟語のタブでひらがなの設定を変えると意味と例文を出し直す()
        {
            using (var idiomPresenter = CreateIdiomPresenter())
            {
                view.IdiomsPerPage = 2;
                idiomPresenter.Open();
                view.TabClicked.OnNext(1);

                displaySettings.SetUseHiragana(true);

                Assert.AreEqual("ふたつのとく", view.IdiomDetail.Meaning);
                Assert.AreEqual("きょうは一石二鳥だ。", view.IdiomDetail.Example);
            }
        }

        [Test]
        public void 四字熟語のタブから漢字のタブに戻るとマス目に戻る()
        {
            using (var idiomPresenter = CreateIdiomPresenter())
            {
                idiomPresenter.Open();
                view.TabClicked.OnNext(1);

                view.TabClicked.OnNext(0);

                Assert.IsFalse(view.IsIdiomMode);
                Assert.IsNull(idiomPresenter.SelectedWord);
                Assert.AreEqual("二", idiomPresenter.SelectedKanji);
            }
        }
    }
}
