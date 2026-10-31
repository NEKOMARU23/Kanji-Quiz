using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の表示の設定の保存先。メモリに保存し、保存した回数を数える
    /// </summary>
    public class FakeDisplaySettingRepository : IDisplaySettingRepository
    {
        public bool UseHiragana { get; set; }
        public int SaveCount { get; private set; }

        public bool LoadUseHiragana() => UseHiragana;

        public void SaveUseHiragana(bool value)
        {
            UseHiragana = value;
            SaveCount++;
        }
    }
}
