using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字1字を、画の線（LineRenderer）でまとめて描く
    /// フォントにない字（本物の漢字さがしの偽物）も描けるように、KanjiVG の画のデータから描く
    /// 字の大きさは StrokePathParser の座標（1辺 3）で、この Transform の拡大率で調整する
    /// </summary>
    public class KanjiGlyphRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRendererPrefab;
        [Tooltip("線の太さ（この Transform の座標で）")]
        [SerializeField] private float strokeWidth = 0.15f;
        [SerializeField] private Color strokeColor = Color.black;
        [Tooltip("角と端を丸めるための頂点数")]
        [SerializeField] private int roundVertices = 10;
        [Tooltip("描画順。UI（Screen Space - Camera の Canvas）より手前に描くため、0 より大きくする")]
        [SerializeField] private int sortingOrder = 1;

        private Material sharedMaterial;

        private void Awake()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[KanjiGlyphRenderer] Sprites/Default シェーダが見つかりません。");
                return;
            }
            sharedMaterial = new Material(shader);
        }

        private void OnDestroy()
        {
            if (sharedMaterial != null) Destroy(sharedMaterial);
        }

        /// <summary>
        /// 前の字を消してから、すべての画を描く
        /// </summary>
        public void Draw(IReadOnlyList<StrokeData> strokes)
        {
            Clear();
            for (int i = 0; i < strokes.Count; i++)
            {
                DrawStroke(strokes[i], i + 1);
            }
        }

        /// <summary>
        /// 描いた字を消す
        /// </summary>
        public void Clear()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        private void DrawStroke(StrokeData stroke, int number)
        {
            var lineRenderer = Instantiate(lineRendererPrefab, transform);
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
            // 線の太さは Transform の拡大縮小の影響を受けないので、拡大率を掛けてワールド座標の太さにする
            lineRenderer.startWidth = lineRenderer.endWidth = strokeWidth * transform.lossyScale.x;
            lineRenderer.startColor = lineRenderer.endColor = strokeColor;
            lineRenderer.sortingOrder = sortingOrder;
            if (sharedMaterial != null) lineRenderer.material = sharedMaterial;
        }
    }
}
