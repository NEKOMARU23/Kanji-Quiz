using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の効果音。鳴らした音を記録する
    /// </summary>
    public class FakeAudioPlayer : IAudioPlayer
    {
        /// <summary>1回だけ鳴らした音（鳴らした順）</summary>
        public List<SeType> PlayedSe { get; } = new List<SeType>();

        /// <summary>ループで鳴らし始めた音（鳴らした順）</summary>
        public List<SeType> PlayedLoops { get; } = new List<SeType>();

        /// <summary>今ループ中かどうか</summary>
        public bool IsLooping { get; private set; }

        public void PlaySe(SeType type) => PlayedSe.Add(type);

        public void PlayLoop(SeType type)
        {
            PlayedLoops.Add(type);
            IsLooping = true;
        }

        public void StopLoop() => IsLooping = false;
    }
}
