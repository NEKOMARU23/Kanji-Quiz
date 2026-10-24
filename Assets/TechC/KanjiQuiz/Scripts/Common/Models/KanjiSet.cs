using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 出題範囲の漢字の集まり（例：小学漢字）
    /// JSON（Data/KanjiSets）から読み込む
    /// </summary>
    public sealed class KanjiSet
    {
        /// <summary>識別子（例：Elementary）</summary>
        public string Id { get; }

        /// <summary>表示名（例：小学漢字）</summary>
        public string Title { get; }

        /// <summary>漢字の一覧。1要素が1文字</summary>
        public IReadOnlyList<string> Kanji { get; }

        public KanjiSet(string id, string title, IReadOnlyList<string> kanji)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Kanji = kanji ?? throw new ArgumentNullException(nameof(kanji));
        }

        /// <summary>
        /// JSON から漢字セットを作る
        /// JSON の kanji は漢字をつなげた1つの文字列
        /// </summary>
        public static KanjiSet FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("JSON が空です。", nameof(json));

            var data = JsonUtility.FromJson<KanjiSetData>(json);
            if (data == null || string.IsNullOrEmpty(data.id))
            {
                throw new FormatException("漢字セットの JSON の形式が正しくありません。");
            }
            return new KanjiSet(data.id, data.title ?? string.Empty, SplitCharacters(data.kanji ?? string.Empty));
        }

        /// <summary>
        /// 文字列を1文字ずつに分ける
        /// 「𠮟」のようなサロゲートペアの文字も1文字として扱う
        /// </summary>
        public static List<string> SplitCharacters(string text)
        {
            var characters = new List<string>(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsWhiteSpace(text[i])) continue;

                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    characters.Add(text.Substring(i, 2));
                    i++;
                }
                else
                {
                    characters.Add(text[i].ToString());
                }
            }
            return characters;
        }

        [Serializable]
        private class KanjiSetData
        {
            public string id;
            public string title;
            public string source;
            public string kanji;
        }
    }
}
