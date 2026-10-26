namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 苦手な問題のリストの保存先
    /// </summary>
    public interface IWeakQuestionRepository
    {
        /// <summary>保存したリストを読む。なければ空のリストを返す</summary>
        /// <param name="key">モードと難易度を表すキー（例：StrokeOrder_Easy）</param>
        WeakQuestionList Load(string key);

        /// <summary>リストを保存する</summary>
        void Save(string key, WeakQuestionList list);
    }
}
