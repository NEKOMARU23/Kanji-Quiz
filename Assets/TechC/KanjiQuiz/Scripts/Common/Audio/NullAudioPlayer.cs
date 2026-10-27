namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 何も鳴らさない IAudioPlayer。シーンに AudioManager がないときに使う
    /// </summary>
    public sealed class NullAudioPlayer : IAudioPlayer
    {
        /// <summary>共有のインスタンス</summary>
        public static readonly NullAudioPlayer Instance = new NullAudioPlayer();

        private NullAudioPlayer()
        {
        }

        public void PlaySe(SeType type)
        {
        }

        public void PlayLoop(SeType type)
        {
        }

        public void StopLoop()
        {
        }

        public void SetSeVolume(float volume)
        {
        }
    }
}
