using System;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分けの選択肢1つ。言葉と、その意味（振り返りで出す）
    /// </summary>
    public readonly struct KanjiUsageChoice
    {
        /// <summary>選択肢に出す言葉（例：回答）</summary>
        public string Text { get; }

        /// <summary>言葉の意味。漢字には {漢字|よみ} で読みがついている。なければ空</summary>
        public string Meaning { get; }

        public KanjiUsageChoice(string text, string meaning)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Meaning = meaning ?? string.Empty;
        }
    }
}
