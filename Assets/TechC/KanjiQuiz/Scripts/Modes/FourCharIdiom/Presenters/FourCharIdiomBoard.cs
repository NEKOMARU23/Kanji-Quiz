using System.Collections.Generic;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// マスと候補の表示内容を作る（プレイと振り返りで共通）
    /// </summary>
    public static class FourCharIdiomBoard
    {
        /// <summary>
        /// 4つのマスの表示内容を作る
        /// </summary>
        /// <param name="filledTiles">空いたマスごとに入れた候補の番号（空なら -1）</param>
        /// <param name="review">振り返りなら true（入れた字が正しいかどうかで見せ方を変える）</param>
        public static FourCharIdiomCell[] Cells(FourCharIdiomQuestion question, IReadOnlyList<int> filledTiles, bool review)
        {
            var shown = question.Compose(filledTiles);
            var cells = new FourCharIdiomCell[FourCharIdiomRule.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                bool isBlank = false;
                foreach (int position in question.BlankPositions) isBlank |= position == i;

                FourCharIdiomCellKind kind;
                if (!isBlank) kind = FourCharIdiomCellKind.Fixed;
                else if (review) kind = shown[i] == question.Characters[i] ? FourCharIdiomCellKind.Correct : FourCharIdiomCellKind.Incorrect;
                else kind = shown[i] == null ? FourCharIdiomCellKind.Blank : FourCharIdiomCellKind.Filled;
                cells[i] = new FourCharIdiomCell(shown[i], kind);
            }
            return cells;
        }

        /// <summary>候補ごとに、マスに入れて使っているかどうか</summary>
        public static bool[] UsedTiles(FourCharIdiomQuestion question, IReadOnlyList<int> filledTiles)
        {
            var used = new bool[question.Tiles.Count];
            if (filledTiles == null) return used;
            foreach (int tile in filledTiles)
            {
                if (tile >= 0 && tile < used.Length) used[tile] = true;
            }
            return used;
        }
    }
}
