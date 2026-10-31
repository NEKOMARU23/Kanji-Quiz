using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の苦手な問題の保存先。メモリに保存する
    /// </summary>
    public class FakeWeakQuestionRepository : IWeakQuestionRepository
    {
        private readonly Dictionary<string, WeakQuestionList> lists = new Dictionary<string, WeakQuestionList>();

        public WeakQuestionList Load(string key) =>
            lists.TryGetValue(key, out var list) ? new WeakQuestionList(list.Items) : new WeakQuestionList();

        public void Save(string key, WeakQuestionList list) => lists[key] = new WeakQuestionList(list.Items);

        public void Delete(string key) => lists.Remove(key);
    }
}
