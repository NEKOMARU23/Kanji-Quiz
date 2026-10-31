using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 保存した記録（自己ベスト・苦手な問題・漢字帳）を、すべてのモードと難易度についてまとめて消す
    /// </summary>
    public sealed class QuizRecordEraser
    {
        private readonly IReadOnlyList<QuizModeDefinition> modes;
        private readonly IBestRecordRepository bestRecordRepository;
        private readonly IWeakQuestionRepository weakQuestionRepository;
        private readonly IKanjiBookRepository kanjiBookRepository;

        /// <param name="modes">記録を消すモード（ふつうはモードの一覧のすべて）</param>
        /// <param name="kanjiBookRepository">漢字帳の保存先。null なら漢字帳は消さない</param>
        public QuizRecordEraser(
            IReadOnlyList<QuizModeDefinition> modes,
            IBestRecordRepository bestRecordRepository,
            IWeakQuestionRepository weakQuestionRepository,
            IKanjiBookRepository kanjiBookRepository = null)
        {
            this.kanjiBookRepository = kanjiBookRepository;
            this.modes = modes ?? throw new ArgumentNullException(nameof(modes));
            this.bestRecordRepository = bestRecordRepository ?? throw new ArgumentNullException(nameof(bestRecordRepository));
            this.weakQuestionRepository = weakQuestionRepository ?? throw new ArgumentNullException(nameof(weakQuestionRepository));
        }

        /// <summary>すべての記録を消す</summary>
        public void EraseAll()
        {
            foreach (var mode in modes)
            {
                if (mode == null) continue;
                kanjiBookRepository?.Delete(mode.ModeId);
                foreach (var difficulty in mode.Difficulties)
                {
                    string key = mode.RecordKey(difficulty);
                    bestRecordRepository.Delete(key);
                    weakQuestionRepository.Delete(key);
                }
            }
        }
    }
}
