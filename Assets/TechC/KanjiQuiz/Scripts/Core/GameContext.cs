using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

namespace TechC.KanjiQuiz.Data
{
    public class GameContext : MonoBehaviour
    {
        [Header("--- 出題データ設定 ---")]
        [Tooltip("このクイズで出題する漢字のリスト")]
        [SerializeField] private List<string> questionKanjiList = new List<string> { "令", "漢", "字" };

        public List<string> QuestionKanjiList => questionKanjiList;

        [Header("--- 現在のゲーム状況（進行データ） ---")]
        public int CurrentQuestionIndex { get; set; } = 0;

        // true = 正解, false = 不正解, null = 未回答
        public List<bool?> AnswerHistory { get; private set; } = new List<bool?>();

        public int CurrentScore => AnswerHistory.Count(ans => ans == true);

        /// <summary>
        /// ゲームの進行状況をリセットするメソッド
        /// これにより、CurrentQuestionIndex が 0 にリセットされ、AnswerHistory が空の状態に戻る
        /// さらに、AnswerHistory のサイズを questionKanjiList のサイズに合わせて 
        /// </summary>
        public void ResetContext()
        {
            CurrentQuestionIndex = 0;
            AnswerHistory.Clear();
            
            for (int i = 0; i < questionKanjiList.Count; i++)
            {
                AnswerHistory.Add(null);
            }
            Debug.Log("[GameContext] データをリセットしました。ゲームを新しく開始できます。");
        }

        private void Start()
        {
            if (AnswerHistory.Count == 0) ResetContext();
        }

        /// <summary>
        /// 現在の問題の漢字を取得するメソッド
        /// もし現在の問題番号が出題リストの範囲外であれば、空文字列を返す
        /// </summary>
        /// <returns></returns>
        public string GetCurrentKanji()
        {
            if (questionKanjiList == null || questionKanjiList.Count == 0) return string.Empty;
            if (CurrentQuestionIndex >= questionKanjiList.Count) return string.Empty;
            return questionKanjiList[CurrentQuestionIndex];
        }

        /// <summary>
        /// ゲームが終了しているかどうかを判定するメソッド
        /// ゲーム終了の条件は、現在の問題番号が出題リストの数を超えた場合とする
        /// </summary>
        /// <returns></returns>
        public bool IsGameFinished()
        {
            return CurrentQuestionIndex >= questionKanjiList.Count;
        }

        /// <summary>
        /// ★ 現在の問題に対する回答を記録するメソッド
        /// これにより、AnswerHistory が更新され、CurrentScore も自動的に計算されるようになる  
        /// </summary>
        /// <param name="isCorrect"></param>
        public void RecordAnswer(bool isCorrect)
        {
            if (CurrentQuestionIndex < AnswerHistory.Count)
            {
                AnswerHistory[CurrentQuestionIndex] = isCorrect;
            }
        }
    }
}