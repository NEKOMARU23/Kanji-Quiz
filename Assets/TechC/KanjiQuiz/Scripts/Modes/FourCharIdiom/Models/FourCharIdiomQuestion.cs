using System;
using System.Collections.Generic;
using System.Text;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 四字熟語づくりの1問分のデータ
    /// どのマスを空けるかと、候補の字の並びは、問題を作るときに決める（前の問題に戻っても変わらない）
    /// </summary>
    public sealed class FourCharIdiomQuestion
    {
        private readonly string[] characters;
        private readonly int[] blankPositions;
        private readonly string[] tiles;

        /// <summary>四字熟語（例：一石二鳥）</summary>
        public string Word { get; }

        /// <summary>読み（ひらがな）</summary>
        public string Reading { get; }

        /// <summary>意味（{漢字|よみ} の読みつき）</summary>
        public string Meaning { get; }

        /// <summary>例文（{漢字|よみ} の読みつき）</summary>
        public string Example { get; }

        /// <summary>熟語の4字</summary>
        public IReadOnlyList<string> Characters => characters;

        /// <summary>空けるマスの位置（0〜3、小さい順）</summary>
        public IReadOnlyList<int> BlankPositions => blankPositions;

        /// <summary>候補の字（空けたマスの正解の字と、まぎらわしい字を混ぜたもの）</summary>
        public IReadOnlyList<string> Tiles => tiles;

        public FourCharIdiomQuestion(
            string word, string reading, string meaning, string example,
            IReadOnlyList<int> blankPositions, IReadOnlyList<string> tiles)
        {
            if (string.IsNullOrEmpty(word)) throw new ArgumentException("熟語が空です。", nameof(word));
            characters = KanjiSet.SplitCharacters(word).ToArray();
            if (characters.Length != FourCharIdiomRule.Length) throw new ArgumentException($"熟語「{word}」が4字ではありません。", nameof(word));
            if (blankPositions == null || blankPositions.Count == 0) throw new ArgumentException("空けるマスがありません。", nameof(blankPositions));
            if (tiles == null || tiles.Count < blankPositions.Count || tiles.Count > FourCharIdiomAnswer.MaxTileCount)
            {
                throw new ArgumentException("候補の字の数が正しくありません。", nameof(tiles));
            }

            Word = word;
            Reading = reading ?? string.Empty;
            Meaning = meaning ?? string.Empty;
            Example = example ?? string.Empty;
            this.blankPositions = new int[blankPositions.Count];
            for (int i = 0; i < blankPositions.Count; i++)
            {
                int position = blankPositions[i];
                if (position < 0 || position >= FourCharIdiomRule.Length) throw new ArgumentOutOfRangeException(nameof(blankPositions));
                if (i > 0 && position <= this.blankPositions[i - 1]) throw new ArgumentException("空けるマスは小さい順に、重ならないように並べてください。", nameof(blankPositions));
                this.blankPositions[i] = position;
            }
            this.tiles = new string[tiles.Count];
            for (int i = 0; i < tiles.Count; i++) this.tiles[i] = tiles[i];
        }

        /// <summary>
        /// マスごとに入れた候補の番号（空いたままなら -1）から、4つのマスに見える字を返す（空いたマスは null）
        /// </summary>
        public string[] Compose(IReadOnlyList<int> filledTiles)
        {
            var result = new string[FourCharIdiomRule.Length];
            for (int i = 0; i < result.Length; i++) result[i] = characters[i];
            for (int k = 0; k < blankPositions.Length; k++)
            {
                int tile = filledTiles != null && k < filledTiles.Count ? filledTiles[k] : -1;
                result[blankPositions[k]] = tile >= 0 && tile < tiles.Length ? tiles[tile] : null;
            }
            return result;
        }

        /// <summary>空いたマスがすべて埋まっているかどうか</summary>
        public bool IsComplete(IReadOnlyList<int> filledTiles)
        {
            if (filledTiles == null || filledTiles.Count < blankPositions.Length) return false;
            for (int k = 0; k < blankPositions.Length; k++)
            {
                if (filledTiles[k] < 0 || filledTiles[k] >= tiles.Length) return false;
            }
            return true;
        }

        /// <summary>
        /// 入れた字で熟語ができているかどうか。同じ字の候補が2つあるときは、どちらを使っても正解
        /// </summary>
        public bool IsCorrect(IReadOnlyList<int> filledTiles)
        {
            if (!IsComplete(filledTiles)) return false;
            var builder = new StringBuilder();
            foreach (var ch in Compose(filledTiles)) builder.Append(ch);
            return builder.ToString() == Word;
        }
    }
}
