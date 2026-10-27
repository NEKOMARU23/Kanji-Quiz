using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の書き順データ。漢字ごとに画数だけを決め、単純な直線のパスを返す
    /// </summary>
    public class FakeKanjiStrokeSource : IKanjiStrokeSource
    {
        private readonly Dictionary<string, int> strokeCounts;

        public FakeKanjiStrokeSource(Dictionary<string, int> strokeCounts)
        {
            this.strokeCounts = strokeCounts;
        }

        public UniTask<IReadOnlyList<string>> LoadStrokePathsAsync(string kanji, CancellationToken ct)
        {
            if (!strokeCounts.TryGetValue(kanji, out int count))
            {
                return UniTask.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            var paths = new List<string>();
            for (int i = 0; i < count; i++)
            {
                paths.Add($"M{10 + i},10L{10 + i},90");
            }
            return UniTask.FromResult<IReadOnlyList<string>>(paths);
        }
    }
}
