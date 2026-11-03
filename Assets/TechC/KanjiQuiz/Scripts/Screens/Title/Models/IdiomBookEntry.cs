using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の四字熟語のページに出す、熟語1つ分の情報（四字熟語づくりの問題データから読む）
    /// </summary>
    public sealed class IdiomBookEntry
    {
        /// <summary>四字熟語</summary>
        public string Word { get; }

        /// <summary>読み（ひらがな）</summary>
        public string Reading { get; }

        /// <summary>意味（{漢字|よみ} の読みつき）</summary>
        public string Meaning { get; }

        /// <summary>例文（{漢字|よみ} の読みつき）</summary>
        public string Example { get; }

        public IdiomBookEntry(string word, string reading, string meaning, string example)
        {
            Word = word ?? string.Empty;
            Reading = reading ?? string.Empty;
            Meaning = meaning ?? string.Empty;
            Example = example ?? string.Empty;
        }

        /// <summary>
        /// 四字熟語づくりの問題データ（Data/FourCharIdiom の JSON）から、熟語を並べた順に読む。同じ熟語は1つにまとめる
        /// </summary>
        public static List<IdiomBookEntry> FromJson(params string[] jsons)
        {
            var result = new List<IdiomBookEntry>();
            var seen = new HashSet<string>();
            foreach (var json in jsons ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(json)) continue;
                var data = JsonUtility.FromJson<Data>(json);
                if (data?.questions == null) continue;
                foreach (var q in data.questions)
                {
                    if (q == null || string.IsNullOrEmpty(q.word) || !seen.Add(q.word)) continue;
                    result.Add(new IdiomBookEntry(q.word, q.reading, q.meaning, q.example));
                }
            }
            return result;
        }

        [Serializable]
        private sealed class Data
        {
            public Entry[] questions;
        }

        [Serializable]
        private sealed class Entry
        {
            public string word;
            public string reading;
            public string meaning;
            public string example;
        }
    }
}
