using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字ごとの意味と例文の辞書
    /// JSON（Data/KanjiMeanings）から読み込む。AI で作り、Tools/check_kanji_meanings.py で確かめたもの
    /// </summary>
    public sealed class KanjiMeaningDictionary
    {
        private readonly Dictionary<string, KanjiMeaning> meanings = new Dictionary<string, KanjiMeaning>();

        /// <summary>載っている漢字の数</summary>
        public int Count => meanings.Count;

        /// <summary>
        /// 意味と例文の JSON から辞書を作る。漢字セットごとのファイルをまとめて渡せる
        /// </summary>
        public static KanjiMeaningDictionary FromJson(params string[] jsons)
        {
            if (jsons == null) throw new ArgumentNullException(nameof(jsons));

            var dictionary = new KanjiMeaningDictionary();
            foreach (var json in jsons)
            {
                if (string.IsNullOrEmpty(json)) continue;

                var data = JsonUtility.FromJson<SetData>(json);
                if (data == null || data.entries == null)
                {
                    throw new FormatException("意味と例文の JSON の形式が正しくありません。");
                }

                foreach (var entry in data.entries)
                {
                    if (string.IsNullOrEmpty(entry.kanji)) continue;
                    dictionary.meanings[entry.kanji] = new KanjiMeaning(entry.meaning, entry.example);
                }
            }
            return dictionary;
        }

        /// <summary>
        /// 漢字の意味と例文を返す
        /// </summary>
        /// <returns>載っていなければ空（IsEmpty が true）</returns>
        public KanjiMeaning Get(string kanji)
        {
            if (kanji != null && meanings.TryGetValue(kanji, out var meaning)) return meaning;
            return default;
        }

        [Serializable]
        private class SetData
        {
            public string id;
            public string source;
            public EntryData[] entries;
        }

        [Serializable]
        private class EntryData
        {
            public string kanji;
            public string meaning;
            public string example;
        }
    }
}
