using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 表示の設定を PlayerPrefs に保存する
    /// </summary>
    public sealed class PlayerPrefsDisplaySettingRepository : IDisplaySettingRepository
    {
        private const string UseHiraganaKey = "Option_Hiragana";

        public bool LoadUseHiragana()
        {
            return PlayerPrefs.GetInt(UseHiraganaKey, 0) != 0;
        }

        public void SaveUseHiragana(bool value)
        {
            PlayerPrefs.SetInt(UseHiraganaKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
