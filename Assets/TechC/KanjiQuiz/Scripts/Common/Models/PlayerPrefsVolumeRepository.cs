using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 音量の設定を PlayerPrefs に保存する
    /// </summary>
    public sealed class PlayerPrefsVolumeRepository : IVolumeRepository
    {
        private const string SeVolumeKey = "Volume_Se";
        private const string BgmVolumeKey = "Volume_Bgm";

        // 効果音（初期値 1）より低くして、BGM が問題文や効果音をじゃましないようにする
        private const float DefaultBgmVolume = 0.5f;

        public float LoadSeVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(SeVolumeKey, 1f));
        }

        public void SaveSeVolume(float volume)
        {
            PlayerPrefs.SetFloat(SeVolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }

        public float LoadBgmVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, DefaultBgmVolume));
        }

        public void SaveBgmVolume(float volume)
        {
            PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
        }
    }
}
