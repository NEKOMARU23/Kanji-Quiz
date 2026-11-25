using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字帳。モードごとに、正解して集めた漢字を持つ
    /// </summary>
    public sealed class KanjiBook
    {
        private readonly List<string> modeIds = new List<string>();
        private readonly Dictionary<string, HashSet<string>> collected = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, List<string>> collectedInOrder = new Dictionary<string, List<string>>();

        /// <summary>
        /// 保存先から、指定したモードの漢字帳を読む
        /// </summary>
        /// <param name="modeIds">読むモード（この順に ModesOf で返す）</param>
        public static KanjiBook Load(IKanjiBookRepository repository, IEnumerable<string> modeIds)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (modeIds == null) throw new ArgumentNullException(nameof(modeIds));

            var book = new KanjiBook();
            foreach (var modeId in modeIds)
            {
                if (string.IsNullOrEmpty(modeId) || book.collected.ContainsKey(modeId)) continue;
                book.modeIds.Add(modeId);

                // 集めた順を覚えておく（読み当ての一覧は、集めた語だけを集めた順に並べるため）
                var set = new HashSet<string>();
                var list = new List<string>();
                foreach (var word in repository.Load(modeId))
                {
                    if (set.Add(word)) list.Add(word);
                }
                book.collected[modeId] = set;
                book.collectedInOrder[modeId] = list;
            }
            return book;
        }

        /// <summary>
        /// 1回のプレイの結果から、正解したもの（結果の Label。漢字1字か、四字熟語づくりなら熟語）をそのモードの漢字帳に足して保存する
        /// </summary>
        /// <returns>新しく集めた漢字の数</returns>
        public static int Collect(IKanjiBookRepository repository, string modeId, QuizResult result)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            if (string.IsNullOrEmpty(modeId)) throw new ArgumentException("モードが空です。", nameof(modeId));
            if (result == null) throw new ArgumentNullException(nameof(result));

            var kanji = new List<string>(repository.Load(modeId));
            var known = new HashSet<string>(kanji);
            int added = 0;
            foreach (var item in result.Items)
            {
                if (item.State != AnswerState.Correct || string.IsNullOrEmpty(item.Label)) continue;
                if (!known.Add(item.Label)) continue;
                kanji.Add(item.Label);
                added++;
            }

            if (added > 0) repository.Save(modeId, kanji);
            return added;
        }

        /// <summary>どれかのモードで集めた漢字かどうか</summary>
        public bool IsCollected(string kanji)
        {
            foreach (var set in collected.Values)
            {
                if (set.Contains(kanji)) return true;
            }
            return false;
        }

        /// <summary>
        /// そのモードで集めたかどうか
        /// 熟語のように、モードをまたいで同じ文字列が出てくるもの（一心同体は四字熟語づくりにも読み当てにもある）は、
        /// IsCollected では別のモードで集めたものも「集めた」になるので、こちらで調べる
        /// </summary>
        public bool IsCollectedIn(string modeId, string word)
        {
            return modeId != null && collected.TryGetValue(modeId, out var set) && set.Contains(word);
        }

        /// <summary>そのモードで集めたもの（集めた順）。Load で読んでいないモードは空</summary>
        public IReadOnlyList<string> CollectedIn(string modeId)
        {
            if (modeId != null && collectedInOrder.TryGetValue(modeId, out var list)) return list;
            return Array.Empty<string>();
        }

        /// <summary>その漢字を集めたモード（Load で渡した順）</summary>
        public IReadOnlyList<string> ModesOf(string kanji)
        {
            var result = new List<string>();
            foreach (var modeId in modeIds)
            {
                if (collected[modeId].Contains(kanji)) result.Add(modeId);
            }
            return result;
        }

        /// <summary>並べた漢字のうち、集めた漢字の数</summary>
        public int CountCollected(IEnumerable<string> kanji)
        {
            int count = 0;
            foreach (var k in kanji)
            {
                if (IsCollected(k)) count++;
            }
            return count;
        }
    }
}
