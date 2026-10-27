using System.Collections.Generic;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 問題データの読み込み口
    /// フェーズ1は同梱の JSON（LocalQuestionRepository）。フェーズ2でサーバーやプレイヤーが作った問題に差し替えられるようにする
    /// </summary>
    /// <typeparam name="TEntry">問題データ1件の型（JSON の questions の要素）</typeparam>
    public interface IQuestionRepository<TEntry>
    {
        /// <summary>問題データをすべて読み込む</summary>
        IReadOnlyList<TEntry> LoadAll();
    }
}
