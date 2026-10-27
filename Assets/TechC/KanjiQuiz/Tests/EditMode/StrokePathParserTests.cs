using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class StrokePathParserTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void 中心は原点に変換される()
        {
            var p = StrokePathParser.Normalize(new Vector2(54.5f, 54.5f), 3f);

            Assert.AreEqual(0f, p.x, Tolerance);
            Assert.AreEqual(0f, p.y, Tolerance);
        }

        [Test]
        public void 上がプラスのY方向になる()
        {
            var top = StrokePathParser.Normalize(new Vector2(54.5f, 0f), 1f);

            Assert.AreEqual(0.5f, top.y, Tolerance);
        }

        [Test]
        public void 直線は始点と終点の2点になる()
        {
            var stroke = StrokePathParser.Parse("M0,0L109,109", 1f);

            Assert.AreEqual(2, stroke.Points.Count);
            Assert.AreEqual(-0.5f, stroke.Points[0].x, Tolerance);
            Assert.AreEqual(0.5f, stroke.Points[0].y, Tolerance);
            Assert.AreEqual(0.5f, stroke.Points[1].x, Tolerance);
            Assert.AreEqual(-0.5f, stroke.Points[1].y, Tolerance);
        }

        [Test]
        public void 相対座標の曲線は分割された点の列になり終点が正しい()
        {
            var stroke = StrokePathParser.Parse("M54.5,54.5c0,0,0,0,10.9,0", 1f);

            Assert.AreEqual(StrokePathParser.BezierSegments + 1, stroke.Points.Count);
            var last = stroke.Points[stroke.Points.Count - 1];
            Assert.AreEqual(0.1f, last.x, Tolerance);
            Assert.AreEqual(0f, last.y, Tolerance);
        }

        [Test]
        public void 区切りなしの負の数も読み取れる()
        {
            var stroke = StrokePathParser.Parse("M54.5,54.5l10.9-10.9", 1f);

            var last = stroke.Points[stroke.Points.Count - 1];
            Assert.AreEqual(0.1f, last.x, Tolerance);
            Assert.AreEqual(0.1f, last.y, Tolerance);
        }
    }
}
