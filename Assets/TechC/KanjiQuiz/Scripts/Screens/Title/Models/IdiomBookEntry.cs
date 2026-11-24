using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の、語のタブに出す語1つ分の情報
    /// 四字熟語づくり（<see cref="FromJson"/>）・読み当て（<see cref="FromReadingQuizJson"/>）・使い分け（<see cref="FromKanjiUsageJson"/>）の問題データから読む
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

        /// <summary>
        /// 読み当ての問題データ（Data/ReadingQuiz の JSON）から、語を並べた順に読む。同じ語は最初の1つにまとめる
        /// 読みは、正しい読みをすべて「、」でつなぐ。意味と例文のデータはないので空
        /// </summary>
        public static List<IdiomBookEntry> FromReadingQuizJson(params string[] jsons)
        {
            var result = new List<IdiomBookEntry>();
            var seen = new HashSet<string>();
            foreach (var json in jsons ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(json)) continue;
                var data = JsonUtility.FromJson<ReadingQuizData>(json);
                if (data?.questions == null) continue;
                foreach (var q in data.questions)
                {
                    if (q == null || string.IsNullOrEmpty(q.word) || !seen.Add(q.word)) continue;
                    string reading = q.readings == null ? string.Empty : string.Join("、", q.readings);
                    result.Add(new IdiomBookEntry(q.word, reading, string.Empty, string.Empty));
                }
            }
            return result;
        }

        /// <summary>
        /// 使い分けの問題データ（Data/KanjiUsage の JSON）から、正解の言葉を並べた順に読む。同じ言葉は最初の1つにまとめる
        /// 意味は正解の言葉の意味。例文は、文の空いたところに正解の言葉を入れたもの（言葉は漢字のまま。読みの印をつけない）
        /// </summary>
        public static List<IdiomBookEntry> FromKanjiUsageJson(params string[] jsons)
        {
            var result = new List<IdiomBookEntry>();
            var seen = new HashSet<string>();
            foreach (var json in jsons ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(json)) continue;
                var data = JsonUtility.FromJson<KanjiUsageData>(json);
                if (data?.questions == null) continue;
                foreach (var q in data.questions)
                {
                    if (q == null || string.IsNullOrEmpty(q.answer) || !seen.Add(q.answer)) continue;
                    string meaning = q.meanings != null && q.meanings.Length > 0 ? q.meanings[0] : string.Empty;
                    string example = (q.sentence ?? string.Empty).Replace("＿", q.answer);
                    result.Add(new IdiomBookEntry(q.answer, q.reading, meaning, example));
                }
            }
            return result;
        }

        [Serializable]
        private sealed class ReadingQuizData
        {
            public ReadingQuizEntry[] questions;
        }

        [Serializable]
        private sealed class ReadingQuizEntry
        {
            public string word;
            public string[] readings;
        }

        [Serializable]
        private sealed class KanjiUsageData
        {
            public KanjiUsageEntry[] questions;
        }

        [Serializable]
        private sealed class KanjiUsageEntry
        {
            public string reading;
            public string answer;
            public string sentence;
            public string[] meanings;
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
