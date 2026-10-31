using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字帳（正解して集めた漢字）の保存先。モードごとに分けて保存する
    /// </summary>
    public interface IKanjiBookRepository
    {
        /// <summary>そのモードで集めた漢字を読む。なければ空</summary>
        IReadOnlyCollection<string> Load(string modeId);

        /// <summary>そのモードで集めた漢字を保存する</summary>
        void Save(string modeId, IEnumerable<string> kanji);

        /// <summary>そのモードで集めた漢字を消す（なければ何もしない）</summary>
        void Delete(string modeId);
    }
}
