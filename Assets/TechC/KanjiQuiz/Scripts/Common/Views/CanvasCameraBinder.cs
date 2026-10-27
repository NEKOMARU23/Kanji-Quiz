using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// Screen Space - Camera の Canvas に、シーンのカメラ（Camera.main）を設定する
    /// プレハブの Canvas はシーンのカメラを参照できないので、置いたシーンで起動したときに設定する
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraBinder : MonoBehaviour
    {
        private void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }
    }
}
