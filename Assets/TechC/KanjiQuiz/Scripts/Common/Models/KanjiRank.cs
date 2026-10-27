using System;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 結果につける「級」（10級〜初段）
    /// 難易度ごとに全問正解のときの級が決まっていて、正解率が下がるほど級も下がる
    /// </summary>
    public static class KanjiRank
    {
        /// <summary>級の名前。低い順</summary>
        private static readonly string[] Names =
        {
            "10級", "9級", "8級", "7級", "6級", "5級", "4級", "3級", "2級", "1級", "初段",
        };

        /// <summary>全問正解のときに出る級の位置（Names の番号）</summary>
        private static int TopLevel(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Normal: return 7; // 3級
                case Difficulty.Hard: return 10;  // 初段
                default: return 5;                // 5級
            }
        }

        /// <summary>
        /// 正解数と難易度から級を決める
        /// 全問正解なら難易度ごとの最高の級、正解率が 20% 下がるごとに1つずつ下がる（最大5つ）
        /// </summary>
        /// <returns>級の名前（例：5級、初段）</returns>
        public static string Evaluate(int correctCount, int total, Difficulty difficulty)
        {
            if (total <= 0) throw new ArgumentOutOfRangeException(nameof(total));
            if (correctCount < 0 || correctCount > total) throw new ArgumentOutOfRangeException(nameof(correctCount));

            return Names[TopLevel(difficulty) - StepsDown(correctCount, total)];
        }

        private static int StepsDown(int correctCount, int total)
        {
            if (correctCount == total) return 0;

            // 整数で比べて、割り算の誤差で境目がずれないようにする（5問中4問は 80% ちょうど）
            int percent = correctCount * 100;
            if (percent >= total * 80) return 1;
            if (percent >= total * 60) return 2;
            if (percent >= total * 40) return 3;
            if (percent >= total * 20) return 4;
            return 5;
        }
    }
}
