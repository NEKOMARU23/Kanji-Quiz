using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 漢字の書き順データを、部品のグループの情報つきで読み込む口
    /// 部品を入れ替えて字を組み立てるとき（本物の漢字さがしの偽物）に使う
    /// </summary>
    public interface IKanjiSvgSource
    {
        /// <summary>
        /// 漢字の1画ごとのデータを書き順の順番で読み込む
        /// </summary>
        /// <returns>データがない場合は空のリスト</returns>
        UniTask<IReadOnlyList<KanjiSvgStroke>> LoadStrokesAsync(string kanji, CancellationToken ct);
    }
}
