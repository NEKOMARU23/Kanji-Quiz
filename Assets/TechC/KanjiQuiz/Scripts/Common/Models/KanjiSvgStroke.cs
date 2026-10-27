using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// KanjiVG の SVG から取り出した1画分のデータ（パスデータと、その画が入っている部品のグループ）
    /// </summary>
    public readonly struct KanjiSvgStroke
    {
        /// <summary>SVG の d 属性の文字列</summary>
        public string PathData { get; }

        /// <summary>この画を囲んでいる g 要素の id。外側から順に並ぶ</summary>
        public IReadOnlyList<string> GroupIds { get; }

        public KanjiSvgStroke(string pathData, IReadOnlyList<string> groupIds)
        {
            PathData = pathData ?? throw new ArgumentNullException(nameof(pathData));
            GroupIds = groupIds ?? Array.Empty<string>();
        }

        /// <summary>
        /// 指定したグループのどれかに入っているかどうか
        /// </summary>
        public bool IsInAnyGroup(ICollection<string> groupIds)
        {
            foreach (var id in GroupIds)
            {
                if (groupIds.Contains(id)) return true;
            }
            return false;
        }
    }
}
