using System;
using System.Collections.Generic;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の、語（熟語や言葉）を並べるタブ1つ分
    /// 四字熟語づくり・読み当て・使い分けの正解を、モードごとのタブに並べる
    /// </summary>
    public sealed class WordBookSection
    {
        private readonly Lazy<IReadOnlyList<IdiomBookEntry>> entries;

        /// <summary>タブの名前</summary>
        public string Title { get; }

        /// <summary>集めた語を保存しているモードの ID（そのモードで集めたかどうかで判定する）</summary>
        public string ModeId { get; }

        /// <summary>
        /// true なら、データの語をすべて並べ、まだ集めていない語は「？？」にする（四字熟語・使い分け）
        /// false なら、集めた語だけを集めた順に並べる（読み当て。数が多く、全部並べると使いにくいため）
        /// </summary>
        public bool ShowUncollected { get; }

        /// <summary>データの語（最初に使うときに読む）</summary>
        public IReadOnlyList<IdiomBookEntry> Entries => entries.Value;

        /// <param name="load">データの語を読む。最初に使うときに1回だけ呼ぶ（読み当てのデータは大きいため）</param>
        public WordBookSection(string title, string modeId, Func<IReadOnlyList<IdiomBookEntry>> load, bool showUncollected)
        {
            if (load == null) throw new ArgumentNullException(nameof(load));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            ModeId = modeId ?? throw new ArgumentNullException(nameof(modeId));
            ShowUncollected = showUncollected;
            entries = new Lazy<IReadOnlyList<IdiomBookEntry>>(load);
        }

        /// <summary>読み込み済みの語から作る（テスト用）</summary>
        public WordBookSection(string title, string modeId, IReadOnlyList<IdiomBookEntry> words, bool showUncollected)
            : this(title, modeId, () => words ?? throw new ArgumentNullException(nameof(words)), showUncollected)
        {
        }
    }
}
