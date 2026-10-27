using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の音量の保存先。メモリに保存する
    /// </summary>
    public class FakeVolumeRepository : IVolumeRepository
    {
        public float SeVolume { get; set; } = 1f;

        public float LoadSeVolume() => SeVolume;

        public void SaveSeVolume(float volume) => SeVolume = volume;
    }
}
