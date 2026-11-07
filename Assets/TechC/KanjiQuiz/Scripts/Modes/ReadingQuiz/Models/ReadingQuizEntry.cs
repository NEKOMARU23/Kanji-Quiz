using System;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての問題データ1件（Data/ReadingQuiz の JSON の questions の1つ）
    /// 正しい読みは1つ、または複数（複数回答）。まちがいの読みは、問題を作るときに使う
    /// </summary>
    [Serializable]
    public class ReadingQuizEntry
    {
        /// <summary>熟語（漢字）</summary>
        public string word;

        /// <summary>正しい読み（1つ、または複数）</summary>
        public string[] readings;

        /// <summary>まちがいの読み（問題を作るときに、必要な数だけ使う）</summary>
        public string[] wrong;

        /// <summary>よく使われる言葉かどうか</summary>
        public bool common;
    }
}
