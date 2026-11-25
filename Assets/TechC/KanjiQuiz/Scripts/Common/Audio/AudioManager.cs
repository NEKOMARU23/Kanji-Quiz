using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 効果音と BGM を鳴らす。シーンをまたいで1つだけ置く
    /// 鳴らす音はインスペクターで種類ごとに設定する
    /// </summary>
    public sealed class AudioManager : SingletonMonoBehaviour<AudioManager>, IAudioPlayer, IBgmPlayer
    {
        [Serializable]
        private struct SeEntry
        {
            public SeType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Serializable]
        private struct BgmEntry
        {
            public BgmType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Tooltip("1回だけ鳴らす効果音用")]
        [SerializeField] private AudioSource seSource;
        [Tooltip("ループで鳴らす効果音用")]
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private SeEntry[] seEntries;

        [Tooltip("BGM 用。Loop は鳴らすときにコードで有効にする")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private BgmEntry[] bgmEntries;
        [Tooltip("BGM を切り替える・止めるときにかける、フェードアウト／フェードインの秒数")]
        [SerializeField, Min(0f)] private float bgmFadeSeconds = 0.6f;

        private float seVolume = 1f;
        private float loopEntryVolume = 1f;

        private float bgmVolume = 1f;
        private float bgmEntryVolume = 1f;
        // フェードの途中の音量（0〜1）。曲ごとの音量と BGM の音量にかけ合わせる
        private float bgmFade = 1f;
        // 今流している（流そうとしている）曲。止めたあと、まだ何も流していないときは null
        private BgmType? requestedBgm;
        private CancellationTokenSource bgmCts;

        protected override void OnAwake()
        {
            // 中断メニューで設定した音量を読み込む
            var volumeRepository = new PlayerPrefsVolumeRepository();
            SetSeVolume(volumeRepository.LoadSeVolume());
            SetBgmVolume(volumeRepository.LoadBgmVolume());
        }

        protected override void OnRelease()
        {
            CancelBgmFade();
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

        public void PlayBgm(BgmType type)
        {
            if (bgmSource == null) return;
            // すでにその曲を流している（流そうとしている）なら頭から流し直さない
            if (requestedBgm == type) return;
            if (!TryGetBgmEntry(type, out var entry)) return;

            requestedBgm = type;
            SwitchBgmAsync(entry, ResetBgmFade()).Forget();
        }

        public void StopBgm()
        {
            if (bgmSource == null) return;
            if (requestedBgm == null) return;

            requestedBgm = null;
            StopBgmAsync(ResetBgmFade()).Forget();
        }

        public void SetBgmVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            ApplyBgmVolume();
        }

        private async UniTaskVoid SwitchBgmAsync(BgmEntry entry, CancellationToken ct)
        {
            // 流れている曲があれば、いまの音量からフェードアウトする
            if (bgmSource.isPlaying) await FadeBgmAsync(0f, ct);

            bgmSource.Stop();
            bgmSource.clip = entry.clip;
            bgmSource.loop = true;
            bgmEntryVolume = entry.volume;
            bgmFade = 0f;
            ApplyBgmVolume();
            bgmSource.Play();

            await FadeBgmAsync(1f, ct);
        }

        private async UniTaskVoid StopBgmAsync(CancellationToken ct)
        {
            await FadeBgmAsync(0f, ct);
            bgmSource.Stop();
        }

        // bgmFade を、いまの値から target まで動かす。全体にかかる秒数は、動かす幅に比例させる
        private async UniTask FadeBgmAsync(float target, CancellationToken ct)
        {
            float start = bgmFade;
            float duration = bgmFadeSeconds * Mathf.Abs(target - start);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                bgmFade = Mathf.Lerp(start, target, elapsed / duration);
                ApplyBgmVolume();

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                // 一時停止の仕組みが timeScale を止めるようになっても、フェードが止まらないようにする
                elapsed += Time.unscaledDeltaTime;
            }
            bgmFade = target;
            ApplyBgmVolume();
        }

        private void ApplyBgmVolume()
        {
            if (bgmSource == null) return;
            bgmSource.volume = bgmFade * bgmEntryVolume * bgmVolume;
        }

        // 動いているフェードを止めて、新しいフェード用のトークンを返す。AudioManager が破棄されたら一緒に止まる
        private CancellationToken ResetBgmFade()
        {
            CancelBgmFade();
            bgmCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            return bgmCts.Token;
        }

        private void CancelBgmFade()
        {
            bgmCts?.Cancel();
            bgmCts?.Dispose();
            bgmCts = null;
        }

        private bool TryGetBgmEntry(BgmType type, out BgmEntry entry)
        {
            foreach (var e in bgmEntries)
            {
                if (e.type == type && e.clip != null)
                {
                    entry = e;
                    return true;
                }
            }

            Debug.LogWarning($"[AudioManager] {type} の BGM が設定されていません。");
            entry = default;
            return false;
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
