using System.Collections.Generic;
using UnityEngine;
using TechC.KanjiQuiz.Data;
using Cysharp.Threading.Tasks;

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
        [SerializeField] private float horizontalSpacing = 2.5f; 

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

        /// <summary>
        /// 指定された漢字の書き順を段階的に描画する
        /// </summary>
        /// <param name="kanji"></param>
        /// <param name="correctOrder"></param>
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

            DrawCumulativeStrokesAsync(strokeDataList, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid DrawCumulativeStrokesAsync(List<StrokeData> strokes, System.Threading.CancellationToken token)
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

                parent.transform.localPosition = new Vector3(i * horizontalSpacing, baseOffset.y, baseOffset.z);
                
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            Debug.Log($"[CumulativeStrokeRenderer] 漢字の累積描画が完了しました。総画数: {strokes.Count}");
        }

        /// <summary>
        /// 既存の描画済みストロークをすべて削除する
        /// </summary>
        public void ClearExistingStrokes()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 解析・シャッフル済みのストロークデータを直接受け取って描画を開始する
        /// </summary>
        public void DrawStrokeData(List<StrokeData> strokeDataList)
        {
            ClearExistingStrokes();
            DrawCumulativeStrokesAsync(strokeDataList, this.GetCancellationTokenOnDestroy()).Forget();
        }
    }
}