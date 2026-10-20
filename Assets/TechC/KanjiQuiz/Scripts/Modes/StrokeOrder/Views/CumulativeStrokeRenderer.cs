using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        [Tooltip("図と図の横の間隔")]
        [SerializeField] private float horizontalSpacing = 2.5f;
        [Tooltip("角と端を丸めるための頂点数")]
        [SerializeField] private int roundVertices = 10;

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
                for (int stage = 1; stage <= strokes.Count; stage++)
                {
                    token.ThrowIfCancellationRequested();
                    DrawStage(strokes, stage);
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

        private void DrawStage(IReadOnlyList<StrokeData> strokes, int stage)
        {
            var stageObject = new GameObject($"Stage_{stage}");
            stageObject.transform.SetParent(transform, false);
            stageObject.transform.localPosition = new Vector3(stage * horizontalSpacing, 0f, 0f);

            for (int i = 0; i < stage; i++)
            {
                DrawStroke(stageObject.transform, strokes[i], i + 1);
            }
        }

        private void DrawStroke(Transform parent, StrokeData stroke, int number)
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
            lineRenderer.startWidth = lineRenderer.endWidth = strokeWidth;
            lineRenderer.startColor = lineRenderer.endColor = strokeColor;
            if (sharedMaterial != null) lineRenderer.material = sharedMaterial;
        }
    }
}
