using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Modes.FourCharIdiom;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の四字熟語づくりの表示。表示の指示を記録し、候補やマスを押したことをテストから送れる
    /// </summary>
    public class FakeFourCharIdiomView : IFourCharIdiomView
    {
        public readonly Subject<int> TileClicked = new Subject<int>();
        public readonly Subject<int> CellClicked = new Subject<int>();

        public Observable<int> OnTileClicked => TileClicked;
        public Observable<int> OnCellClicked => CellClicked;

        public string Meaning { get; private set; }
        public IReadOnlyList<FourCharIdiomCell> Cells { get; private set; }
        public IReadOnlyList<string> Tiles { get; private set; }
        public IReadOnlyList<bool> UsedTiles { get; private set; }
        public bool IsInteractable { get; private set; } = true;
        public bool IsWeakMarkVisible { get; private set; }
        public string AnswerWord { get; private set; }
        public string AnswerReading { get; private set; }
        public string AnswerExample { get; private set; }

        /// <summary>マスに見えている4字（空いたマスは「？」）</summary>
        public string Shown
        {
            get
            {
                var text = string.Empty;
                foreach (var cell in Cells) text += cell.Character ?? "？";
                return text;
            }
        }

        public void SetMeaning(string meaning) => Meaning = meaning;
        public void SetCells(IReadOnlyList<FourCharIdiomCell> cells) => Cells = cells;

        public void SetTiles(IReadOnlyList<string> tiles, IReadOnlyList<bool> used)
        {
            Tiles = tiles;
            UsedTiles = used;
        }

        public void SetInteractable(bool interactable) => IsInteractable = interactable;
        public void SetWeakMarkVisible(bool visible) => IsWeakMarkVisible = visible;

        public void ShowAnswerInfo(string word, string reading, string example)
        {
            AnswerWord = word;
            AnswerReading = reading;
            AnswerExample = example;
        }

        /// <summary>指定した字の候補を押す（同じ字が2つあれば、まだ使っていない方）</summary>
        public void ClickTile(string character)
        {
            for (int i = 0; i < Tiles.Count; i++)
            {
                if (Tiles[i] == character && !UsedTiles[i])
                {
                    TileClicked.OnNext(i);
                    return;
                }
            }
        }
    }
}
