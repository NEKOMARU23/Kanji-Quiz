using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// リストのシャッフル処理をまとめたクラス
    /// </summary>
    public static class ShuffleUtility
    {
        /// <summary>
        /// リストをその場でシャッフルする（Fisher-Yates）
        /// </summary>
        public static void Shuffle<T>(IList<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            for (int i = list.Count - 1; i > 0; i--)
            {
                int k = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[k]) = (list[k], list[i]);
            }
        }

        /// <summary>
        /// シャッフルしたコピーを返す。元のリストは変更しない
        /// </summary>
        public static List<T> ShuffledCopy<T>(IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var copy = new List<T>(source);
            Shuffle(copy);
            return copy;
        }
    }
}
