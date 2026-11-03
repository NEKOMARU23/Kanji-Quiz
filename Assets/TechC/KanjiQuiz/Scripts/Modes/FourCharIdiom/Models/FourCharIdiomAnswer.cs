using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 空いたマスごとに入れた候補の番号を、回答の記録（AnswerRecord.SelectedIndex）の1つの数にまとめる・戻す
    /// マスごとの「候補の番号＋1」（空なら 0）を、13 進数の1桁として並べる（候補は最大 12）
    /// </summary>
    public static class FourCharIdiomAnswer
    {
        /// <summary>まとめられる候補の数の上限</summary>
        public const int MaxTileCount = 12;

        private const int Radix = MaxTileCount + 1;

        /// <summary>
        /// マスごとに入れた候補の番号（空いたままなら -1）を1つの数にまとめる
        /// </summary>
        public static int Encode(IReadOnlyList<int> filledTiles)
        {
            if (filledTiles == null) throw new ArgumentNullException(nameof(filledTiles));
            if (filledTiles.Count > FourCharIdiomRule.Length) throw new ArgumentException("マスの数が多すぎます。", nameof(filledTiles));

            int value = 0;
            for (int i = filledTiles.Count - 1; i >= 0; i--)
            {
                int tile = filledTiles[i];
                if (tile < -1 || tile >= MaxTileCount) throw new ArgumentOutOfRangeException(nameof(filledTiles));
                value = value * Radix + tile + 1;
            }
            return value;
        }

        /// <summary>
        /// まとめた数から、マスごとに入れた候補の番号（空いたままなら -1）を戻す
        /// </summary>
        /// <param name="value">Encode でまとめた数。負の数（未回答）なら、どのマスも空</param>
        /// <param name="blankCount">空いたマスの数</param>
        public static int[] Decode(int value, int blankCount)
        {
            if (blankCount < 0 || blankCount > FourCharIdiomRule.Length) throw new ArgumentOutOfRangeException(nameof(blankCount));

            var tiles = new int[blankCount];
            for (int i = 0; i < blankCount; i++)
            {
                if (value <= 0)
                {
                    tiles[i] = -1;
                    continue;
                }
                tiles[i] = value % Radix - 1;
                value /= Radix;
            }
            return tiles;
        }
    }
}
