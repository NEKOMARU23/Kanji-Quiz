using System.Collections.Generic;
using System.Text.RegularExpressions;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// プレイ中に出す意味（ヒント）を作る
    /// 意味の中に空いたマスの字があると答えが見えてしまうので、その字を含む言葉は読み（ひらがな）で出す
    /// 例：老若男女で「若」が空いていれば、「{若|わか}い{人|ひと}」は「わかい人」になる
    /// </summary>
    public static class FourCharIdiomHint
    {
        private static readonly Regex Mark = new Regex(@"\{([^{}|]+)\|([^{}|]+)\}");

        /// <summary>
        /// 空いたマスの字を隠した意味を、表示する文にする
        /// </summary>
        /// <param name="question">出題している問題</param>
        /// <param name="useHiragana">ほかの漢字もひらがなにするかどうか（表示の設定）</param>
        public static string Render(FourCharIdiomQuestion question, bool useHiragana)
        {
            var hidden = new HashSet<char>();
            foreach (int position in question.BlankPositions)
            {
                foreach (var ch in question.Characters[position]) hidden.Add(ch);
            }

            string marked = Mark.Replace(question.Meaning, m =>
            {
                foreach (var ch in m.Groups[1].Value)
                {
                    if (hidden.Contains(ch)) return m.Groups[2].Value;
                }
                return m.Value;
            });
            return RubyText.Render(marked, useHiragana);
        }
    }
}
