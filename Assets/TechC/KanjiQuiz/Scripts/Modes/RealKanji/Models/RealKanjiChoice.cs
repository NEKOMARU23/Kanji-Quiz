using System;
using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの選択肢1つ（本物か偽物の漢字）
    /// </summary>
    public sealed class RealKanjiChoice
    {
        /// <summary>字の画のデータ</summary>
        public IReadOnlyList<StrokeData> Strokes { get; }

        /// <summary>字の下に出す読み</summary>
        public string Reading { get; }

        /// <summary>振り返りで出す説明。本物は漢字、偽物は部品の組み合わせ（例：氵＋舎（捨の右側））</summary>
        public string Description { get; }

        public RealKanjiChoice(IReadOnlyList<StrokeData> strokes, string reading, string description)
        {
            Strokes = strokes ?? throw new ArgumentNullException(nameof(strokes));
            Reading = reading ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
