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

        // 「海」の SVG を短くしたもの。部首の「氵」と、右の「毎」（その中に部品がある）に分かれている
        private const string PartsSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"">
<g id=""kvg:StrokePaths_06d77"">
<g id=""kvg:06d77"" kvg:element=""海"">
	<g id=""kvg:06d77-g1"" kvg:element=""氵"" kvg:position=""left"" kvg:radical=""general"">
		<path id=""kvg:06d77-s1"" d=""M1,1""/>
	</g>
	<g id=""kvg:06d77-g2"" kvg:element=""毎"" kvg:position=""right"">
		<g id=""kvg:06d77-g3"" kvg:position=""top""/>
		<g id=""kvg:06d77-g4"" kvg:element=""毋"">
			<path id=""kvg:06d77-s2"" d=""M2,2""/>
		</g>
		<path id=""kvg:06d77-s3"" d=""M3,3""/>
	</g>
</g>
</g>
</svg>";

        [Test]
        public void 画ごとに入っている部品のグループを取り出す()
        {
            var strokes = KanjiSvgParser.ExtractStrokes(PartsSvg);

            Assert.AreEqual(3, strokes.Count);
            CollectionAssert.AreEqual(new[] { "kvg:StrokePaths_06d77", "kvg:06d77", "kvg:06d77-g1" }, strokes[0].GroupIds);
            CollectionAssert.AreEqual(new[] { "kvg:StrokePaths_06d77", "kvg:06d77", "kvg:06d77-g2", "kvg:06d77-g4" }, strokes[1].GroupIds);
            // 中身のない <g/> は閉じタグがないので、そのあとの画のグループに混ざらない
            CollectionAssert.AreEqual(new[] { "kvg:StrokePaths_06d77", "kvg:06d77", "kvg:06d77-g2" }, strokes[2].GroupIds);
            Assert.AreEqual("M3,3", strokes[2].PathData);
        }

        [Test]
        public void 指定したグループに入っている画かどうかを調べられる()
        {
            var strokes = KanjiSvgParser.ExtractStrokes(PartsSvg);
            var radical = new[] { "kvg:06d77-g1" };

            Assert.IsTrue(strokes[0].IsInAnyGroup(radical));
            Assert.IsFalse(strokes[1].IsInAnyGroup(radical));
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
