using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字帳を PlayerPrefs に保存する（キーは KanjiBook_モード）
    /// 値は、集めたもの（漢字1字か、四字熟語）を1つずつ改行で区切った文字列（最後にも改行をつける）
    /// 2026-11-03 より前の形式（漢字をつなげただけの文字列）も読める
    /// </summary>
    public sealed class PlayerPrefsKanjiBookRepository : IKanjiBookRepository
    {
        private const string KeyPrefix = "KanjiBook_";
        private const char Separator = '\n';

        public IReadOnlyCollection<string> Load(string modeId)
        {
            return Deserialize(PlayerPrefs.GetString(KeyPrefix + modeId, string.Empty));
        }

        public void Save(string modeId, IEnumerable<string> kanji)
        {
            PlayerPrefs.SetString(KeyPrefix + modeId, Serialize(kanji));
            PlayerPrefs.Save();
        }

        public void Delete(string modeId)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + modeId);
            PlayerPrefs.Save();
        }

        /// <summary>集めたものを、保存する文字列にする（1つずつ改行で区切り、最後にも改行をつける）</summary>
        public static string Serialize(IEnumerable<string> items)
        {
            var builder = new StringBuilder();
            foreach (var item in items ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(item)) continue;
                builder.Append(item).Append(Separator);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 保存した文字列から、集めたものを戻す。改行がなければ、前の形式（漢字をつなげただけ）として1字ずつに分ける
        /// </summary>
        public static List<string> Deserialize(string value)
        {
            if (string.IsNullOrEmpty(value)) return new List<string>();
            if (value.IndexOf(Separator) < 0) return KanjiSet.SplitCharacters(value);

            var items = new List<string>();
            foreach (var item in value.Split(Separator))
            {
                if (!string.IsNullOrEmpty(item)) items.Add(item);
            }
            return items;
        }
    }
}
