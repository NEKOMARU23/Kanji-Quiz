using System.Collections.Generic;
using R3;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の表示（机の上に開いた、マス目の漢字練習帳）
    /// 左のページに出題範囲ごとのマス目、右のページに選んだ字の内容を出す
    /// </summary>
    public interface IKanjiBookView
    {
        /// <summary>[← タイトルへ] が押されたとき（Esc を含む）</summary>
        Observable<Unit> OnBackClicked { get; }

        /// <summary>出題範囲のタブが押されたとき（タブの番号）</summary>
        Observable<int> OnTabClicked { get; }

        /// <summary>ページをめくる操作（-1 で前、+1 で次。ボタンと ← →）</summary>
        Observable<int> OnPageMoved { get; }

        /// <summary>マスが押されたとき（ページの中のマスの番号）</summary>
        Observable<int> OnCellClicked { get; }

        /// <summary>1ページのマスの数</summary>
        int CellsPerPage { get; }

        /// <summary>漢字帳を表示するかどうか</summary>
        void SetVisible(bool visible);

        /// <summary>出題範囲のタブを並べ、選んでいるタブを目立たせる</summary>
        void SetTabs(IReadOnlyList<string> titles, int selectedIndex);

        /// <summary>今の出題範囲の名前と、集めた数／全部の数を出す</summary>
        void SetSectionSummary(string title, int collectedCount, int totalCount);

        /// <summary>
        /// ページのマスを出す
        /// </summary>
        /// <param name="cells">マスごとの字。集めた字はその字、まだの字は空文字（空のマス）、範囲の外は null（マスを出さない）</param>
        void SetPage(IReadOnlyList<string> cells, int pageIndex, int pageCount);

        /// <summary>選んでいるマスを囲む。-1 なら囲まない</summary>
        void SetSelectedCell(int index);

        /// <summary>右のページに字の内容を出す。null なら案内の文を出す</summary>
        void ShowDetail(KanjiBookDetail detail);

        /// <summary>右のページのお手本に、書き順を1画ずつ書く（KanjiVG の d 属性、書き順の順）。空なら何も書かない</summary>
        void PlayStrokes(IReadOnlyList<string> strokePaths);
    }
}
