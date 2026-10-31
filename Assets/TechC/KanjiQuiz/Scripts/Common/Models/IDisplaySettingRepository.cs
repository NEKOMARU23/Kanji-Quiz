namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 表示の設定の保存先
    /// </summary>
    public interface IDisplaySettingRepository
    {
        /// <summary>出題の字のほかの漢字をひらがなにするかどうかを読む。保存していなければ false</summary>
        bool LoadUseHiragana();

        /// <summary>出題の字のほかの漢字をひらがなにするかどうかを保存する</summary>
        void SaveUseHiragana(bool value);
    }
}
