using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 選んだ選択肢の組み合わせを、回答の記録（AnswerRecord.SelectedIndex）の1つの数にまとめる・戻す
    /// 選択肢の番号 i を選んだら、ビット i を立てる（選択肢は最大 6 つ）
    /// </summary>
    public static class ReadingQuizAnswer
    {
        /// <summary>選択肢の数の上限</summary>
        public const int MaxChoiceCount = 6;

        /// <summary>
        /// 選んだ選択肢の番号の並びを1つの数（ビットマスク）にまとめる
        /// </summary>
        public static int Encode(IEnumerable<int> selectedIndices)
        {
            if (selectedIndices == null) throw new ArgumentNullException(nameof(selectedIndices));

            int mask = 0;
            foreach (int index in selectedIndices)
            {
                if (index < 0 || index >= MaxChoiceCount) throw new ArgumentOutOfRangeException(nameof(selectedIndices));
                mask |= 1 << index;
            }
            return mask;
        }

        /// <summary>
        /// ビットマスクから、選んだ選択肢の番号を昇順で戻す
        /// </summary>
        /// <param name="mask">Encode でまとめた数。負の数（未回答）なら、何も選んでいない</param>
        public static List<int> Decode(int mask)
        {
            var indices = new List<int>();
            if (mask <= 0) return indices;

            for (int i = 0; i < MaxChoiceCount; i++)
            {
                if ((mask & (1 << i)) != 0) indices.Add(i);
            }
            return indices;
        }

        /// <summary>選択肢 index を選んでいるかどうか</summary>
        public static bool IsSelected(int mask, int index)
        {
            return mask > 0 && index >= 0 && index < MaxChoiceCount && (mask & (1 << index)) != 0;
        }
    }
}
