using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 1画分の座標データ
    /// </summary>
    public readonly struct StrokeData
    {
        /// <summary>1画を構成する点の列</summary>
        public IReadOnlyList<Vector3> Points { get; }

        public StrokeData(IReadOnlyList<Vector3> points)
        {
            Points = points;
        }
    }
}
