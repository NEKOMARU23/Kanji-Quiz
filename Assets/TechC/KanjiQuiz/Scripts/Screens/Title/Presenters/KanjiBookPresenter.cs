using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の Presenter
    /// 出題範囲（タブ）ごとに、集めた字をマス目に書き、選んだ字の読み・意味・例文・使用例・正解したモード・書き順を出す
    /// 四字熟語のデータを渡すと、最後のタブに四字熟語のページを足す（正解した熟語を一覧に書き、選んだ熟語の読み・意味・例文を出す）
    /// </summary>
    public sealed class KanjiBookPresenter : ITitleSubScreen
    {
        /// <summary>右のページに出す使用例の熟語の数</summary>
        public const int MaxWords = 3;

        /// <summary>四字熟語のページのタブの名前</summary>
        public const string IdiomSectionTitle = "四字熟語";

        private readonly IKanjiBookView view;
        private readonly IAudioPlayer audioPlayer;
        private readonly IReadOnlyList<KanjiSet> sections;
        private readonly IReadOnlyList<QuizModeDefinition> modes;
        private readonly Func<KanjiBook> loadBook;
        private readonly Lazy<KanjiInfo> info;
        private readonly IKanjiStrokeSource strokeSource;
        private readonly DisplaySettings displaySettings;
        private readonly IReadOnlyList<IdiomBookEntry> idioms;
        private readonly Subject<Unit> closed = new Subject<Unit>();
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private CancellationTokenSource strokeCts;
        private KanjiBook book;
        private bool isOpen;
        private int sectionIndex;
        private int pageIndex;
        private string selectedKanji;
        private IdiomBookEntry selectedIdiom;

        public Observable<Unit> OnClosed => closed;

        /// <summary>今のタブの番号</summary>
        public int SectionIndex => sectionIndex;

        /// <summary>今のページの番号（0 始まり）</summary>
        public int PageIndex => pageIndex;

        /// <summary>選んでいる字。選んでいなければ null</summary>
        public string SelectedKanji => selectedKanji;

        /// <summary>四字熟語のページで選んでいる熟語。選んでいなければ null</summary>
        public string SelectedIdiom => selectedIdiom?.Word;

        /// <summary>今のタブが四字熟語のページかどうか</summary>
        public bool IsIdiomSection => HasIdioms && sectionIndex == sections.Count;

        /// <param name="sections">タブに並べる出題範囲（漢字セット）</param>
        /// <param name="modes">正解したモードの名前を出すためのモードの一覧（この順に出す）</param>
        /// <param name="loadBook">漢字帳を読む（開くたびに読み直す）</param>
        /// <param name="loadInfo">字の情報を読む（最初に字を選んだときに1回だけ呼ぶ）</param>
        /// <param name="strokeSource">書き順のデータの読み込み口</param>
        /// <param name="displaySettings">表示の設定（ひらがなで表示）</param>
        /// <param name="idioms">四字熟語のページに並べる熟語（この順に出す）。null か空なら四字熟語のページを出さない</param>
        public KanjiBookPresenter(
            IKanjiBookView view, IAudioPlayer audioPlayer, IReadOnlyList<KanjiSet> sections,
            IReadOnlyList<QuizModeDefinition> modes, Func<KanjiBook> loadBook, Func<KanjiInfo> loadInfo,
            IKanjiStrokeSource strokeSource, DisplaySettings displaySettings, IReadOnlyList<IdiomBookEntry> idioms = null)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
            this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
            this.modes = modes ?? throw new ArgumentNullException(nameof(modes));
            this.loadBook = loadBook ?? throw new ArgumentNullException(nameof(loadBook));
            if (loadInfo == null) throw new ArgumentNullException(nameof(loadInfo));
            info = new Lazy<KanjiInfo>(loadInfo);
            this.strokeSource = strokeSource ?? throw new ArgumentNullException(nameof(strokeSource));
            this.displaySettings = displaySettings ?? throw new ArgumentNullException(nameof(displaySettings));
            this.idioms = idioms ?? Array.Empty<IdiomBookEntry>();
        }

        private bool HasIdioms => idioms.Count > 0;

        private int TabCount => sections.Count + (HasIdioms ? 1 : 0);

        public void Initialize()
        {
            view.SetVisible(false);
            view.OnBackClicked.Subscribe(this, static (_, self) => self.Close()).AddTo(disposables);
            view.OnTabClicked.Subscribe(this, static (i, self) => self.SelectSection(i, playSound: true)).AddTo(disposables);
            view.OnPageMoved.Subscribe(this, static (d, self) => self.MovePage(d)).AddTo(disposables);
            view.OnCellClicked.Subscribe(this, static (i, self) => self.SelectCell(i)).AddTo(disposables);
            view.OnIdiomClicked.Subscribe(this, static (i, self) => self.SelectIdiomRow(i)).AddTo(disposables);
            // ひらがなの設定が変わったら、意味と例文を出し直す
            displaySettings.UseHiragana
                .Skip(1)
                .Subscribe(this, static (_, self) => self.RefreshDetail())
                .AddTo(disposables);
        }

        public void Open()
        {
            isOpen = true;
            book = loadBook();
            view.SetVisible(true);
            SelectSection(0, playSound: false);
        }

        private void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            CancelStrokes();
            audioPlayer.PlaySe(SeType.Select);
            view.SetVisible(false);
            closed.OnNext(Unit.Default);
        }

        /// <summary>タブを選ぶ。最初のページを出し、そのページで最初に集めた字を選ぶ</summary>
        private void SelectSection(int index, bool playSound)
        {
            if (!isOpen || index < 0 || index >= TabCount) return;

            sectionIndex = index;
            pageIndex = 0;
            var titles = new List<string>();
            foreach (var section in sections) titles.Add(section.Title);
            if (HasIdioms) titles.Add(IdiomSectionTitle);
            view.SetTabs(titles, index);

            if (IsIdiomSection)
            {
                OpenIdiomSection(playSound);
                return;
            }

            view.SetIdiomMode(false);
            selectedIdiom = null;
            var kanji = sections[index].Kanji;
            view.SetSectionSummary(sections[index].Title, book.CountCollected(kanji), kanji.Count);
            if (playSound) audioPlayer.PlaySe(SeType.PageTurn);

            ShowPage();
            Select(FirstCollectedOnPage());
        }

        private void MovePage(int delta)
        {
            if (!isOpen || sections.Count == 0) return;

            int next = Math.Clamp(pageIndex + delta, 0, PageCount - 1);
            if (next == pageIndex) return;

            pageIndex = next;
            audioPlayer.PlaySe(SeType.PageTurn);
            if (IsIdiomSection) ShowIdiomPage();
            else ShowPage();
        }

        private void SelectCell(int cellIndex)
        {
            if (!isOpen || IsIdiomSection || sections.Count == 0 || cellIndex < 0 || cellIndex >= CellsPerPage) return;

            var kanji = sections[sectionIndex].Kanji;
            int index = pageIndex * CellsPerPage + cellIndex;
            if (index >= kanji.Count || !book.IsCollected(kanji[index])) return;

            audioPlayer.PlaySe(SeType.Select);
            Select(kanji[index]);
        }

        private int CellsPerPage => Math.Max(1, view.CellsPerPage);

        private int PageCount
        {
            get
            {
                if (IsIdiomSection) return Math.Max(1, (idioms.Count + IdiomsPerPage - 1) / IdiomsPerPage);
                int count = sections.Count > 0 ? sections[sectionIndex].Kanji.Count : 0;
                return Math.Max(1, (count + CellsPerPage - 1) / CellsPerPage);
            }
        }

        private int IdiomsPerPage => Math.Max(1, view.IdiomsPerPage);

        /// <summary>四字熟語のページを開き、最初のページで最初に集めた熟語を選ぶ</summary>
        private void OpenIdiomSection(bool playSound)
        {
            view.SetIdiomMode(true);
            selectedKanji = null;
            CancelStrokes();
            view.PlayStrokes(Array.Empty<string>());
            int collected = 0;
            foreach (var idiom in idioms)
            {
                if (book.IsCollected(idiom.Word)) collected++;
            }
            view.SetSectionSummary(IdiomSectionTitle, collected, idioms.Count);
            if (playSound) audioPlayer.PlaySe(SeType.PageTurn);

            ShowIdiomPage();
            IdiomBookEntry first = null;
            int end = Math.Min(idioms.Count, IdiomsPerPage);
            for (int i = 0; i < end && first == null; i++)
            {
                if (book.IsCollected(idioms[i].Word)) first = idioms[i];
            }
            SelectIdiom(first);
        }

        private void ShowIdiomPage()
        {
            var rows = new string[IdiomsPerPage];
            for (int i = 0; i < rows.Length; i++)
            {
                int index = pageIndex * IdiomsPerPage + i;
                if (index >= idioms.Count) rows[i] = null;
                else rows[i] = book.IsCollected(idioms[index].Word) ? idioms[index].Word : string.Empty;
            }
            view.SetIdiomPage(rows, pageIndex, PageCount);
            view.SetSelectedIdiom(IdiomRowOf(selectedIdiom));
        }

        private void SelectIdiomRow(int row)
        {
            if (!isOpen || !IsIdiomSection || row < 0 || row >= IdiomsPerPage) return;

            int index = pageIndex * IdiomsPerPage + row;
            if (index >= idioms.Count || !book.IsCollected(idioms[index].Word)) return;

            audioPlayer.PlaySe(SeType.Select);
            SelectIdiom(idioms[index]);
        }

        /// <summary>熟語を選んで、右のページに読み・意味・例文を出す。null なら案内の文を出す</summary>
        private void SelectIdiom(IdiomBookEntry idiom)
        {
            selectedIdiom = idiom;
            view.SetSelectedIdiom(IdiomRowOf(idiom));
            view.ShowIdiomDetail(idiom == null ? null : CreateIdiomDetail(idiom));
        }

        private IdiomBookDetail CreateIdiomDetail(IdiomBookEntry idiom)
        {
            bool useHiragana = displaySettings.UseHiragana.CurrentValue;
            return new IdiomBookDetail(
                idiom.Word, idiom.Reading, RubyText.Render(idiom.Meaning, useHiragana), RubyText.Render(idiom.Example, useHiragana));
        }

        /// <summary>今のページでの熟語の行の番号。ページにないなら -1</summary>
        private int IdiomRowOf(IdiomBookEntry idiom)
        {
            if (idiom == null) return -1;
            int start = pageIndex * IdiomsPerPage;
            int end = Math.Min(idioms.Count, start + IdiomsPerPage);
            for (int i = start; i < end; i++)
            {
                if (idioms[i] == idiom) return i - start;
            }
            return -1;
        }

        /// <summary>表示の設定が変わったときに、右のページの意味と例文を出し直す</summary>
        private void RefreshDetail()
        {
            if (!isOpen) return;
            if (IsIdiomSection)
            {
                if (selectedIdiom != null) view.ShowIdiomDetail(CreateIdiomDetail(selectedIdiom));
            }
            else if (selectedKanji != null)
            {
                view.ShowDetail(CreateDetail(selectedKanji));
            }
        }

        private void ShowPage()
        {
            var kanji = sections.Count > 0 ? sections[sectionIndex].Kanji : (IReadOnlyList<string>)new string[0];
            var cells = new string[CellsPerPage];
            for (int i = 0; i < cells.Length; i++)
            {
                int index = pageIndex * CellsPerPage + i;
                if (index >= kanji.Count) cells[i] = null;
                else cells[i] = book.IsCollected(kanji[index]) ? kanji[index] : string.Empty;
            }
            view.SetPage(cells, pageIndex, PageCount);
            view.SetSelectedCell(CellIndexOf(selectedKanji));
        }

        private string FirstCollectedOnPage()
        {
            if (sections.Count == 0) return null;
            var kanji = sections[sectionIndex].Kanji;
            int end = Math.Min(kanji.Count, (pageIndex + 1) * CellsPerPage);
            for (int i = pageIndex * CellsPerPage; i < end; i++)
            {
                if (book.IsCollected(kanji[i])) return kanji[i];
            }
            return null;
        }

        /// <summary>今のページでの字のマスの番号。ページにないなら -1</summary>
        private int CellIndexOf(string target)
        {
            if (target == null || sections.Count == 0) return -1;
            var kanji = sections[sectionIndex].Kanji;
            int start = pageIndex * CellsPerPage;
            int end = Math.Min(kanji.Count, start + CellsPerPage);
            for (int i = start; i < end; i++)
            {
                if (kanji[i] == target) return i - start;
            }
            return -1;
        }

        /// <summary>字を選んで、右のページに内容と書き順を出す。null なら案内の文を出す</summary>
        private void Select(string kanji)
        {
            selectedKanji = kanji;
            view.SetSelectedCell(CellIndexOf(kanji));
            CancelStrokes();
            if (kanji == null)
            {
                view.ShowDetail(null);
                view.PlayStrokes(new string[0]);
                return;
            }

            view.ShowDetail(CreateDetail(kanji));
            strokeCts = new CancellationTokenSource();
            LoadStrokesAsync(kanji, strokeCts.Token).Forget();
        }

        private KanjiBookDetail CreateDetail(string kanji)
        {
            var kanjiInfo = info.Value;
            bool useHiragana = displaySettings.UseHiragana.CurrentValue;
            var meaning = kanjiInfo.Meaning(kanji);

            var words = new List<string>();
            foreach (var word in kanjiInfo.Words(kanji))
            {
                if (words.Count >= MaxWords) break;
                words.Add(string.IsNullOrEmpty(word.Reading) ? word.Word : $"{word.Word}（{word.Reading}）");
            }

            var modeNames = new List<string>();
            var collectedModes = book.ModesOf(kanji);
            foreach (var mode in modes)
            {
                if (mode != null && Contains(collectedModes, mode.ModeId)) modeNames.Add(mode.DisplayName);
            }

            return new KanjiBookDetail(
                kanji, kanjiInfo.Reading(kanji),
                RubyText.Render(meaning.Meaning ?? string.Empty, useHiragana),
                RubyText.Render(meaning.Example ?? string.Empty, useHiragana),
                words, modeNames);
        }

        private static bool Contains(IReadOnlyList<string> list, string value)
        {
            foreach (var item in list)
            {
                if (item == value) return true;
            }
            return false;
        }

        private async UniTaskVoid LoadStrokesAsync(string kanji, CancellationToken ct)
        {
            IReadOnlyList<string> paths;
            try
            {
                paths = await strokeSource.LoadStrokePathsAsync(kanji, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (ct.IsCancellationRequested) return;
            view.PlayStrokes(paths);
        }

        private void CancelStrokes()
        {
            strokeCts?.Cancel();
            strokeCts?.Dispose();
            strokeCts = null;
        }

        public void Dispose()
        {
            CancelStrokes();
            disposables.Dispose();
            closed.Dispose();
        }
    }
}
