using System.Collections.Generic;
using System.Text;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 読みをつけた文（例：{夏休|なつやす}みに海へ{行|い}った。）を、漢字のまま、またはひらがなにして出す
    /// 読みをつけていない漢字（出題した字など）は、どちらでも漢字のまま出す
    /// </summary>
    public static class RubyText
    {
        private const char Open = '{';
        private const char Separator = '|';
        private const char Close = '}';

        /// <summary>
        /// 読みをつけた文を、表示する文に直す
        /// </summary>
        /// <param name="marked">読みをつけた文</param>
        /// <param name="useHiragana">true なら、読みをつけた漢字をひらがなにする</param>
        public static string Render(string marked, bool useHiragana)
        {
            if (string.IsNullOrEmpty(marked)) return string.Empty;

            var builder = new StringBuilder(marked.Length);
            int i = 0;
            while (i < marked.Length)
            {
                if (marked[i] == Open)
                {
                    int separator = marked.IndexOf(Separator, i + 1);
                    int close = marked.IndexOf(Close, i + 1);
                    if (separator > i && close > separator)
                    {
                        builder.Append(useHiragana
                            ? marked.Substring(separator + 1, close - separator - 1)
                            : marked.Substring(i + 1, separator - i - 1));
                        i = close + 1;
                        continue;
                    }
                }
                builder.Append(marked[i]);
                i++;
            }
            return builder.ToString();
        }

        /// <summary>
        /// 読みをつけていない漢字を探す（データを確かめるのに使う）
        /// </summary>
        /// <param name="allowed">読みがなくてよい漢字（出題した字など）</param>
        /// <returns>読みのない漢字（並んだ順、重なりなし）</returns>
        public static IReadOnlyList<string> FindUnmarkedKanji(string marked, string allowed)
        {
            var found = new List<string>();
            if (string.IsNullOrEmpty(marked)) return found;

            string plain = RemoveMarkedParts(marked);
            foreach (var character in KanjiSet.SplitCharacters(plain))
            {
                if (!IsKanji(character)) continue;
                if (allowed != null && allowed.Contains(character)) continue;
                if (!found.Contains(character)) found.Add(character);
            }
            return found;
        }

        /// <summary>漢字（CJK 統合漢字とその拡張、々）かどうか</summary>
        public static bool IsKanji(string character)
        {
            if (string.IsNullOrEmpty(character)) return false;
            int code = char.ConvertToUtf32(character, 0);
            return (code >= 0x4E00 && code <= 0x9FFF)
                || (code >= 0x3400 && code <= 0x4DBF)
                || (code >= 0x20000 && code <= 0x2FA1F)
                || code == 0x3005;
        }

        /// <summary>読みをつけた部分を取り除いた文（読みのない漢字を探すのに使う）</summary>
        private static string RemoveMarkedParts(string marked)
        {
            var builder = new StringBuilder(marked.Length);
            int i = 0;
            while (i < marked.Length)
            {
                if (marked[i] == Open)
                {
                    int close = marked.IndexOf(Close, i + 1);
                    if (close > i)
                    {
                        i = close + 1;
                        continue;
                    }
                }
                builder.Append(marked[i]);
                i++;
            }
            return builder.ToString();
        }
    }
}
