using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズの1問
    /// 選択肢ごとの書き順は問題を作った時点で確定していて、表示のたびに変わらない
    /// </summary>
    public sealed class StrokeOrderQuestion : IChoiceQuestion
    {
        private readonly List<int[]> choiceOrders;

        /// <summary>出題する漢字</summary>
        public string Kanji { get; }

        /// <summary>正しい書き順の画のデータ</summary>
        public IReadOnlyList<StrokeData> Strokes { get; }

        public int ChoiceCount => choiceOrders.Count;

        public int CorrectIndex { get; }

        /// <param name="kanji">出題する漢字</param>
        /// <param name="strokes">正しい書き順の画のデータ</param>
        /// <param name="choiceOrders">選択肢ごとの書き順（正しい書き順で何番目の画かの並び）</param>
        /// <param name="correctIndex">正解の選択肢の番号</param>
        public StrokeOrderQuestion(string kanji, IReadOnlyList<StrokeData> strokes, IEnumerable<int[]> choiceOrders, int correctIndex)
        {
            Kanji = kanji ?? throw new ArgumentNullException(nameof(kanji));
            Strokes = strokes ?? throw new ArgumentNullException(nameof(strokes));
            this.choiceOrders = new List<int[]>(choiceOrders ?? throw new ArgumentNullException(nameof(choiceOrders)));

            if (correctIndex < 0 || correctIndex >= this.choiceOrders.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(correctIndex));
            }
            foreach (var order in this.choiceOrders)
            {
                if (order.Length != strokes.Count)
                {
                    throw new ArgumentException("書き順の長さが画数と一致しません。", nameof(choiceOrders));
                }
            }

            CorrectIndex = correctIndex;
        }

        /// <summary>
        /// 選択肢の書き順（正しい書き順で何番目の画かの並び）を返す
        /// </summary>
        public IReadOnlyList<int> GetChoiceOrder(int choiceIndex)
        {
            return choiceOrders[choiceIndex];
        }

        /// <summary>
        /// 選択肢の書き順に並べた画のデータを返す
        /// </summary>
        public IReadOnlyList<StrokeData> GetChoiceStrokes(int choiceIndex)
        {
            var order = choiceOrders[choiceIndex];
            var result = new StrokeData[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                result[i] = Strokes[order[i]];
            }
            return result;
        }
    }
}
