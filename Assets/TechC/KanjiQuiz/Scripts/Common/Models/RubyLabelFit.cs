using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// ひらがなで表示するときの、文字の縮め方
    /// ひらがなは漢字より長くなるので、元の大きさを上限に、枠に収まるまで縮める。縮める下限を決める
    /// </summary>
    public static class RubyLabelFit
    {
        /// <summary>下限は、元の大きさの何割か（5文字の級「ろっきゅう」が、ハンコの枠に収まる大きさ。小さすぎて読めなくならないようにもする）</summary>
        public const float MinRatio = 0.35f;

        /// <summary>これより小さくはしない（元の文字が小さいときのため）</summary>
        public const float MinFloor = 10f;

        /// <summary>中央にそろえた文字の、左右にとる余白の、元の大きさに対する割合</summary>
        public const float SidePaddingRatio = 0.4f;

        /// <summary>中央にそろえた文字（ハンコ・ボタン）の、左右にとる余白。枠の線に文字がかからないようにする</summary>
        public static float SidePadding(float originalSize)
        {
            return originalSize <= 0f ? 0f : originalSize * SidePaddingRatio;
        }

        /// <summary>縮めてよい下限の大きさ。元の大きさより大きくはならない</summary>
        public static float MinSize(float originalSize)
        {
            if (originalSize <= 0f) return 0f;
            return Mathf.Min(originalSize, Mathf.Max(MinFloor, originalSize * MinRatio));
        }
    }
}
