using System;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// プレイが終わって結果画面へ移る前の記録を、全モード共通でまとめて行う
    /// 振り返りのデータと結果を置き、苦手な問題を保存し、正解したものを漢字帳に足す
    /// シーンの読み込みはしない（呼び出し側の組み立て役が行う）
    /// </summary>
    public sealed class QuizResultRecorder
    {
        private readonly IWeakQuestionRepository weakRepository;
        private readonly IKanjiBookRepository kanjiBookRepository;

        public QuizResultRecorder(IWeakQuestionRepository weakRepository, IKanjiBookRepository kanjiBookRepository)
        {
            this.weakRepository = weakRepository ?? throw new ArgumentNullException(nameof(weakRepository));
            this.kanjiBookRepository = kanjiBookRepository ?? throw new ArgumentNullException(nameof(kanjiBookRepository));
        }

        /// <summary>
        /// 1回のプレイを記録して、結果を返す
        /// </summary>
        /// <param name="mode">プレイしたモード。振り返りと「もう一度」は、このモードのシーンを開く</param>
        /// <param name="difficulty">プレイした難易度</param>
        /// <param name="session">プレイの進行状況</param>
        /// <param name="labelSelector">
        /// 問題から表示用の短い文字列を取り出す。苦手な問題と漢字帳に保存する値にもなるので、
        /// 出題の候補（WeakQuestionList.OrderCandidates に渡すもの）と同じ文字列にする
        /// </param>
        /// <param name="weakList">出題の準備で読んだ、このモードと難易度の苦手な問題</param>
        public QuizResult Record<TQuestion>(
            QuizModeDefinition mode,
            Difficulty difficulty,
            QuizSession<TQuestion> session,
            Func<TQuestion, string> labelSelector,
            WeakQuestionList weakList)
        {
            if (mode == null) throw new ArgumentNullException(nameof(mode));
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (labelSelector == null) throw new ArgumentNullException(nameof(labelSelector));
            if (weakList == null) throw new ArgumentNullException(nameof(weakList));

            // 振り返りに使う問題と回答を置き、結果から同じシーンを振り返りとして開けるようにする
            QuizReviewStore.Set(session.Questions, session.Answers);
            string recordKey = mode.RecordKey(difficulty);
            var result = QuizResult.FromSession(
                mode.Title(difficulty), mode.SceneName, session, labelSelector, mode.SceneName, difficulty, recordKey);
            QuizResultStore.Set(result);

            // 間違えた問題を苦手に加え、正解した問題を外す
            weakList.RecordResult(result);
            weakRepository.Save(recordKey, weakList);

            // 正解したものを漢字帳に集める
            KanjiBook.Collect(kanjiBookRepository, mode.ModeId, result);

            return result;
        }
    }
}
