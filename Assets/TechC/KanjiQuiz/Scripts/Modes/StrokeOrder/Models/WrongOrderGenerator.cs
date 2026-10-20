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

        private static int[] CreateCandidate(int strokeCount, Difficulty difficulty)
        {
            var order = CreateIdentity(strokeCount);

            switch (difficulty)
            {
                case Difficulty.Easy:
                    // 全画を並べ替える
                    ShuffleUtility.Shuffle(order);
                    break;

                case Difficulty.Normal:
                    // ランダムに選んだ2〜3画だけを並べ替える
                    ShuffleSubset(order, UnityEngine.Random.Range(2, 4));
                    break;

                case Difficulty.Hard:
                    // ランダムに選んだ2画だけを入れ替える
                    ShuffleSubset(order, 2);
                    break;
            }

            return order;
        }

        private static void ShuffleSubset(int[] order, int subsetSize)
        {
            subsetSize = System.Math.Min(subsetSize, order.Length);

            var indices = ShuffleUtility.ShuffledCopy(CreateIdentity(order.Length)).GetRange(0, subsetSize);
            var values = new List<int>(subsetSize);
            foreach (int index in indices) values.Add(order[index]);

            ShuffleUtility.Shuffle(values);
            for (int i = 0; i < subsetSize; i++)
            {
                order[indices[i]] = values[i];
            }
        }
    }
}
