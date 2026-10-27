using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の、部品のグループつきの書き順データ。漢字ごとに画を登録し、読み込んだ回数を数える
    /// </summary>
    public class FakeKanjiSvgSource : IKanjiSvgSource
    {
        private readonly Dictionary<string, List<KanjiSvgStroke>> strokes = new Dictionary<string, List<KanjiSvgStroke>>();

        /// <summary>漢字ごとの読み込んだ回数</summary>
        public Dictionary<string, int> LoadCounts { get; } = new Dictionary<string, int>();

        /// <summary>
        /// 1つのグループに入った画を登録する。画は (x1, y1) から (x2, y2) への直線（KanjiVG の座標）
        /// </summary>
        public FakeKanjiSvgSource Add(string kanji, string groupId, float x1, float y1, float x2, float y2)
        {
            if (!strokes.TryGetValue(kanji, out var list))
            {
                list = new List<KanjiSvgStroke>();
                strokes.Add(kanji, list);
            }
            list.Add(new KanjiSvgStroke($"M{x1},{y1}L{x2},{y2}", new[] { $"kvg:{kanji}", groupId }));
            return this;
        }

        public UniTask<IReadOnlyList<KanjiSvgStroke>> LoadStrokesAsync(string kanji, CancellationToken ct)
        {
            LoadCounts[kanji] = LoadCounts.TryGetValue(kanji, out int count) ? count + 1 : 1;
            if (!strokes.TryGetValue(kanji, out var list))
            {
                return UniTask.FromResult<IReadOnlyList<KanjiSvgStroke>>(Array.Empty<KanjiSvgStroke>());
            }
            return UniTask.FromResult<IReadOnlyList<KanjiSvgStroke>>(list);
        }
    }
}
