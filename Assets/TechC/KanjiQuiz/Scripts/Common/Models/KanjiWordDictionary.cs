using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字ごとの使用例（熟語）の辞書
    /// JSON（Data/KanjiWords）から読み込む。Tools/generate_kanji_words.py で JMdict から作る
    /// </summary>
    public sealed class KanjiWordDictionary
    {
        private readonly Dictionary<string, KanjiWord[]> words = new Dictionary<string, KanjiWord[]>();

        /// <summary>載っている漢字の数</summary>
        public int Count => words.Count;

        /// <summary>
        /// 熟語の JSON から辞書を作る。漢字セットごとのファイルをまとめて渡せる
        /// </summary>
        public static KanjiWordDictionary FromJson(params string[] jsons)
        {
            if (jsons == null) throw new ArgumentNullException(nameof(jsons));

            var dictionary = new KanjiWordDictionary();
            foreach (var json in jsons)
            {
                if (string.IsNullOrEmpty(json)) continue;

                var data = JsonUtility.FromJson<KanjiWordSetData>(json);
                if (data == null || data.entries == null)
                {
                    throw new FormatException("熟語の JSON の形式が正しくありません。");
                }
                foreach (var entry in data.entries)
                {
                    if (string.IsNullOrEmpty(entry.kanji)) continue;
                    dictionary.Add(entry.kanji, entry.words);
                }
            }
            return dictionary;
        }

        /// <summary>
        /// 漢字の使用例を返す
        /// </summary>
        /// <returns>よく使われる順の熟語。載っていなければ空のリスト</returns>
        public IReadOnlyList<KanjiWord> GetWords(string kanji)
        {
            if (kanji != null && words.TryGetValue(kanji, out var list)) return list;
            return Array.Empty<KanjiWord>();
        }

        private void Add(string kanji, WordData[] data)
        {
            var list = new KanjiWord[data?.Length ?? 0];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = new KanjiWord(data[i].word ?? string.Empty, data[i].reading ?? string.Empty);
            }
            words[kanji] = list;
        }

        [Serializable]
        private class KanjiWordSetData
        {
            public string id;
            public string source;
            public EntryData[] entries;
        }

        [Serializable]
        private class EntryData
        {
            public string kanji;
            public WordData[] words;
        }

        [Serializable]
        private class WordData
        {
            public string word;
            public string reading;
        }
    }
}
