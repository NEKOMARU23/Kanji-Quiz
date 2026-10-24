using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 不正解の書き順（画の並び順）を作る
    /// 作った書き順は、正しい書き順とも、お互いとも重複しないことを保証する
    /// </summary>
    public static class WrongOrderGenerator
    {
        /// <summary>候補を作り直す回数の上限。超えたら作れなかったとみなす</summary>
        public const int MaxAttempts = 200;

        /// <summary>
        /// 難易度ごとの出題対象の最小画数
        /// </summary>
        public static int MinStrokeCount(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Normal: return 3;
                case Difficulty.Hard: return 5;
                default: return 1;
            }
        }

        /// <summary>
        /// 不正解の書き順を指定した数だけ作る
        /// 書き順は「正しい書き順で何番目の画か」の並びで表す（正しい書き順は 0, 1, 2, …）
        /// </summary>
        /// <param name="strokeCount">漢字の画数</param>
        /// <param name="difficulty">難易度</param>
        /// <param name="count">作る数</param>
        /// <param name="orders">作った書き順</param>
        /// <returns>指定した数だけ作れた場合は true。画数が少なくて作れない場合は false</returns>
        public static bool TryGenerate(int strokeCount, Difficulty difficulty, int count, out List<int[]> orders)
        {
            orders = new List<int[]>(count);
            if (strokeCount < MinStrokeCount(difficulty) || strokeCount < 2) return false;

            var correct = CreateIdentity(strokeCount);
            for (int attempt = 0; attempt < MaxAttempts && orders.Count < count; attempt++)
            {
                var candidate = CreateCandidate(strokeCount, difficulty);
                if (IsSameOrder(candidate, correct) || ContainsOrder(orders, candidate)) continue;
                orders.Add(candidate);
            }

            return orders.Count == count;
        }

        /// <summary>
        /// 正しい書き順（0, 1, 2, …）を作る
        /// </summary>
        public static int[] CreateIdentity(int strokeCount)
        {
            var order = new int[strokeCount];
            for (int i = 0; i < strokeCount; i++) order[i] = i;
            return order;
        }

        /// <summary>
        /// 2つの書き順が同じかどうか
        /// </summary>
        public static bool IsSameOrder(IReadOnlyList<int> a, IReadOnlyList<int> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private static bool ContainsOrder(List<int[]> orders, int[] candidate)
        {
            foreach (var order in orders)
            {
                if (IsSameOrder(order, candidate)) return true;
            }
            return false;
        }

        /// <summary>
        /// 難易度「常識」で位置を変える画の割合
        /// </summary>
        public const float NormalChangeRatio = 0.3f;

        /// <summary>
        /// 難易度「常識」で位置を変える画の数（画数の30%、最低2画）
        /// </summary>
        public static int NormalChangeCount(int strokeCount)
        {
            int count = (int)Math.Round(strokeCount * NormalChangeRatio, MidpointRounding.AwayFromZero);
            return Math.Min(strokeCount, Math.Max(2, count));
        }

        private static int[] CreateCandidate(int strokeCount, Difficulty difficulty)
        {
            var order = CreateIdentity(strokeCount);

            switch (difficulty)
            {
                case Difficulty.Easy:
                    // 全画をランダムに並べ替える
                    ShuffleUtility.Shuffle(order);
                    break;

                case Difficulty.Normal:
                    // 連続した画数の30%の画を、すべて元の位置から動かす
                    DerangeBlock(order, NormalChangeCount(strokeCount));
                    break;

                case Difficulty.Hard:
                    // 隣り合う2画を1か所だけ入れ替える
                    int index = UnityEngine.Random.Range(0, strokeCount - 1);
                    (order[index], order[index + 1]) = (order[index + 1], order[index]);
                    break;
            }

            return order;
        }

        /// <summary>
        /// ランダムな位置から連続した blockSize 個の画を選び、どの画も元の位置に残らないように並べ替える
        /// （Sattolo のアルゴリズム。必ず全要素が動く並べ替えになる）
        /// </summary>
        private static void DerangeBlock(int[] order, int blockSize)
        {
            int start = UnityEngine.Random.Range(0, order.Length - blockSize + 1);
            for (int i = blockSize - 1; i > 0; i--)
            {
                int k = UnityEngine.Random.Range(0, i);
                (order[start + i], order[start + k]) = (order[start + k], order[start + i]);
            }
        }
    }
}
