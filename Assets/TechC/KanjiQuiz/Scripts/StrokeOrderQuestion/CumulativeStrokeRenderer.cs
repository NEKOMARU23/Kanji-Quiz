using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TechC.KanjiQuiz.Data; 

namespace TechC.KanjiQuiz.StrokeOrderQuestion
{
    public class CumulativeStrokeRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRendererPrefab;
        [SerializeField] private float strokeWidth = 0.15f;
        [SerializeField] private Color strokeColor = Color.black;

        private Material sharedMaterial;
        private KanjiDataParser kanjiParser; // ステップ1で作った計算クラス

        void Awake()
        {
            // 計算担当クラスをインスタンス化（初期化）
            kanjiParser = new KanjiDataParser();

            // ラインの描画に使うマテリアルを用意
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("Sprites/Default シェーダが見つかりません。");
                return;
            }
            sharedMaterial = new Material(shader);
        }

        /// <summary>
        /// 外部（ゲーム管理クラスなど）からこのメソッドを呼ぶことで、漢字の描画が始まります
        /// </summary>
        /// <param name="kanji">表示したい漢字（例: "令"）</param>
        /// <param name="correctOrder">正しい書き順で表示するかどうか</param>
        public void StartKanjiDrawing(string kanji, bool correctOrder = true)
        {
            // 1️⃣ 既存の描画（古い子オブジェクト）があればきれいに削除
            ClearExistingStrokes();

            // 2️⃣ 計算担当クラス（KanjiDataParser）から、SVGのテキストデータを抽出してもらう
            List<string> strokeXmls = kanjiParser.LoadSingleKanjiStrokes(kanji);
            if (strokeXmls == null || strokeXmls.Count == 0)
            {
                Debug.LogWarning($"[CumulativeStrokeRenderer] 漢字 '{kanji}' のデータが見つからないか、空です。");
                return;
            }

            // 3️⃣ さらに計算担当クラスに、テキストデータを「Unityの座標（Vector3）のリスト」に変換してもらう
            List<List<Vector3>> strokePointsList = kanjiParser.GenerateStrokePoints(strokeXmls, correctOrder);

            // 4️⃣ 演出（コルーチン）を開始して画面に1画ずつ描画する
            StartCoroutine(DrawCumulativeStrokesCoroutine(strokePointsList));
        }

        // 昔の④の処理：書き順に沿って段階的にLineRendererを生成・配置する演出
        private IEnumerator DrawCumulativeStrokesCoroutine(List<List<Vector3>> strokes)
        {
            Vector3 baseOffset = Vector3.zero;

            for (int i = 1; i <= strokes.Count; i++)
            {
                // 「〇画目までの状態」をまとめる親オブジェクト
                GameObject parent = new GameObject($"DrawnKanji_{i}Strokes");
                parent.transform.SetParent(transform, false);

                for (int j = 0; j < i; j++)
                {
                    GameObject strokeObj = new GameObject($"Stroke_{j + 1}");
                    strokeObj.transform.SetParent(parent.transform, false);

                    // プレハブからLineRendererを生成
                    var lr = Instantiate(lineRendererPrefab, strokeObj.transform);
                    var points = strokes[j];

                    // 座標を設定
                    lr.positionCount = points.Count;
                    lr.SetPositions(points.ToArray());
                    lr.useWorldSpace = false;

                    // 線の見た目をきれいに設定
                    lr.numCornerVertices = 10;
                    lr.numCapVertices = 10;
                    lr.startWidth = lr.endWidth = strokeWidth;
                    lr.startColor = lr.endColor = strokeColor;
                    lr.material = sharedMaterial;
                }

                // 段階ごとに少しずつ右にずらして並べる演出
                parent.transform.localPosition = new Vector3(i * 2.5f, baseOffset.y, baseOffset.z);

                // 昔のコードの挙動を再現（1フレーム待つ、あるいは即時並べるなら yield break 等に変更可）
                yield return null;
            }

            Debug.Log($"[CumulativeStrokeRenderer] 漢字の累積描画が完了しました。総画数: {strokes.Count}");
        }

        // 画面をリセットする便利メソッド
        public void ClearExistingStrokes()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}