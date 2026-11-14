using System;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分けの問題データ1件（Data/KanjiUsage の JSON の questions の1つ）
    /// 同じ読みの言葉から、文の空いたところに合うものを選ぶ問題のもと
    /// </summary>
    [Serializable]
    public class KanjiUsageEntry
    {
        /// <summary>空いたところの読み（例：かいとう）。送り仮名も含めた言葉全体の読み</summary>
        public string reading;

        /// <summary>正解の言葉（例：回答）</summary>
        public string answer;

        /// <summary>
        /// 文。空いたところは「＿」で1か所。漢字には {漢字|よみ} で読みがついている
        /// </summary>
        public string sentence;

        /// <summary>まちがいの言葉（選択肢の数は、まちがいの数＋1になる）</summary>
        public string[] wrong;

        /// <summary>意味（正解、まちがいの順。振り返りで候補それぞれに出す）。漢字には {漢字|よみ} で読みがついている</summary>
        public string[] meanings;
    }
}
