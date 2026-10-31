namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 自己ベスト（最高の正解数）の保存先
    /// </summary>
    public interface IBestRecordRepository
    {
        /// <summary>保存した最高の正解数を読む</summary>
        /// <param name="key">モードと難易度を表すキー（例：StrokeOrder_Easy）</param>
        /// <returns>記録があれば true</returns>
        bool TryLoad(string key, out int bestCorrectCount);

        /// <summary>最高の正解数を保存する</summary>
        void Save(string key, int bestCorrectCount);

        /// <summary>記録を消す（記録がなければ何もしない）</summary>
        void Delete(string key);
    }
}
