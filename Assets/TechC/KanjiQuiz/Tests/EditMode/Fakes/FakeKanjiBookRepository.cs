using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の漢字帳の保存先。メモリに保存し、保存した回数を数える
    /// </summary>
    public class FakeKanjiBookRepository : IKanjiBookRepository
    {
        private readonly Dictionary<string, List<string>> books = new Dictionary<string, List<string>>();

        /// <summary>Save が呼ばれた回数</summary>
        public int SaveCount { get; private set; }

        public IReadOnlyCollection<string> Load(string modeId) =>
            books.TryGetValue(modeId, out var list) ? new List<string>(list) : new List<string>();

        public void Save(string modeId, IEnumerable<string> kanji)
        {
            books[modeId] = new List<string>(kanji);
            SaveCount++;
        }

        public void Delete(string modeId) => books.Remove(modeId);
    }
}
