using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiSvgParserTests
    {
        // KanjiVG の SVG を短くしたもの
        private const string SampleSvg = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg xmlns=""http://www.w3.org/2000/svg"" width=""109"" height=""109"" viewBox=""0 0 109 109"">
<g id=""kvg:StrokePaths_04e8c"" style=""fill:none;stroke:#000000;"">
<g id=""kvg:04e8c"" kvg:element=""二"">
	<path id=""kvg:04e8c-s1"" kvg:type=""㇐"" d=""M25.5,26.5c2,0.5,5,0.5,8,0.25""/>
	<path id=""kvg:04e8c-s2"" kvg:type=""㇐"" d=""M12.5,82.5c3,0.75,6,0.5,9,0.25""/>
</g>
</g>
<g id=""kvg:StrokeNumbers_04e8c"">
	<text transform=""matrix(1 0 0 1 18.5 25.5)"">1</text>
</g>
</svg>";

        [Test]
        public void 画のパスデータを書き順の順番で取り出す()
        {
            var strokes = KanjiSvgParser.ExtractStrokePaths(SampleSvg);

            Assert.AreEqual(2, strokes.Count);
            Assert.AreEqual("M25.5,26.5c2,0.5,5,0.5,8,0.25", strokes[0]);
            Assert.AreEqual("M12.5,82.5c3,0.75,6,0.5,9,0.25", strokes[1]);
        }

        [Test]
        public void 画のpath要素がないときは空になる()
        {
            Assert.IsEmpty(KanjiSvgParser.ExtractStrokePaths("<svg></svg>"));
        }

        [TestCase("漢", "06f22.svg")]
        [TestCase("一", "04e00.svg")]
        [TestCase("あ", "03042.svg")]
        public void 漢字からファイル名を作る(string kanji, string expected)
        {
            Assert.AreEqual(expected, KanjiSvgParser.ToFileName(kanji));
        }
    }
}
