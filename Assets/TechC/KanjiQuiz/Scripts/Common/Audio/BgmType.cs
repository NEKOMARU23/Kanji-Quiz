namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// BGM の種類。画面ごとに変えず、メニューとクイズの2曲だけにする
    /// </summary>
    public enum BgmType
    {
        /// <summary>タイトル・モード選択・漢字帳など、メニューの画面</summary>
        Menu,

        /// <summary>クイズ中と振り返り</summary>
        Quiz,
    }
}
