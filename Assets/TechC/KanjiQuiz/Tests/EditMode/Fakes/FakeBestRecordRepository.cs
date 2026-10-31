using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の自己ベストの保存先。メモリに保存し、保存した回数を数える
    /// </summary>
    public class FakeBestRecordRepository : IBestRecordRepository
    {
        private readonly Dictionary<string, int> records = new Dictionary<string, int>();

        /// <summary>Save が呼ばれた回数</summary>
        public int SaveCount { get; private set; }

        public bool TryLoad(string key, out int bestCorrectCount) => records.TryGetValue(key, out bestCorrectCount);

        public void Save(string key, int bestCorrectCount)
        {
            records[key] = bestCorrectCount;
            SaveCount++;
        }

        public void Delete(string key) => records.Remove(key);
    }
}
