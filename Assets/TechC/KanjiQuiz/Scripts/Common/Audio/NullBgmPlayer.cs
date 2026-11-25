namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 何も鳴らさない IBgmPlayer。シーンに AudioManager がないときに使う
    /// </summary>
    public sealed class NullBgmPlayer : IBgmPlayer
    {
        /// <summary>共有のインスタンス</summary>
        public static readonly NullBgmPlayer Instance = new NullBgmPlayer();

        private NullBgmPlayer()
        {
        }

        public void PlayBgm(BgmType type)
        {
        }

        public void StopBgm()
        {
        }

        public void SetBgmVolume(float volume)
        {
        }
    }
}
