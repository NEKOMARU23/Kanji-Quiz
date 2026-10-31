using System;
using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 表示の設定（出題の字のほかの漢字をひらがなにするか）
    /// 中断メニューで切り替え、振り返りの意味と例文が読む。シーンをまたいで同じものを使うので、ふだんは Shared を使う
    /// </summary>
    public sealed class DisplaySettings
    {
        private static DisplaySettings shared;

        private readonly IDisplaySettingRepository repository;
        private readonly ReactiveProperty<bool> useHiragana;

        /// <summary>ゲーム全体で使う設定（PlayerPrefs に保存する）</summary>
        public static DisplaySettings Shared => shared ??= new DisplaySettings(new PlayerPrefsDisplaySettingRepository());

        /// <summary>出題の字のほかの漢字をひらがなにするかどうか。変わったら通知する</summary>
        public ReadOnlyReactiveProperty<bool> UseHiragana => useHiragana;

        /// <param name="repository">設定の保存先。作るときに保存した値を読む</param>
        public DisplaySettings(IDisplaySettingRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            useHiragana = new ReactiveProperty<bool>(repository.LoadUseHiragana());
        }

        /// <summary>ひらがなにするかどうかを変えて保存する</summary>
        public void SetUseHiragana(bool value)
        {
            if (useHiragana.Value == value) return;
            useHiragana.Value = value;
            repository.SaveUseHiragana(value);
        }
    }
}
