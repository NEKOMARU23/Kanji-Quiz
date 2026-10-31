using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 自己ベストを PlayerPrefs に保存する
    /// </summary>
    public sealed class PlayerPrefsBestRecordRepository : IBestRecordRepository
    {
        private const string KeyPrefix = "BestRecord_";

        public bool TryLoad(string key, out int bestCorrectCount)
        {
            string prefsKey = KeyPrefix + key;
            if (!PlayerPrefs.HasKey(prefsKey))
            {
                bestCorrectCount = 0;
                return false;
            }

            bestCorrectCount = PlayerPrefs.GetInt(prefsKey);
            return true;
        }

        public void Save(string key, int bestCorrectCount)
        {
            PlayerPrefs.SetInt(KeyPrefix + key, bestCorrectCount);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + key);
            PlayerPrefs.Save();
        }
    }
}
