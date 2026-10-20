using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// KanjiVG の SVG テキストから、1画ごとのパスデータを書き順の順番で取り出す
    /// </summary>
    public static class KanjiSvgParser
    {
        private static readonly Regex PathTagRegex = new Regex(@"<path\b[^>]*>", RegexOptions.Compiled);
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
            if (svgText == null) throw new ArgumentNullException(nameof(svgText));

            var strokes = new List<string>();
            foreach (Match tag in PathTagRegex.Matches(svgText))
            {
                var id = IdRegex.Match(tag.Value);
                var d = DRegex.Match(tag.Value);
                if (!id.Success || !d.Success) continue;
                if (!StrokeIdRegex.IsMatch(id.Groups[1].Value)) continue;

                strokes.Add(d.Groups[1].Value);
            }
            return strokes;
        }
    }
}
