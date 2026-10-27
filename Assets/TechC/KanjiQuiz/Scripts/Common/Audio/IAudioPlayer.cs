namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 効果音を鳴らす。Presenter はこのインターフェースを通して音を鳴らす
    /// </summary>
    public interface IAudioPlayer
    {
        /// <summary>効果音を1回鳴らす</summary>
        void PlaySe(SeType type);

        /// <summary>効果音をループで鳴らし始める。すでにループ中の音は止める</summary>
        void PlayLoop(SeType type);

        /// <summary>ループ中の効果音を止める</summary>
        void StopLoop();

        /// <summary>効果音の音量（0〜1）を設定する。種類ごとの音量にかけ合わせる</summary>
        void SetSeVolume(float volume);
    }
}
