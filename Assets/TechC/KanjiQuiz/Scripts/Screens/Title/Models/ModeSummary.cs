using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// モード選択画面の時間割の1行と、黒板に書く内容（モードの説明と難易度ごとの記録）
    /// </summary>
    public sealed class ModeSummary
    {
        /// <summary>モードの定義</summary>
        public QuizModeDefinition Definition { get; }

        /// <summary>時間割の時限（1 始まり）</summary>
        public int Period { get; }

        /// <summary>遊べるかどうか（false なら時間割に「自習」と書く）</summary>
        public bool IsAvailable => Definition.IsAvailable;

        /// <summary>難易度ごとの記録（選べる難易度の順）</summary>
        public IReadOnlyList<DifficultyRecord> Records { get; }

        /// <summary>そのモードで取った一番上の級。記録がなければ null</summary>
        public string BestRank { get; }

        public ModeSummary(QuizModeDefinition definition, int period, IReadOnlyList<DifficultyRecord> records)
        {
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            Period = period;
            Records = records ?? throw new ArgumentNullException(nameof(records));

            int bestLevel = -1;
            foreach (var record in records)
            {
                if (record.RankLevel > bestLevel) bestLevel = record.RankLevel;
            }
            BestRank = bestLevel >= 0 ? KanjiRank.NameOf(bestLevel) : null;
        }

        /// <summary>
        /// モードの一覧と保存先から、時間割に並べる内容を作る
        /// </summary>
        public static IReadOnlyList<ModeSummary> Load(
            IReadOnlyList<QuizModeDefinition> modes, IBestRecordRepository bestRecords, IWeakQuestionRepository weakQuestions)
        {
            if (modes == null) throw new ArgumentNullException(nameof(modes));
            if (bestRecords == null) throw new ArgumentNullException(nameof(bestRecords));
            if (weakQuestions == null) throw new ArgumentNullException(nameof(weakQuestions));

            var summaries = new List<ModeSummary>();
            foreach (var mode in modes)
            {
                if (mode == null) continue;

                var records = new List<DifficultyRecord>();
                foreach (var difficulty in mode.Difficulties)
                {
                    string key = mode.RecordKey(difficulty);
                    int? best = bestRecords.TryLoad(key, out int count) ? count : (int?)null;
                    records.Add(new DifficultyRecord(
                        difficulty, mode.Range(difficulty), mode.QuestionCount(difficulty), best, weakQuestions.Load(key).Items));
                }
                summaries.Add(new ModeSummary(mode, summaries.Count + 1, records));
            }
            return summaries;
        }
    }
}
