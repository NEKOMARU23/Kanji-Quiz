namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// BGM を鳴らす。効果音の IAudioPlayer とは分けてある（BGM を使わない Presenter は知らなくてよい）
    /// </summary>
    public interface IBgmPlayer
    {
        /// <summary>
        /// BGM を流す。すでにその曲が流れている（流そうとしている）ときは何もしない
        /// ほかの曲が流れているときは、フェードアウトしてから切り替える
        /// </summary>
        void PlayBgm(BgmType type);

        /// <summary>BGM をフェードアウトして止める。流れていなければ何もしない</summary>
        void StopBgm();

        /// <summary>BGM の音量（0〜1）を設定する。曲ごとの音量にかけ合わせ、鳴っている曲にもすぐ反映する</summary>
        void SetBgmVolume(float volume);
    }
}
