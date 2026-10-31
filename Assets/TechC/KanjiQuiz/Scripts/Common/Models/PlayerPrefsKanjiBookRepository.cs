using System.Collections.Generic;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字帳を PlayerPrefs に保存する（キーは KanjiBook_モード、値は漢字をつなげた文字列）
    /// </summary>
    public sealed class PlayerPrefsKanjiBookRepository : IKanjiBookRepository
    {
        private const string KeyPrefix = "KanjiBook_";

        public IReadOnlyCollection<string> Load(string modeId)
        {
            return KanjiSet.SplitCharacters(PlayerPrefs.GetString(KeyPrefix + modeId, string.Empty));
        }

        public void Save(string modeId, IEnumerable<string> kanji)
        {
            PlayerPrefs.SetString(KeyPrefix + modeId, string.Concat(kanji));
            PlayerPrefs.Save();
        }

        public void Delete(string modeId)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + modeId);
            PlayerPrefs.Save();
        }
    }
}
