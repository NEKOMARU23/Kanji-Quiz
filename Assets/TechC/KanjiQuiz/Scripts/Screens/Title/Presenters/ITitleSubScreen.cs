using System;
using R3;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトルのメニューから開く画面（設定・漢字帳・クレジット）
    /// 閉じるとタイトルに戻る（ロゴは書き直さない）
    /// </summary>
    public interface ITitleSubScreen : IDisposable
    {
        /// <summary>画面が閉じられたとき</summary>
        Observable<Unit> OnClosed { get; }

        /// <summary>入力の購読などの準備をする（最初に1回だけ呼ぶ）</summary>
        void Initialize();

        /// <summary>画面を開く</summary>
        void Open();
    }
}
