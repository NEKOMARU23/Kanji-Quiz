using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 問題の振り返りに使うデータの置き場所
    /// プレイ画面が終了時に問題と回答を置き、結果画面が振り返りたい問題の番号を頼み、
    /// 各モードの EntryPoint がそれを受け取って振り返りの画面を開く
    /// </summary>
    public static class QuizReviewStore
    {
        private static object questions;
        private static AnswerRecord[] answers;

        /// <summary>振り返りを頼まれた問題の番号。頼まれていなければ null</summary>
        public static int? RequestedIndex { get; private set; }

        /// <summary>
        /// 振り返りに使う問題と回答を置く。前に頼まれていた振り返りは取り消す
        /// </summary>
        public static void Set<TQuestion>(IReadOnlyList<TQuestion> questionList, IReadOnlyList<AnswerRecord> answerList)
        {
            questions = new List<TQuestion>(questionList);
            answers = new AnswerRecord[answerList.Count];
            for (int i = 0; i < answerList.Count; i++) answers[i] = answerList[i];
            RequestedIndex = null;
        }

        /// <summary>
        /// 指定した番号の問題の振り返りを頼む
        /// </summary>
        public static void Request(int index)
        {
            RequestedIndex = index;
        }

        /// <summary>
        /// 頼まれた振り返りを受け取る。受け取ったら頼みは消える（データは残る）
        /// </summary>
        /// <returns>頼まれていて、問題の型と番号が合っていれば true</returns>
        public static bool TryTakeRequest<TQuestion>(
            out IReadOnlyList<TQuestion> questionList,
            out IReadOnlyList<AnswerRecord> answerList,
            out int index)
        {
            questionList = questions as List<TQuestion>;
            answerList = answers;
            index = RequestedIndex ?? -1;

            if (!RequestedIndex.HasValue) return false;

            // 頼みは1回だけ使う。データが合わなくても消しておく
            RequestedIndex = null;
            if (questionList == null || answerList == null) return false;
            return index >= 0 && index < questionList.Count;
        }

        /// <summary>置いたデータと頼みを消す（テスト用）</summary>
        public static void Clear()
        {
            questions = null;
            answers = null;
            RequestedIndex = null;
        }
    }
}
