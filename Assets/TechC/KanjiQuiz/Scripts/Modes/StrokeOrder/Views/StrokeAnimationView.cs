using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 漢字を1画ずつ筆で書くアニメーション（お手本）
    /// 薄い字の上に、1画ずつ線が伸びていくように書く。書き終わったら少し待って最初から繰り返す
    /// </summary>
    public class StrokeAnimationView : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRendererPrefab;
        [Tooltip("枠の1辺の長さ（この Transform の座標で）。KanjiVG の文字の範囲は StrokePathParser の拡大率と同じ大きさ")]
        [SerializeField] private float frameSize = 3f;
        [Tooltip("書く線の太さ（枠の1辺に対する割合）")]
        [SerializeField] private float strokeWidthRatio = 0.075f;
        [Tooltip("枠の線の太さ（枠の1辺に対する割合）")]
        [SerializeField] private float frameWidthRatio = 0.02f;
        [SerializeField] private Color strokeColor = new Color(0.12f, 0.12f, 0.12f);
        [Tooltip("下に敷く薄い字の色")]
        [SerializeField] private Color guideColor = new Color(0f, 0f, 0f, 0.12f);
        [SerializeField] private Color frameColor = Color.black;
        [Tooltip("線の太さの変化（0 が書き始め、1 が筆先）。筆のように、入りと筆先を細くする")]
        [SerializeField] private AnimationCurve brushCurve = new AnimationCurve(
            new Keyframe(0f, 0.7f), new Keyframe(0.12f, 1f), new Keyframe(0.85f, 0.95f), new Keyframe(1f, 0.5f));
        [Tooltip("1画を書く速さ（枠の1辺を1秒で何回分書くか）")]
        [SerializeField] private float writeSpeed = 2.2f;
        [Tooltip("1画を書き終えてから次の画までの秒数")]
        [SerializeField] private float strokeInterval = 0.12f;
        [Tooltip("書き終えてから最初に戻るまでの秒数")]
        [SerializeField] private float loopInterval = 1.5f;
        [Tooltip("角と端を丸めるための頂点数")]
        [SerializeField] private int roundVertices = 8;
        [Tooltip("描画順。UI（Screen Space - Camera の Canvas）より手前に描くため、0 より大きくする")]
        [SerializeField] private int sortingOrder = 1;

        private readonly List<LineRenderer> writtenLines = new List<LineRenderer>();
        private Material sharedMaterial;

        private void Awake()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[StrokeAnimationView] Sprites/Default シェーダが見つかりません。");
                return;
            }
            sharedMaterial = new Material(shader);
        }

        private void OnDestroy()
        {
            if (sharedMaterial != null) Destroy(sharedMaterial);
        }

        /// <summary>
        /// 前の字を消してから、書くアニメーションをキャンセルされるまで繰り返す
        /// </summary>
        /// <param name="strokes">正しい書き順に並べた画のデータ</param>
        public async UniTask PlayAsync(IReadOnlyList<StrokeData> strokes, CancellationToken ct)
        {
            Clear();
            DrawFrame();
            foreach (var stroke in strokes)
            {
                var guide = CreateLine("Guide", guideColor, sortingOrder);
                SetPoints(guide, stroke.Points, stroke.Points.Count);
            }

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken))
            {
                var token = linkedCts.Token;
                while (true)
                {
                    foreach (var line in writtenLines) Destroy(line.gameObject);
                    writtenLines.Clear();

                    for (int i = 0; i < strokes.Count; i++)
                    {
                        await WriteStrokeAsync(strokes[i], i + 1, token);
                        await UniTask.Delay(System.TimeSpan.FromSeconds(strokeInterval), cancellationToken: token);
                    }
                    await UniTask.Delay(System.TimeSpan.FromSeconds(loopInterval), cancellationToken: token);
                }
            }
        }

        /// <summary>
        /// 書いた字と枠をすべて消す
        /// </summary>
        public void Clear()
        {
            writtenLines.Clear();
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 1画を、書き始めから筆先へ線が伸びていくように書く
        /// </summary>
        private async UniTask WriteStrokeAsync(StrokeData stroke, int number, CancellationToken ct)
        {
            var points = stroke.Points;
            var line = CreateLine($"Stroke_{number}", strokeColor, sortingOrder + 1);
            line.widthCurve = brushCurve;
            writtenLines.Add(line);
            if (points.Count < 2)
            {
                SetPoints(line, points, points.Count);
                return;
            }

            // 点と点の間隔がばらばらでも同じ速さで伸びるように、書き始めからの長さで進める
            var lengths = new float[points.Count];
            for (int p = 1; p < points.Count; p++)
            {
                lengths[p] = lengths[p - 1] + Vector3.Distance(points[p - 1], points[p]);
            }
            float total = lengths[points.Count - 1];
            float speed = writeSpeed * frameSize;

            float written = 0f;
            var buffer = new List<Vector3>(points.Count);
            while (written < total)
            {
                buffer.Clear();
                for (int p = 0; p < points.Count && lengths[p] <= written; p++)
                {
                    buffer.Add(points[p]);
                }
                // 次の点までの途中を足して、線がなめらかに伸びるようにする
                int next = buffer.Count;
                if (next > 0 && next < points.Count)
                {
                    float t = Mathf.InverseLerp(lengths[next - 1], lengths[next], written);
                    buffer.Add(Vector3.Lerp(points[next - 1], points[next], t));
                }
                SetPoints(line, buffer, buffer.Count);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                written += speed * Time.deltaTime;
            }
            SetPoints(line, points, points.Count);
        }

        private void DrawFrame()
        {
            var frame = CreateLine("Frame", frameColor, sortingOrder);
            float half = frameSize / 2f;
            frame.positionCount = 4;
            frame.SetPosition(0, new Vector3(-half, half, 0f));
            frame.SetPosition(1, new Vector3(half, half, 0f));
            frame.SetPosition(2, new Vector3(half, -half, 0f));
            frame.SetPosition(3, new Vector3(-half, -half, 0f));
            frame.loop = true;
            frame.numCornerVertices = 0;
            frame.numCapVertices = 0;
            frame.widthMultiplier = frameSize * frameWidthRatio * transform.lossyScale.x;
        }

        private LineRenderer CreateLine(string lineName, Color color, int order)
        {
            var line = Instantiate(lineRendererPrefab, transform);
            line.name = lineName;
            line.useWorldSpace = false;
            line.loop = false;
            line.numCornerVertices = roundVertices;
            line.numCapVertices = roundVertices;
            // 線の太さは Transform の拡大縮小の影響を受けないので、ワールド座標の太さに直す
            line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
            line.widthMultiplier = frameSize * strokeWidthRatio * transform.lossyScale.x;
            line.startColor = line.endColor = color;
            line.sortingOrder = order;
            if (sharedMaterial != null) line.material = sharedMaterial;
            return line;
        }

        private static void SetPoints(LineRenderer line, IReadOnlyList<Vector3> points, int count)
        {
            line.positionCount = count;
            for (int p = 0; p < count; p++)
            {
                line.SetPosition(p, points[p]);
            }
        }
    }
}
