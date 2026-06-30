using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.StrokeOrderQuestion
{
    /// <summary>
    /// 選択肢プレハブのコンポーネントをまとめて保持するクラス
    /// </summary>
    public class QuizChoiceItem : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private CumulativeStrokeRenderer strokeRenderer;

        public Toggle Toggle => toggle;
        public CumulativeStrokeRenderer StrokeRenderer => strokeRenderer;
    }
}