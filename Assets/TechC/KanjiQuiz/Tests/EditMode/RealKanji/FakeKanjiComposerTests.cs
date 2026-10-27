using System.Collections.Generic;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.RealKanji;

namespace TechC.KanjiQuiz.Tests
{
    public class FakeKanjiComposerTests
    {
        private const float Tolerance = 0.001f;

        private IReadOnlyList<KanjiSvgStroke> real;

        [SetUp]
        public void SetUp()
        {
            // 左に部首（縦の1画）、右に残りの部分（横の2画、x 50〜90・y 20〜80）
            real = new FakeKanjiSvgSource()
                .Add("海", "radical", 10, 10, 10, 90)
                .Add("海", "rest", 50, 20, 90, 20)
                .Add("海", "rest", 50, 80, 90, 80)
                .LoadStrokesAsync("海", default).GetAwaiter().GetResult();
        }

        private static IReadOnlyList<KanjiSvgStroke> Donor()
        {
            // 部品を借りる漢字。左の部分（x 0〜20）と、右の部分（十字、x 10〜100・y 0〜100）
            return new FakeKanjiSvgSource()
                .Add("持", "left", 0, 10, 20, 90)
                .Add("持", "right", 10, 50, 100, 50)
                .Add("持", "right", 55, 0, 55, 100)
                .LoadStrokesAsync("持", default).GetAwaiter().GetResult();
        }

        [Test]
        public void 部首を残し借りた部品を元の部分の外枠に合わせる()
        {
            var strokes = FakeKanjiComposer.Compose(real, new[] { "radical" }, Donor(), new[] { "right" });

            Assert.AreEqual(3, strokes.Count);
            // 1画目は本物の部首のまま
            var radical = FakeKanjiComposer.ToStrokeData(new[] { real[0] });
            CollectionAssert.AreEqual(radical[0].Points, strokes[0].Points);

            // 借りた部品（2画）は、本物の残りの部分と同じ外枠に収まる
            var expected = FakeKanjiComposer.GetBounds(FakeKanjiComposer.ToStrokeData(new[] { real[1], real[2] }));
            var actual = FakeKanjiComposer.GetBounds(strokes.GetRange(1, 2));
            Assert.AreEqual(expected.xMin, actual.xMin, Tolerance);
            Assert.AreEqual(expected.xMax, actual.xMax, Tolerance);
            Assert.AreEqual(expected.yMin, actual.yMin, Tolerance);
            Assert.AreEqual(expected.yMax, actual.yMax, Tolerance);
        }

        [Test]
        public void 借りるグループが空なら部品を借りる漢字の画をすべて使う()
        {
            var strokes = FakeKanjiComposer.Compose(real, new[] { "radical" }, Donor(), new string[0]);

            Assert.AreEqual(4, strokes.Count);
        }

        [Test]
        public void 部首か残りの部分か借りる部品がなければ組み立てない()
        {
            Assert.IsNull(FakeKanjiComposer.Compose(real, new[] { "none" }, Donor(), new[] { "right" }), "部首がない");
            Assert.IsNull(FakeKanjiComposer.Compose(real, new[] { "radical", "rest" }, Donor(), new[] { "right" }), "残りの部分がない");
            Assert.IsNull(FakeKanjiComposer.Compose(real, new[] { "radical" }, Donor(), new[] { "none" }), "借りる部品がない");
        }

        [Test]
        public void 幅がほぼない部分の外枠は最小の大きさまで広げる()
        {
            // 縦の1画だけ（幅 0）
            var bounds = FakeKanjiComposer.GetBounds(FakeKanjiComposer.ToStrokeData(new[] { real[0] }));

            Assert.AreEqual(FakeKanjiComposer.MinExtent, bounds.width, Tolerance);
            Assert.Greater(bounds.height, FakeKanjiComposer.MinExtent);
        }
    }
}
