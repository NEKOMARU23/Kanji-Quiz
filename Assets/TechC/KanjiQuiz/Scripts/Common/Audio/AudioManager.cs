using System;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 効果音を鳴らす。シーンをまたいで1つだけ置く
    /// 鳴らす音はインスペクターで種類ごとに設定する
    /// </summary>
    public sealed class AudioManager : SingletonMonoBehaviour<AudioManager>, IAudioPlayer
    {
        [Serializable]
        private struct SeEntry
        {
            public SeType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Tooltip("1回だけ鳴らす効果音用")]
        [SerializeField] private AudioSource seSource;
        [Tooltip("ループで鳴らす効果音用")]
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private SeEntry[] seEntries;

        private float seVolume = 1f;
        private float loopEntryVolume = 1f;

        protected override void OnAwake()
        {
            // 中断メニューで設定した音量を読み込む
            SetSeVolume(new PlayerPrefsVolumeRepository().LoadSeVolume());
        }

        public void PlaySe(SeType type)
        {
            // Play を止めたときやアプリの終了時は、AudioSource が呼んだ側より先に破棄されていることがある
            if (seSource == null) return;
            if (!TryGetEntry(type, out var entry)) return;

            seSource.PlayOneShot(entry.clip, entry.volume * seVolume);
        }

        public void PlayLoop(SeType type)
        {
            if (loopSource == null) return;
            if (!TryGetEntry(type, out var entry)) return;

            loopSource.Stop();
            loopSource.clip = entry.clip;
            loopEntryVolume = entry.volume;
            loopSource.volume = loopEntryVolume * seVolume;
            loopSource.loop = true;
            loopSource.Play();
        }

        public void StopLoop()
        {
            if (loopSource == null) return;
            loopSource.Stop();
        }

        public void SetSeVolume(float volume)
        {
            seVolume = Mathf.Clamp01(volume);
            // 鳴っているループの音量もすぐに変える
            if (loopSource != null) loopSource.volume = loopEntryVolume * seVolume;
        }

        private bool TryGetEntry(SeType type, out SeEntry entry)
        {
            foreach (var e in seEntries)
            {
                if (e.type == type && e.clip != null)
                {
                    entry = e;
                    return true;
                }
            }

            Debug.LogWarning($"[AudioManager] {type} の効果音が設定されていません。");
            entry = default;
            return false;
        }
    }
}
