using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 音量の設定を PlayerPrefs に保存する
    /// </summary>
    public sealed class PlayerPrefsVolumeRepository : IVolumeRepository
    {
        private const string SeVolumeKey = "Volume_Se";

        public float LoadSeVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(SeVolumeKey, 1f));
        }

        public void SaveSeVolume(float volume)
        {
            PlayerPrefs.SetFloat(SeVolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }
    }
}
