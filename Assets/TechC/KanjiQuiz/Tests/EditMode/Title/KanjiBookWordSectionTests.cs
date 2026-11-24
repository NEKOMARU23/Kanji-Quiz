using System.Collections.Generic;
using NUnit.Framework;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Title;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// 漢字帳の語のタブ（四字熟語・読み当て・使い分け）のテスト
    /// 読み当てのタブは、集めた語だけを集めた順に並べる。使い分けのタブは、全部の言葉を並べてまだの言葉を「？？」にする
    /// </summary>
    public class KanjiBookWordSectionTests
    {
        private const string Idiom = "FourCharIdiom";
        private const string Reading = "ReadingQuiz";
        private const string Usage = "KanjiUsage";

        private readonly List<Object> created = new List<Object>();
        private FakeKanjiBookView view;
        private FakeKanjiBookRepository repository;
        private FakeDisplaySettingRepository displayRepository;
        private DisplaySettings displaySettings;
        private KanjiBookPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            view = new FakeKanjiBookView { CellsPerPage = 4, IdiomsPerPage = 2 };
            repository = new FakeKanjiBookRepository();
            displayRepository = new FakeDisplaySettingRepository();
            displaySettings = new DisplaySettings(displayRepository);
        }

        [TearDown]
        public void TearDown()
        {
            presenter?.Dispose();
            foreach (var obj in created) Object.DestroyImmediate(obj);
            created.Clear();
        }

        private static KanjiInfo LoadInfo()
        {
            return new KanjiInfo(
                KanjiReadingDictionary.FromJson("{\"questions\":[]}"),
                KanjiMeaningDictionary.FromJson("{\"entries\":[]}"),
                KanjiWordDictionary.FromJson("{\"entries\":[]}"));
        }

        /// <summary>漢字のタブ1つ（小学漢字）のあとに、指定した語のタブをつけた漢字帳を開く</summary>
        private KanjiBookPresenter Open(params WordBookSection[] wordSections)
        {
            var modes = new List<QuizModeDefinition>();
            foreach (var id in new[] { "StrokeOrder", Idiom, Reading, Usage })
            {
                var mode = QuizModeDefinition.Create(id, id, id);
                created.Add(mode);
                modes.Add(mode);
            }

            presenter = new KanjiBookPresenter(
                view, new FakeAudioPlayer(), new[] { new KanjiSet("A", "小学漢字", new[] { "一", "二" }) }, modes,
                () => KanjiBook.Load(repository, new[] { "StrokeOrder", Idiom, Reading, Usage }),
                LoadInfo, new FakeKanjiStrokeSource(new Dictionary<string, int>()), displaySettings, wordSections);
            presenter.Initialize();
            presenter.Open();
            return presenter;
        }

        private static IReadOnlyList<IdiomBookEntry> Words(params string[] words)
        {
            var list = new List<IdiomBookEntry>();
            foreach (var word in words) list.Add(new IdiomBookEntry(word, "よみ" + word, "意味" + word, "例" + word));
            return list;
        }

        [Test]
        public void 語のタブは漢字のタブのあとにモードの順で並ぶ()
        {
            Open(
                new WordBookSection("四字熟語", Idiom, Words("一石二鳥"), true),
                new WordBookSection("読み当て", Reading, Words("今日"), false),
                new WordBookSection("使い分け", Usage, Words("回答"), true));

            CollectionAssert.AreEqual(new[] { "小学漢字", "四字熟語", "読み当て", "使い分け" }, view.TabTitles);
        }

        [Test]
        public void 集めた語だけを並べるタブは集めた順に並べ語の数だけを出す()
        {
            // データでは「明日」「今日」の順だが、集めたのは「今日」「明日」の順
            repository.Save(Reading, new[] { "今日", "明日" });
            Open(new WordBookSection("読み当て", Reading, Words("明日", "今日", "昨日"), false));

            view.TabClicked.OnNext(1);

            Assert.IsTrue(view.IsIdiomMode);
            CollectionAssert.AreEqual(new[] { "今日", "明日" }, view.Idioms, "データにある「昨日」は集めていないので並べない");
            Assert.AreEqual(2, view.CollectedCount);
            Assert.AreEqual(3, view.TotalCount, "コンプ率の分母は、データの語の数");
            Assert.AreEqual(1, view.PageCount);
            Assert.AreEqual("今日", presenter.SelectedWord);
            Assert.AreEqual("よみ今日", view.IdiomDetail.Reading);
        }

        [Test]
        public void 集めた語だけを並べるタブで次のページの語を選べる()
        {
            repository.Save(Reading, new[] { "一", "二", "三" });
            Open(new WordBookSection("読み当て", Reading, Words("一", "二", "三", "四"), false));
            view.TabClicked.OnNext(1);

            Assert.AreEqual(2, view.PageCount, "3語を1ページ2つで2ページ");
            view.PageMoved.OnNext(1);
            CollectionAssert.AreEqual(new[] { "三", null }, view.Idioms);

            view.IdiomClicked.OnNext(0);

            Assert.AreEqual("三", presenter.SelectedWord);
            Assert.AreEqual("意味三", view.IdiomDetail.Meaning);
        }

        [Test]
        public void 集めた語がデータになくても読みなしで残す()
        {
            // 問題データを作り直して外れた語でも、集めた記録は消さない
            repository.Save(Reading, new[] { "消えた語", "今日" });
            Open(new WordBookSection("読み当て", Reading, Words("今日"), false));

            view.TabClicked.OnNext(1);

            CollectionAssert.AreEqual(new[] { "消えた語", "今日" }, view.Idioms);
            Assert.AreEqual(1, view.CollectedCount, "データから外れた語は数に入れない（コンプ率が100%を超えないように）");
            Assert.AreEqual(1, view.TotalCount);
            Assert.AreEqual("消えた語", presenter.SelectedWord);
            Assert.AreEqual(string.Empty, view.IdiomDetail.Reading);
            view.IdiomClicked.OnNext(1);
            Assert.AreEqual("よみ今日", view.IdiomDetail.Reading);
        }

        [Test]
        public void 集めた語がなければ空の一覧と案内を出す()
        {
            Open(new WordBookSection("読み当て", Reading, Words("今日"), false));

            view.TabClicked.OnNext(1);

            CollectionAssert.AreEqual(new string[] { null, null }, view.Idioms);
            Assert.AreEqual(0, view.CollectedCount);
            Assert.IsNull(presenter.SelectedWord);
            Assert.IsNull(view.IdiomDetail, "案内の文を出す");
        }

        [Test]
        public void 全部並べるタブはまだの言葉を空文字にして集めた数と全部の数を出す()
        {
            repository.Save(Usage, new[] { "回答" });
            Open(new WordBookSection("使い分け", Usage, Words("回答", "解答", "解凍"), true));

            view.TabClicked.OnNext(1);

            CollectionAssert.AreEqual(new[] { "回答", "" }, view.Idioms);
            Assert.AreEqual(1, view.CollectedCount);
            Assert.AreEqual(3, view.TotalCount);
            Assert.AreEqual("回答", presenter.SelectedWord);
        }

        [Test]
        public void 同じ語がほかのモードで集まっていても四字熟語のタブでは集めた扱いにならない()
        {
            // 「一心同体」は四字熟語づくりにも読み当てにもある（Data の 81 語が重なる）。読み当てだけで集めた場合
            repository.Save(Reading, new[] { "一心同体" });
            repository.Save(Idiom, new[] { "一石二鳥" });
            Open(
                new WordBookSection("四字熟語", Idiom, Words("一石二鳥", "一心同体"), true),
                new WordBookSection("読み当て", Reading, Words("一心同体"), false));

            view.TabClicked.OnNext(1);

            CollectionAssert.AreEqual(new[] { "一石二鳥", "" }, view.Idioms, "読み当てで集めた「一心同体」は、四字熟語ではまだ");
            Assert.AreEqual(1, view.CollectedCount);

            view.TabClicked.OnNext(2);
            CollectionAssert.AreEqual(new[] { "一心同体", null }, view.Idioms, "読み当てには「一心同体」だけ（四字熟語の「一石二鳥」は出ない）");
        }

        [Test]
        public void 使い分けの正解が漢字1字のときは漢字のマス目でも集めた字になる()
        {
            // 使い分けの正解には「川」「木」のような漢字1字がある。今の仕様では、どのモードで集めた字も漢字のマス目に出す
            repository.Save(Usage, new[] { "二" });
            var kanjiPresenter = Open(new WordBookSection("使い分け", Usage, Words("二"), true));

            Assert.AreEqual(1, view.CollectedCount, "小学漢字のマス目に「二」が集めた字として出る");
            Assert.AreEqual("二", kanjiPresenter.SelectedKanji);
            CollectionAssert.AreEqual(new[] { Usage }, view.Detail.ModeNames, "正解したモードに使い分けが出る");
        }

        [Test]
        public void 語のタブでひらがなの設定を変えると意味と例文を出し直す()
        {
            repository.Save(Usage, new[] { "回答" });
            var sections = new[]
            {
                new WordBookSection("使い分け", Usage, new[]
                {
                    new IdiomBookEntry("回答", "かいとう", "{質問|しつもん}に{答|こた}えること。", "{問題|もんだい}の回答を書く。"),
                }, true),
            };
            Open(sections);
            view.TabClicked.OnNext(1);
            Assert.AreEqual("質問に答えること。", view.IdiomDetail.Meaning);

            displaySettings.SetUseHiragana(true);

            Assert.AreEqual("しつもんにこたえること。", view.IdiomDetail.Meaning);
            Assert.AreEqual("もんだいの回答を書く。", view.IdiomDetail.Example, "例文の中の正解の言葉は漢字のまま");
        }

        [Test]
        public void 語のデータは最初にそのタブを開くまで読まない()
        {
            int loadCount = 0;
            Open(new WordBookSection("読み当て", Reading, () =>
            {
                loadCount++;
                return Words("今日");
            }, false));

            Assert.AreEqual(0, loadCount, "漢字のタブを開いただけでは読まない");

            view.TabClicked.OnNext(1);
            view.TabClicked.OnNext(0);
            view.TabClicked.OnNext(1);

            Assert.AreEqual(1, loadCount, "読むのは1回だけ");
        }

        [Test]
        public void 開き直すと集めた語から一覧を作り直す()
        {
            repository.Save(Reading, new[] { "今日" });
            Open(new WordBookSection("読み当て", Reading, Words("今日", "明日"), false));
            view.TabClicked.OnNext(1);
            CollectionAssert.AreEqual(new[] { "今日", null }, view.Idioms);

            // 漢字帳を閉じているあいだに、新しく「明日」を集めた
            repository.Save(Reading, new[] { "今日", "明日" });
            view.BackClicked.OnNext(Unit.Default);
            presenter.Open();
            view.TabClicked.OnNext(1);

            CollectionAssert.AreEqual(new[] { "今日", "明日" }, view.Idioms);
        }
    }
}
