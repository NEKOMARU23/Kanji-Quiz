using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の BGM。流した曲と音量を記録する
    /// </summary>
    public class FakeBgmPlayer : IBgmPlayer
    {
        /// <summary>流した曲（呼んだ順）</summary>
        public List<BgmType> PlayedBgm { get; } = new List<BgmType>();

        /// <summary>止めた回数</summary>
        public int StopCount { get; private set; }

        /// <summary>最後に設定された BGM の音量</summary>
        public float BgmVolume { get; private set; } = 1f;

        public void PlayBgm(BgmType type) => PlayedBgm.Add(type);

        public void StopBgm() => StopCount++;

        public void SetBgmVolume(float volume) => BgmVolume = volume;
    }
}
