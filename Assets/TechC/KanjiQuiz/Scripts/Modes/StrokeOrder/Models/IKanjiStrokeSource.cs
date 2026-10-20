using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 漢字の書き順データ（1画ごとのパスデータ）の読み込み口
    /// </summary>
    public interface IKanjiStrokeSource
    {
        /// <summary>
        /// 漢字の1画ごとのパスデータを書き順の順番で読み込む
        /// </summary>
        /// <returns>データがない場合は空のリスト</returns>
        UniTask<IReadOnlyList<string>> LoadStrokePathsAsync(string kanji, CancellationToken ct);
    }
}
