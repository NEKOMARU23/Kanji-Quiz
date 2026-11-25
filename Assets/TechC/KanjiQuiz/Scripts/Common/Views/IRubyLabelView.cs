namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 画面の文字の、漢字／ひらがなの切り替え
    /// 読みの印（{漢字|よみ}）がついた文字を、画面全体で見つけて書き直す
    /// </summary>
    public interface IRubyLabelView
    {
        /// <summary>印がついた文字を、ひらがなにするか、漢字のままにするかを切り替える</summary>
        void SetHiragana(bool useHiragana);
    }
}
