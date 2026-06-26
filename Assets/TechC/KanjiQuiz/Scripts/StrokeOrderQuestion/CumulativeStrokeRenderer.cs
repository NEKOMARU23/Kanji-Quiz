using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TechC.KanjiQuiz.Data; 

namespace TechC.KanjiQuiz.StrokeOrderQuestion
{
    /// <summary>
    /// 漢字の書き順を段階的に描画するクラス
    /// </summary>
    public class CumulativeStrokeRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRendererPrefab;
        [SerializeField] private float strokeWidth = 0.15f;
        [SerializeField] private Color strokeColor = Color.black;

        private Material sharedMaterial;
        private KanjiDataParser kanjiParser; 

        void Awake()
        {
            kanjiParser = new KanjiDataParser();

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("Sprites/Default シェーダが見つかりません。");
                return;
            }
            sharedMaterial = new Material(shader);
        }

        public void StartKanjiDrawing(string kanji, bool correctOrder = true)
        {
            ClearExistingStrokes();

            List<string> strokeXmls = kanjiParser.LoadSingleKanjiStrokes(kanji);
            if (strokeXmls == null || strokeXmls.Count == 0)
            {
                Debug.LogWarning($"[CumulativeStrokeRenderer] 漢字 '{kanji}' のデータが見つからないか、空です。");
                return;
            }

            List<StrokeData> strokeDataList = kanjiParser.GenerateStrokePoints(strokeXmls, correctOrder);

            StartCoroutine(DrawCumulativeStrokesCoroutine(strokeDataList));
        }

        private IEnumerator DrawCumulativeStrokesCoroutine(List<StrokeData> strokes)
        {
            Vector3 baseOffset = Vector3.zero;

            for (int i = 1; i <= strokes.Count; i++)
            {
                GameObject parent = new GameObject($"DrawnKanji_{i}Strokes");
                parent.transform.SetParent(transform, false);

                for (int j = 0; j < i; j++)
                {
                    GameObject strokeObj = new GameObject($"Stroke_{j + 1}");
                    strokeObj.transform.SetParent(parent.transform, false);

                    var lr = Instantiate(lineRendererPrefab, strokeObj.transform);
                    
                    // ★ 2次元リストの記述から、構造体のプロパティ参照（.points）に変更！
                    var points = strokes[j].points;

                    lr.positionCount = points.Count;
                    lr.SetPositions(points.ToArray());
                    lr.useWorldSpace = false;

                    lr.numCornerVertices = 10;
                    lr.numCapVertices = 10;
                    lr.startWidth = lr.endWidth = strokeWidth;
                    lr.startColor = lr.endColor = strokeColor;
                    lr.material = sharedMaterial;
                }

                parent.transform.localPosition = new Vector3(i * 2.5f, baseOffset.y, baseOffset.z);
                yield return null;
            }

            Debug.Log($"[CumulativeStrokeRenderer] 漢字の累積描画が完了しました。総画数: {strokes.Count}");
        }

        public void ClearExistingStrokes()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}