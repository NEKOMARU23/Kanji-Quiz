using UnityEngine;
using TechC.KanjiQuiz.StrokeOrderQuestion; 
public class KanjiTestRunner : MonoBehaviour
{
    // インスペクターで先ほどの描画クラスを紐付ける
    [SerializeField] private CumulativeStrokeRenderer strokeRenderer;
    
    // テストしたい漢字（インスペクターから自由に変えられます）
    [SerializeField] private string testKanji = "令";

    void Start()
    {
        if (strokeRenderer != null)
        {
            // 描画処理をキックする！
            strokeRenderer.StartKanjiDrawing(testKanji, true);
        }
        else
        {
            Debug.LogError("strokeRenderer がセットされていません！");
        }
    }
}