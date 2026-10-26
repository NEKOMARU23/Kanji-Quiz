using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 1回のプレイの結果。プレイ画面から結果画面へ渡す
    /// </summary>
    public sealed class QuizResult
    {
        /// <summary>モードの表示名（例：書き順クイズ）</summary>
        public string ModeName { get; }

        /// <summary>[もう一度] で読み込むシーンの名前</summary>
        public string RetrySceneName { get; }

        /// <summary>問題を振り返るときに読み込むシーンの名前。振り返りに対応していなければ null</summary>
        public string ReviewSceneName { get; }

        /// <summary>問題を振り返れるかどうか</summary>
        public bool CanReview => !string.IsNullOrEmpty(ReviewSceneName);

        /// <summary>問題ごとの結果</summary>
        public IReadOnlyList<QuizResultItem> Items { get; }

        /// <summary>正解した問題の数</summary>
        public int CorrectCount { get; }

        /// <summary>問題の数</summary>
        public int Total => Items.Count;

        /// <summary>全問正解かどうか</summary>
        public bool IsPerfect => Total > 0 && CorrectCount == Total;

        /// <summary>プレイした難易度</summary>
        public Difficulty Difficulty { get; }

        /// <summary>正解数と難易度から決まる級（例：5級）。問題が1問もなければ null</summary>
        public string Rank => Total > 0 ? KanjiRank.Evaluate(CorrectCount, Total, Difficulty) : null;

        /// <summary>自己ベストを保存するときのキー。自己ベストを残さないなら null</summary>
        public string RecordKey { get; }

        /// <summary>自己ベストを残すかどうか</summary>
        public bool HasRecord => !string.IsNullOrEmpty(RecordKey);

        /// <summary>UpdateBestRecord で調べた自己ベスト。まだ調べていなければ null</summary>
        public BestRecordUpdate? RecordUpdate { get; private set; }

        /// <param name="difficulty">プレイした難易度（級を決めるのに使う）</param>
        /// <param name="recordKey">自己ベストを保存するときのキー。残さないなら null</param>
        public QuizResult(
            string modeName,
            string retrySceneName,
            IReadOnlyList<QuizResultItem> items,
            string reviewSceneName = null,
            Difficulty difficulty = Difficulty.Easy,
            string recordKey = null)
        {
            ModeName = modeName ?? throw new ArgumentNullException(nameof(modeName));
            RetrySceneName = retrySceneName ?? throw new ArgumentNullException(nameof(retrySceneName));
            ReviewSceneName = reviewSceneName;
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Difficulty = difficulty;
            RecordKey = recordKey;

            int correct = 0;
            foreach (var item in items)
            {
                if (item.State == AnswerState.Correct) correct++;
            }
            CorrectCount = correct;
        }

        /// <summary>
        /// [問題を振り返る] で最初に開く問題の番号
        /// 最初に間違えた（または答えていない）問題。全問正解なら 0
        /// </summary>
        public int GetFirstReviewIndex()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].State != AnswerState.Correct) return i;
            }
            return 0;
        }

        /// <summary>
        /// このプレイの正解数で自己ベストを調べ、更新していれば保存する
        /// 2回目以降は保存し直さず、1回目に調べた内容を返す（振り返りから結果画面に戻ったときなど）
        /// 記録がないときは、1問以上正解していれば更新とする
        /// </summary>
        public BestRecordUpdate UpdateBestRecord(IBestRecordRepository repository)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (!HasRecord) throw new InvalidOperationException("自己ベストを残さない結果です。");
            if (RecordUpdate.HasValue) return RecordUpdate.Value;

            bool hasPrevious = repository.TryLoad(RecordKey, out int previous);
            bool isNewBest = hasPrevious ? CorrectCount > previous : CorrectCount > 0;
            int best = hasPrevious ? Math.Max(previous, CorrectCount) : CorrectCount;
            if (isNewBest || !hasPrevious)
            {
                repository.Save(RecordKey, best);
            }

            RecordUpdate = new BestRecordUpdate(best, isNewBest);
            return RecordUpdate.Value;
        }

        /// <summary>
        /// プレイの進行状況から結果を作る
        /// </summary>
        /// <param name="labelSelector">問題から表示用の短い文字列を取り出す</param>
        /// <param name="reviewSceneName">問題を振り返るときに読み込むシーンの名前。振り返りに対応していなければ null</param>
        /// <param name="difficulty">プレイした難易度（級を決めるのに使う）</param>
        /// <param name="recordKey">自己ベストを保存するときのキー。残さないなら null</param>
        public static QuizResult FromSession<TQuestion>(
            string modeName,
            string retrySceneName,
            QuizSession<TQuestion> session,
            Func<TQuestion, string> labelSelector,
            string reviewSceneName = null,
            Difficulty difficulty = Difficulty.Easy,
            string recordKey = null)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (labelSelector == null) throw new ArgumentNullException(nameof(labelSelector));

            var items = new List<QuizResultItem>(session.Count);
            for (int i = 0; i < session.Count; i++)
            {
                items.Add(new QuizResultItem(labelSelector(session.Questions[i]), session.Answers[i].State));
            }
            return new QuizResult(modeName, retrySceneName, items, reviewSceneName, difficulty, recordKey);
        }
    }
}
