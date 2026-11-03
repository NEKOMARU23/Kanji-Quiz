using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 問題データ（FourCharIdiomEntry）から四字熟語づくりの問題を作る
    /// 難易度の決まり（FourCharIdiomRule）に合わせて空けるマスを選び、空けた位置のまぎらわしい字を候補に混ぜる
    /// </summary>
    public sealed class FourCharIdiomQuestionFactory
    {
        private readonly Dictionary<string, FourCharIdiomEntry> entries = new Dictionary<string, FourCharIdiomEntry>();
        private readonly List<string> candidates = new List<string>();

        /// <summary>出題できる熟語（問題データの順）</summary>
        public IReadOnlyList<string> Candidates => candidates;

        public FourCharIdiomQuestionFactory(IEnumerable<FourCharIdiomEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.word) || this.entries.ContainsKey(entry.word)) continue;
                this.entries.Add(entry.word, entry);
                candidates.Add(entry.word);
            }
        }

        /// <summary>
        /// 並べた順に熟語を選んで、指定した数の問題を作る。作った問題の出題順はシャッフルする
        /// 苦手な熟語を先頭に並べておけば、優先して出題できる（WeakQuestionList.OrderCandidates）
        /// </summary>
        /// <param name="orderedCandidates">問題を作る順に並べた熟語。問題データにない熟語は飛ばす</param>
        /// <param name="rule">空けるマスとまぎらわしい字の数</param>
        /// <param name="questionCount">作る問題の数</param>
        /// <returns>作れた問題のリスト。熟語が足りないときは指定した数より少なくなる</returns>
        public List<FourCharIdiomQuestion> CreateFromOrdered(IReadOnlyList<string> orderedCandidates, FourCharIdiomRule rule, int questionCount)
        {
            if (orderedCandidates == null) throw new ArgumentNullException(nameof(orderedCandidates));
            if (rule.BlankCount < 1 || rule.BlankCount > FourCharIdiomRule.Length) throw new ArgumentOutOfRangeException(nameof(rule));
            if (rule.TileCount > FourCharIdiomAnswer.MaxTileCount) throw new ArgumentOutOfRangeException(nameof(rule), "候補の字が多すぎます。");
            if (questionCount < 1) throw new ArgumentOutOfRangeException(nameof(questionCount));

            var questions = new List<FourCharIdiomQuestion>(questionCount);
            foreach (var word in orderedCandidates)
            {
                if (questions.Count >= questionCount) break;
                if (!entries.TryGetValue(word, out var entry)) continue;
                var question = TryCreate(entry, rule);
                if (question != null) questions.Add(question);
            }

            if (questions.Count < questionCount)
            {
                Debug.LogWarning($"[FourCharIdiomQuestionFactory] {questionCount} 問を作れませんでした（{questions.Count} 問）。");
            }
            ShuffleUtility.Shuffle(questions);
            return questions;
        }

        /// <summary>1つの熟語から問題を作る。まぎらわしい字が足りなければ null</summary>
        private static FourCharIdiomQuestion TryCreate(FourCharIdiomEntry entry, FourCharIdiomRule rule)
        {
            var characters = KanjiSet.SplitCharacters(entry.word);
            if (characters.Count != FourCharIdiomRule.Length || entry.confusers == null || entry.confusers.Length != FourCharIdiomRule.Length)
            {
                Debug.LogWarning($"[FourCharIdiomQuestionFactory] 「{entry.word}」の問題データが正しくないため除外しました。");
                return null;
            }

            // 空けるマスをランダムに選ぶ
            var positions = ShuffleUtility.ShuffledCopy(new[] { 0, 1, 2, 3 }).GetRange(0, rule.BlankCount);
            positions.Sort();

            // 空けた位置のまぎらわしい字から、熟語の字と重ならないものを選ぶ
            var pool = new List<string>();
            foreach (int position in positions)
            {
                foreach (var ch in KanjiSet.SplitCharacters(entry.confusers[position] ?? string.Empty))
                {
                    if (!characters.Contains(ch) && !pool.Contains(ch)) pool.Add(ch);
                }
            }
            if (pool.Count < rule.DecoyCount)
            {
                Debug.LogWarning($"[FourCharIdiomQuestionFactory] 「{entry.word}」はまぎらわしい字が足りないため除外しました。");
                return null;
            }
            ShuffleUtility.Shuffle(pool);

            var tiles = new List<string>(rule.TileCount);
            foreach (int position in positions) tiles.Add(characters[position]);
            tiles.AddRange(pool.GetRange(0, rule.DecoyCount));
            ShuffleUtility.Shuffle(tiles);

            return new FourCharIdiomQuestion(entry.word, entry.reading, entry.meaning, entry.example, positions, tiles);
        }
    }
}
