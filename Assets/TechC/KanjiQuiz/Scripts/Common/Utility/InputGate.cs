using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// キーボード操作を一時的に止める
    /// 中断メニューを開いている間や、シーンを切り替えている間に、後ろの画面がキーに反応しないようにする
    /// 止めている側（owner）ごとに数えるので、複数が同時に止めても、全員が解除するまで止まったままになる
    /// </summary>
    public static class InputGate
    {
        private static readonly HashSet<object> blockers = new HashSet<object>();

        /// <summary>止められているかどうか</summary>
        public static bool IsBlocked => blockers.Count > 0;

        /// <summary>止める</summary>
        public static void Block(object owner)
        {
            blockers.Add(owner);
        }

        /// <summary>止めるのをやめる</summary>
        public static void Unblock(object owner)
        {
            blockers.Remove(owner);
        }

        /// <summary>すべて解除する（テスト用）</summary>
        public static void Clear()
        {
            blockers.Clear();
        }
    }
}
