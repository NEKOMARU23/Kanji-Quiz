using System.Collections.Generic;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字の部首を残し、部首以外の部分を別の漢字から借りた部品に入れ替えて、偽物の字を組み立てる
    /// 借りた部品は、部品を置く枠（問題データの box。なければ元の部分の外枠）に合わせて縮める・伸ばす
    /// </summary>
    public static class FakeKanjiComposer
    {
        /// <summary>
        /// 外枠の幅・高さの下限（StrokePathParser の座標で）
        /// 縦の1画だけの部分などは幅がほぼ 0 になり、借りた部品がつぶれてしまうため
        /// </summary>
        public const float MinExtent = 0.3f;

        /// <summary>
        /// SVG の画を、描画用の座標の列に変換する
        /// </summary>
        public static List<StrokeData> ToStrokeData(IEnumerable<KanjiSvgStroke> strokes)
        {
            var result = new List<StrokeData>();
            foreach (var stroke in strokes)
            {
                result.Add(StrokePathParser.Parse(stroke.PathData));
            }
            return result;
        }

        /// <summary>
        /// 偽物の字を組み立てる
        /// </summary>
        /// <param name="realStrokes">本物の漢字の画</param>
        /// <param name="radicalGroups">本物の漢字で残す部首のグループの id</param>
        /// <param name="donorStrokes">部品を借りる漢字の画</param>
        /// <param name="donorGroups">借りるグループの id。空なら donorStrokes をすべて使う</param>
        /// <param name="partBox">借りた部品を置く枠（KanjiVG の座標で x, y, 幅, 高さ。y は下向き）。null なら本物の部首以外の部分の外枠</param>
        /// <returns>組み立てた字の画（部首の画のあとに借りた部品の画）。部首・残りの部分・借りる部品のどれかがなければ null</returns>
        public static List<StrokeData> Compose(
            IReadOnlyList<KanjiSvgStroke> realStrokes,
            ICollection<string> radicalGroups,
            IReadOnlyList<KanjiSvgStroke> donorStrokes,
            ICollection<string> donorGroups,
            IReadOnlyList<float> partBox = null)
        {
            var radical = new List<KanjiSvgStroke>();
            var rest = new List<KanjiSvgStroke>();
            foreach (var stroke in realStrokes)
            {
                (stroke.IsInAnyGroup(radicalGroups) ? radical : rest).Add(stroke);
            }

            var part = new List<KanjiSvgStroke>();
            foreach (var stroke in donorStrokes)
            {
                if (donorGroups.Count == 0 || stroke.IsInAnyGroup(donorGroups)) part.Add(stroke);
            }

            if (radical.Count == 0 || rest.Count == 0 || part.Count == 0) return null;

            var result = ToStrokeData(radical);
            var partStrokes = ToStrokeData(part);
            var target = partBox != null && partBox.Count == 4 ? FromKanjiVgBox(partBox) : GetBounds(ToStrokeData(rest));
            var source = GetBounds(partStrokes);
            foreach (var stroke in partStrokes)
            {
                result.Add(Fit(stroke, source, target));
            }
            return result;
        }

        /// <summary>
        /// 画をすべて囲む外枠。幅・高さが MinExtent より小さければ、中心を変えずに MinExtent まで広げる
        /// </summary>
        public static Rect GetBounds(IReadOnlyList<StrokeData> strokes)
        {
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (var stroke in strokes)
            {
                foreach (var p in stroke.Points)
                {
                    xMin = Mathf.Min(xMin, p.x);
                    yMin = Mathf.Min(yMin, p.y);
                    xMax = Mathf.Max(xMax, p.x);
                    yMax = Mathf.Max(yMax, p.y);
                }
            }
            if (xMin > xMax) return Rect.zero;

            var center = new Vector2((xMin + xMax) / 2f, (yMin + yMax) / 2f);
            var size = new Vector2(Mathf.Max(xMax - xMin, MinExtent), Mathf.Max(yMax - yMin, MinExtent));
            return new Rect(center - size / 2f, size);
        }

        /// <summary>
        /// KanjiVG の座標（y は下向き）の枠 (x, y, 幅, 高さ) を、StrokePathParser の座標（中心が原点、y は上向き）の枠にする
        /// </summary>
        public static Rect FromKanjiVgBox(IReadOnlyList<float> box, float scale = 3f)
        {
            var topLeft = StrokePathParser.Normalize(new Vector2(box[0], box[1]), scale);
            var bottomRight = StrokePathParser.Normalize(new Vector2(box[0] + box[2], box[1] + box[3]), scale);
            return Rect.MinMaxRect(topLeft.x, bottomRight.y, bottomRight.x, topLeft.y);
        }

        /// <summary>
        /// 画を source の外枠から target の外枠へ、縦横それぞれの比率で写す
        /// </summary>
        private static StrokeData Fit(StrokeData stroke, Rect source, Rect target)
        {
            var points = new Vector3[stroke.Points.Count];
            for (int i = 0; i < points.Length; i++)
            {
                var p = stroke.Points[i];
                float x = target.xMin + (p.x - source.xMin) / source.width * target.width;
                float y = target.yMin + (p.y - source.yMin) / source.height * target.height;
                points[i] = new Vector3(x, y, p.z);
            }
            return new StrokeData(points);
        }
    }
}
