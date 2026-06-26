using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Data
{
    /// <summary>
    /// ゲーム全体の進行状況や出題データを保持するクラス
    /// </summary>
    public class GameContext : MonoBehaviour
    {
        [Header("--- 出題データ設定 ---")]
        [Tooltip("このクイズで出題する漢字のリスト（インスペクターから自由に追加・変更できます）")]
        [SerializeField] private List<string> questionKanjiList = new List<string> { "令", "漢", "字" };

        // 外部のゲームモードから安全にデータを参照するためのプロパティ（読み取り専用）
        public List<string> QuestionKanjiList => questionKanjiList;

        [Header("--- 現在のゲーム状況（進行データ） ---")]
        // 現在何問目か (0 = 1問目, 1 = 2問目...)
        public int CurrentQuestionIndex { get; set; } = 0;

        // 現在の正解数（スコア）
        public int CurrentScore { get; set; } = 0;

        /// <summary>
        /// ゲームデータを完全に初期化する（タイトルから新しくゲームを始めるときに呼ぶ）
        /// </summary>
        public void ResetContext()
        {
            CurrentQuestionIndex = 0;
            CurrentScore = 0;
            Debug.Log("[GameContext] データをリセットしました。ゲームを新しく開始できます。");
        }

        /// <summary>
        /// 現在のインデックスに対応する漢字を取得する
        /// </summary>
        public string GetCurrentKanji()
        {
            if (questionKanjiList == null || questionKanjiList.Count == 0)
            {
                Debug.LogError("[GameContext] 出題漢字リストが空っぽです！");
                return string.Empty;
            }

            // インデックスがリストの範囲を超えないように安全弁をつける
            if (CurrentQuestionIndex >= questionKanjiList.Count)
            {
                Debug.LogWarning("[GameContext] すべての問題が出題し終わっています。");
                return string.Empty;
            }

            return questionKanjiList[CurrentQuestionIndex];
        }

        /// <summary>
        /// すべての問題を解き終わったかどうかを判定する
        /// </summary>
        public bool IsGameFinished()
        {
            return CurrentQuestionIndex >= questionKanjiList.Count;
        }
    }
}