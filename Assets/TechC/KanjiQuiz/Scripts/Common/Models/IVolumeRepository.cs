namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 音量の設定の保存先
    /// </summary>
    public interface IVolumeRepository
    {
        /// <summary>効果音の音量（0〜1）を読む。保存していなければ 1</summary>
        float LoadSeVolume();

        /// <summary>効果音の音量（0〜1）を保存する</summary>
        void SaveSeVolume(float volume);

        /// <summary>BGM の音量（0〜1）を読む。保存していなければ、効果音より低めの値</summary>
        float LoadBgmVolume();

        /// <summary>BGM の音量（0〜1）を保存する</summary>
        void SaveBgmVolume(float volume);
    }
}
