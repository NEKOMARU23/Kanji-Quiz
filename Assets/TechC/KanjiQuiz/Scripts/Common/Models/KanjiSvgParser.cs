using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// KanjiVG の SVG テキストから、1画ごとのパスデータを書き順の順番で取り出す
    /// </summary>
    public static class KanjiSvgParser
    {
        private static readonly Regex TagRegex = new Regex(@"<path\b[^>]*>|<g\b[^>]*>|</g\s*>", RegexOptions.Compiled);
        private static readonly Regex IdRegex = new Regex(@"\bid=""([^""]*)""", RegexOptions.Compiled);
        private static readonly Regex DRegex = new Regex(@"\sd=""([^""]*)""", RegexOptions.Compiled);
        private static readonly Regex StrokeIdRegex = new Regex(@"-s\d+$", RegexOptions.Compiled);

        /// <summary>
        /// 漢字から KanjiVG のファイル名を作る
        /// コードポイントを5桁の小文字16進数にする（例：「漢」→ 06f22.svg）
        /// </summary>
        public static string ToFileName(string kanji)
        {
            if (string.IsNullOrEmpty(kanji)) throw new ArgumentException("漢字が空です。", nameof(kanji));

            int codePoint = char.ConvertToUtf32(kanji, 0);
            return $"{codePoint:x5}.svg";
        }

        /// <summary>
        /// SVG テキストから、1画ごとのパスデータを出現順（＝書き順）に取り出す
        /// id が「-s数字」で終わる path 要素を1画とみなす
        /// </summary>
        public static List<string> ExtractStrokePaths(string svgText)
        {
            var strokes = ExtractStrokes(svgText);
            var paths = new List<string>(strokes.Count);
            foreach (var stroke in strokes)
            {
                paths.Add(stroke.PathData);
            }
            return paths;
        }

        /// <summary>
        /// SVG テキストから、1画ごとのパスデータと、その画が入っている部品のグループの id を出現順（＝書き順）に取り出す
        /// 部品のグループは KanjiVG の g 要素（例：「海」の「氵」は kvg:06d77-g1）
        /// </summary>
        public static List<KanjiSvgStroke> ExtractStrokes(string svgText)
        {
            if (svgText == null) throw new ArgumentNullException(nameof(svgText));

            var strokes = new List<KanjiSvgStroke>();
            var groupIds = new List<string>();
            foreach (Match tag in TagRegex.Matches(svgText))
            {
                string value = tag.Value;
                if (value.StartsWith("</g", StringComparison.Ordinal))
                {
                    if (groupIds.Count > 0) groupIds.RemoveAt(groupIds.Count - 1);
                    continue;
                }

                var id = IdRegex.Match(value);
                if (value.StartsWith("<g", StringComparison.Ordinal))
                {
                    // 中身のない <g/> は閉じタグがないので積まない
                    if (!value.EndsWith("/>", StringComparison.Ordinal))
                    {
                        groupIds.Add(id.Success ? id.Groups[1].Value : string.Empty);
                    }
                    continue;
                }

                var d = DRegex.Match(value);
                if (!id.Success || !d.Success) continue;
                if (!StrokeIdRegex.IsMatch(id.Groups[1].Value)) continue;

                strokes.Add(new KanjiSvgStroke(d.Groups[1].Value, groupIds.ToArray()));
            }
            return strokes;
        }
    }
}
