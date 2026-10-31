using System.Collections.Generic;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトルのメニューから開く画面のまとめ。null の画面は、メニューのボタンを押しても開かない
    /// </summary>
    public sealed class TitleSubScreens
    {
        /// <summary>何も開かない（テスト用など）</summary>
        public static TitleSubScreens None => new TitleSubScreens();

        /// <summary>設定（タイトルの黒板を消して板書する）</summary>
        public ITitleSubScreen Settings { get; }

        /// <summary>漢字帳（タイトルの上に重ねる）</summary>
        public ITitleSubScreen KanjiBook { get; }

        /// <summary>クレジット（タイトルの上に重ねる）</summary>
        public ITitleSubScreen Credits { get; }

        public TitleSubScreens(ITitleSubScreen settings = null, ITitleSubScreen kanjiBook = null, ITitleSubScreen credits = null)
        {
            Settings = settings;
            KanjiBook = kanjiBook;
            Credits = credits;
        }

        /// <summary>null でない画面をすべて返す</summary>
        public IEnumerable<ITitleSubScreen> All()
        {
            if (Settings != null) yield return Settings;
            if (KanjiBook != null) yield return KanjiBook;
            if (Credits != null) yield return Credits;
        }
    }
}
