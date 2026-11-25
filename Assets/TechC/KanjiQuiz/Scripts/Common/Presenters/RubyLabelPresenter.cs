using System;
using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 「出題の字のほかは ひらがなで表示」の設定を、画面の文字に伝える Presenter
    /// 設定画面と中断メニューのどちらで切り替えても、すぐに画面の文字が変わる
    /// </summary>
    public sealed class RubyLabelPresenter : IDisposable
    {
        private readonly IDisposable subscription;

        public RubyLabelPresenter(IRubyLabelView view, DisplaySettings displaySettings)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (displaySettings == null) throw new ArgumentNullException(nameof(displaySettings));

            // 購読したときに今の設定も届くので、シーンを開いた最初から合っている
            subscription = displaySettings.UseHiragana.Subscribe(view, static (value, v) => v.SetHiragana(value));
        }

        public void Dispose() => subscription.Dispose();
    }
}
