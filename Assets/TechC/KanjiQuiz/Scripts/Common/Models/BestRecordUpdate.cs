namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 1回のプレイで自己ベストがどうなったか
    /// </summary>
    public readonly struct BestRecordUpdate
    {
        /// <summary>このプレイを含めた最高の正解数</summary>
        public int BestCorrectCount { get; }

        /// <summary>このプレイで自己ベストを更新したかどうか</summary>
        public bool IsNewBest { get; }

        public BestRecordUpdate(int bestCorrectCount, bool isNewBest)
        {
            BestCorrectCount = bestCorrectCount;
            IsNewBest = isNewBest;
        }
    }
}
