namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// Title シーンを開くときの設定
    /// 結果画面の [モード選択へ] などが設定し、TitleEntryPoint が読む
    /// </summary>
    public static class TitleLaunchOptions
    {
        /// <summary>タイトルを飛ばして、最初からモード選択（時間割）を出すかどうか</summary>
        public static bool OpenModeSelect { get; set; }

        /// <summary>
        /// モード選択から始めるかどうかを読み、設定を消す（1回だけ効く）
        /// </summary>
        public static bool TakeOpenModeSelect()
        {
            bool value = OpenModeSelect;
            OpenModeSelect = false;
            return value;
        }
    }
}
