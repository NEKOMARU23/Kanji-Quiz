using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の漢字帳。表示の指示を記録し、入力をテストから送れる
    /// </summary>
    public class FakeKanjiBookView : IKanjiBookView
    {
        public readonly Subject<Unit> BackClicked = new Subject<Unit>();
        public readonly Subject<int> TabClicked = new Subject<int>();
        public readonly Subject<int> PageMoved = new Subject<int>();
        public readonly Subject<int> CellClicked = new Subject<int>();

        public Observable<Unit> OnBackClicked => BackClicked;
        public Observable<int> OnTabClicked => TabClicked;
        public Observable<int> OnPageMoved => PageMoved;
        public Observable<int> OnCellClicked => CellClicked;

        public int CellsPerPage { get; set; } = 4;

        public bool IsVisible { get; private set; }
        public IReadOnlyList<string> TabTitles { get; private set; }
        public int SelectedTab { get; private set; } = -1;
        public string SummaryTitle { get; private set; }
        public int CollectedCount { get; private set; }
        public int TotalCount { get; private set; }
        public IReadOnlyList<string> Cells { get; private set; }
        public int PageIndex { get; private set; }
        public int PageCount { get; private set; }
        public int SelectedCell { get; private set; } = -1;
        public KanjiBookDetail Detail { get; private set; }
        public int DetailCount { get; private set; }
        public IReadOnlyList<string> StrokePaths { get; private set; }

        public void SetVisible(bool visible) => IsVisible = visible;

        public void SetTabs(IReadOnlyList<string> titles, int selectedIndex)
        {
            TabTitles = titles;
            SelectedTab = selectedIndex;
        }

        public void SetSectionSummary(string title, int collectedCount, int totalCount)
        {
            SummaryTitle = title;
            CollectedCount = collectedCount;
            TotalCount = totalCount;
        }

        public void SetPage(IReadOnlyList<string> cells, int pageIndex, int pageCount)
        {
            Cells = cells;
            PageIndex = pageIndex;
            PageCount = pageCount;
        }

        public void SetSelectedCell(int index) => SelectedCell = index;

        public void ShowDetail(KanjiBookDetail detail)
        {
            Detail = detail;
            DetailCount++;
        }

        public void PlayStrokes(IReadOnlyList<string> strokePaths) => StrokePaths = strokePaths;

        public readonly Subject<int> IdiomClicked = new Subject<int>();
        public Observable<int> OnIdiomClicked => IdiomClicked;
        public int IdiomsPerPage { get; set; } = 2;
        public bool IsIdiomMode { get; private set; }
        public IReadOnlyList<string> Idioms { get; private set; }
        public int SelectedIdiom { get; private set; } = -1;
        public IdiomBookDetail IdiomDetail { get; private set; }

        public void SetIdiomMode(bool idiomMode) => IsIdiomMode = idiomMode;

        public void SetIdiomPage(IReadOnlyList<string> idioms, int pageIndex, int pageCount)
        {
            Idioms = idioms;
            PageIndex = pageIndex;
            PageCount = pageCount;
        }

        public void SetSelectedIdiom(int index) => SelectedIdiom = index;
        public void ShowIdiomDetail(IdiomBookDetail detail) => IdiomDetail = detail;
    }
}
