using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字の画を UI の線として描く（黒板のチョークの字など）
    /// LineRenderer と違って UI の描画順に従うので、上に重ねた UI（黒板消しの跡など）で隠せ、マスクで切り取れる
    /// 何画目まで書いたかを Progress で決め、途中の画は書き始めから途中まで描く
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ChalkStrokeGraphic : MaskableGraphic
    {
        [Tooltip("線の太さ（px）")]
        [Min(0.5f)] [SerializeField] private float lineWidth = 12f;
        [Tooltip("線の端と曲がり角を丸めるための頂点数")]
        [Range(3, 16)] [SerializeField] private int roundVertices = 8;

        /// <summary>この角度（cos）より大きく向きが変わる点を、曲がり角として丸める（約 20 度）</summary>
        private const float SharpCornerCos = 0.94f;

        private readonly List<Glyph> glyphs = new List<Glyph>();
        private float progress;

        /// <summary>画の総数</summary>
        public int StrokeCount { get; private set; }

        /// <summary>
        /// 何画目まで書いたか（0〜StrokeCount）。小数の部分は、次の画を書き始めからどこまで書いたか（長さの割合）
        /// </summary>
        public float Progress
        {
            get => progress;
            set
            {
                float clamped = Mathf.Clamp(value, 0f, StrokeCount);
                if (Mathf.Approximately(clamped, progress)) return;
                progress = clamped;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// 描く字を設定する。字ごとに画のデータ（StrokePathParser で、字の大きさを px にして作ったもの）と、字の中心の位置（この RectTransform の中心から px）を渡す
        /// </summary>
        public void SetGlyphs(IReadOnlyList<IReadOnlyList<StrokeData>> strokesPerGlyph, IReadOnlyList<Vector2> centers)
        {
            glyphs.Clear();
            StrokeCount = 0;
            for (int g = 0; g < strokesPerGlyph.Count; g++)
            {
                var glyph = new Glyph(strokesPerGlyph[g], g < centers.Count ? centers[g] : Vector2.zero);
                glyphs.Add(glyph);
                StrokeCount += glyph.Strokes.Count;
            }
            progress = 0f;
            SetVerticesDirty();
        }

        /// <summary>すべての画を書き終えた状態にする</summary>
        public void Complete() => Progress = StrokeCount;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int index = 0;
            foreach (var glyph in glyphs)
            {
                foreach (var stroke in glyph.Strokes)
                {
                    float amount = Mathf.Clamp01(progress - index);
                    index++;
                    if (amount <= 0f) return;
                    AddStroke(vh, stroke, glyph.Center, amount);
                }
            }
        }

        private void AddStroke(VertexHelper vh, PreparedStroke stroke, Vector2 offset, float amount)
        {
            var points = stroke.Points;
            if (points.Count == 0) return;

            float limit = stroke.Length * amount;
            float half = lineWidth / 2f;
            Vector2 previous = points[0] + offset;
            Vector2 previousDirection = Vector2.zero;
            AddDisc(vh, previous, half);
            for (int i = 1; i < points.Count; i++)
            {
                if (stroke.Lengths[i - 1] >= limit) break;

                Vector2 next = points[i] + offset;
                bool isLast = i == points.Count - 1;
                if (stroke.Lengths[i] > limit)
                {
                    // 書いている途中の点。前の点から次の点までの途中で止める
                    float t = Mathf.InverseLerp(stroke.Lengths[i - 1], stroke.Lengths[i], limit);
                    next = Vector2.Lerp(previous, next, t);
                    isLast = true;
                }
                // 曲がり角（向きが大きく変わる点）だけ丸める。なめらかな曲線の点ごとに円を足すと頂点が多くなりすぎる
                Vector2 direction = (next - previous).normalized;
                if (previousDirection != Vector2.zero && Vector2.Dot(previousDirection, direction) < SharpCornerCos)
                {
                    AddDisc(vh, previous, half);
                }
                AddSegment(vh, previous, next, half);
                if (isLast) AddDisc(vh, next, half);
                previousDirection = direction;
                previous = next;
            }
        }

        private void AddSegment(VertexHelper vh, Vector2 from, Vector2 to, float half)
        {
            Vector2 direction = to - from;
            if (direction.sqrMagnitude < 0.0001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * half;

            int start = vh.currentVertCount;
            vh.AddVert(from - normal, color, Vector4.zero);
            vh.AddVert(from + normal, color, Vector4.zero);
            vh.AddVert(to + normal, color, Vector4.zero);
            vh.AddVert(to - normal, color, Vector4.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        /// <summary>線の端と曲がり角を丸めるための円</summary>
        private void AddDisc(VertexHelper vh, Vector2 center, float radius)
        {
            int segments = roundVertices * 2;
            int start = vh.currentVertCount;
            vh.AddVert(center, color, Vector4.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector4.zero);
            }
            for (int i = 1; i <= segments; i++)
            {
                vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        /// <summary>1字分の画と、その字の中心の位置</summary>
        private sealed class Glyph
        {
            public readonly List<PreparedStroke> Strokes = new List<PreparedStroke>();
            public readonly Vector2 Center;

            public Glyph(IReadOnlyList<StrokeData> strokes, Vector2 center)
            {
                Center = center;
                foreach (var stroke in strokes) Strokes.Add(new PreparedStroke(stroke));
            }
        }

        /// <summary>1画分の点と、書き始めからの長さ（途中まで描くのに使う）</summary>
        private sealed class PreparedStroke
        {
            public readonly List<Vector2> Points = new List<Vector2>();
            public readonly List<float> Lengths = new List<float>();
            public float Length { get; }

            public PreparedStroke(StrokeData stroke)
            {
                float length = 0f;
                for (int i = 0; i < stroke.Points.Count; i++)
                {
                    Vector2 point = stroke.Points[i];
                    if (i > 0) length += Vector2.Distance(Points[i - 1], point);
                    Points.Add(point);
                    Lengths.Add(length);
                }
                Length = length;
            }
        }
    }
}
