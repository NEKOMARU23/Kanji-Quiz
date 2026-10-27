using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// ゲームモードの一覧。モード選択画面とタイトル画面が読む
    /// </summary>
    [CreateAssetMenu(menuName = "KanjiQuiz/モードの一覧", fileName = "ModeCatalog")]
    public sealed class QuizModeCatalog : ScriptableObject
    {
        [Tooltip("モード選択画面に並べる順")]
        [SerializeField] private List<QuizModeDefinition> modes = new List<QuizModeDefinition>();

        /// <summary>モードの一覧（並べる順）</summary>
        public IReadOnlyList<QuizModeDefinition> Modes => modes;

        /// <summary>ID からモードを探す。見つからなければ null</summary>
        public QuizModeDefinition Find(string modeId)
        {
            foreach (var mode in modes)
            {
                if (mode != null && mode.ModeId == modeId) return mode;
            }
            return null;
        }
    }
}
