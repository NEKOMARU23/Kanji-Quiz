using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順を1画ずつ積み上げた図を、横一列に並べて描く
    /// 例：3画の漢字なら「1画目まで」「2画目まで」「3画目まで」の3つの図を描く
    /// </summary>
    public class CumulativeStrokeRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRendererPrefab;
        [SerializeField] private float strokeWidth = 0.15f;
        [SerializeField] private Color strokeColor = Color.black;
        [Tooltip("図と図の横の間隔（この Transform の座標で）")]
        [SerializeField] private float horizontalSpacing = 2.5f;
        [Tooltip("図を並べる幅の上限（この Transform の座標で）。画数が多くてはみ出すときは図を縮める。0 なら制限しない")]
        [SerializeField] private float maxRowWidth = 0f;
        [Tooltip("角と端を丸めるための頂点数")]
        [SerializeField] private int roundVertices = 10;
        [Tooltip("描画順。UI（Screen Space - Camera の Canvas）より手前に描くため、0 より大きくする。画は枠より1つ手前に描く")]
        [SerializeField] private int sortingOrder = 1;

        [Header("--- 枠（1段階ごとに囲む四角） ---")]
        [SerializeField] private bool drawFrame = true;
        [Tooltip("枠の1辺の長さ（この Transform の座標で）。KanjiVG の文字の範囲は StrokePathParser の拡大率と同じ大きさ")]
        [SerializeField] private float frameSize = 3f;
        [Tooltip("枠の線の太さ（ワールド座標）。図を縮めても変えない")]
        [SerializeField] private float frameWidth = 0.4f;
        [SerializeField] private Color frameColor = Color.black;

        private Material sharedMaterial;

        private void Awake()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[CumulativeStrokeRenderer] Sprites/Default シェーダが見つかりません。");
                return;
            }
            sharedMaterial = new Material(shader);
        }

        private void OnDestroy()
        {
            if (sharedMaterial != null) Destroy(sharedMaterial);
        }

        /// <summary>
        /// 前の図を消してから、1フレームに1段階ずつ描く
        /// キャンセルされると、その時点で描くのをやめる
        /// </summary>
        /// <param name="strokes">描く順番に並べた画のデータ</param>
        public async UniTask DrawAsync(IReadOnlyList<StrokeData> strokes, CancellationToken ct)
        {
            Clear();

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, destroyCancellationToken))
            {
                var token = linkedCts.Token;
                float stageScale = CalculateStageScale(strokes.Count);
                for (int stage = 1; stage <= strokes.Count; stage++)
                {
                    token.ThrowIfCancellationRequested();
                    DrawStage(strokes, stage, stageScale);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
        }

        /// <summary>
        /// 描いた図をすべて消す
        /// </summary>
        public void Clear()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 図を並べた幅が上限を超えるときの縮小率を求める
        /// </summary>
        private float CalculateStageScale(int stageCount)
        {
            float rowWidth = stageCount * horizontalSpacing;
            if (maxRowWidth <= 0f || rowWidth <= maxRowWidth) return 1f;
            return maxRowWidth / rowWidth;
        }

        private void DrawStage(IReadOnlyList<StrokeData> strokes, int stage, float stageScale)
        {
            // 1つ目の図の中心を、この Transform の原点から間隔の半分だけ右に置く
            float x = (stage - 0.5f) * horizontalSpacing * stageScale;

            var stageObject = new GameObject($"Stage_{stage}");
            stageObject.transform.SetParent(transform, false);
            stageObject.transform.localPosition = new Vector3(x, 0f, 0f);
            stageObject.transform.localScale = Vector3.one * stageScale;

            if (drawFrame)
            {
                DrawFrame(stageObject.transform);
            }

            for (int i = 0; i < stage; i++)
            {
                DrawStroke(stageObject.transform, strokes[i], i + 1, stageScale);
            }
        }

        /// <summary>
        /// 1段階の図を囲む四角を描く
        /// </summary>
        private void DrawFrame(Transform parent)
        {
            var frame = Instantiate(lineRendererPrefab, parent);
            frame.name = "Frame";

            float half = frameSize / 2f;
            frame.positionCount = 4;
            frame.SetPosition(0, new Vector3(-half, half, 0f));
            frame.SetPosition(1, new Vector3(half, half, 0f));
            frame.SetPosition(2, new Vector3(half, -half, 0f));
            frame.SetPosition(3, new Vector3(-half, -half, 0f));
            frame.loop = true;

            frame.useWorldSpace = false;
            frame.numCornerVertices = 0;
            frame.numCapVertices = 0;
            // 枠の線はもともと細いので、図を縮めても細くしない（細くしすぎると線が途切れて見える）
            frame.startWidth = frame.endWidth = frameWidth;
            frame.startColor = frame.endColor = frameColor;
            frame.sortingOrder = sortingOrder;
            if (sharedMaterial != null) frame.material = sharedMaterial;
        }

        private void DrawStroke(Transform parent, StrokeData stroke, int number, float stageScale)
        {
            var lineRenderer = Instantiate(lineRendererPrefab, parent);
            lineRenderer.name = $"Stroke_{number}";

            var points = stroke.Points;
            lineRenderer.positionCount = points.Count;
            for (int p = 0; p < points.Count; p++)
            {
                lineRenderer.SetPosition(p, points[p]);
            }

            lineRenderer.useWorldSpace = false;
            lineRenderer.numCornerVertices = roundVertices;
            lineRenderer.numCapVertices = roundVertices;
            // 線の太さは Transform の拡大縮小の影響を受けないので、図を縮めた分だけ細くする
            lineRenderer.startWidth = lineRenderer.endWidth = strokeWidth * stageScale;
            lineRenderer.startColor = lineRenderer.endColor = strokeColor;
            lineRenderer.sortingOrder = sortingOrder + 1;
            if (sharedMaterial != null) lineRenderer.material = sharedMaterial;
        }
    }
}
