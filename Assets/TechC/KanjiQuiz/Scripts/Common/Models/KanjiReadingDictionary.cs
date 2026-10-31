using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字ごとの読みの辞書。本物の漢字さがしの問題データ（Data/RealKanji の JSON。kanji と reading を持つ）から作る
    /// </summary>
    public sealed class KanjiReadingDictionary
    {
        private readonly Dictionary<string, string> readings = new Dictionary<string, string>();

        /// <summary>読みのある漢字の数</summary>
        public int Count => readings.Count;

        /// <summary>
        /// JSON から作る。同じ漢字が2つ以上あれば、最初のものを使う
        /// </summary>
        public static KanjiReadingDictionary FromJson(params string[] jsons)
        {
            var dictionary = new KanjiReadingDictionary();
            if (jsons == null) return dictionary;

            foreach (var json in jsons)
            {
                if (string.IsNullOrEmpty(json)) continue;
                var data = JsonUtility.FromJson<FileData>(json);
                if (data?.questions == null) continue;
                foreach (var entry in data.questions)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.kanji) || string.IsNullOrEmpty(entry.reading)) continue;
                    if (!dictionary.readings.ContainsKey(entry.kanji)) dictionary.readings.Add(entry.kanji, entry.reading);
                }
            }
            return dictionary;
        }

        /// <summary>漢字の読み。なければ空文字</summary>
        public string Get(string kanji)
        {
            return kanji != null && readings.TryGetValue(kanji, out var reading) ? reading : string.Empty;
        }

        [Serializable]
        private class FileData
        {
            public EntryData[] questions;
        }

        [Serializable]
        private class EntryData
        {
            public string kanji;
            public string reading;
        }
    }
}
