namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// アプリを終わらせる
    /// Presenter から使い、テストでは偽物に差し替える
    /// </summary>
    public interface IApplicationQuitter
    {
        /// <summary>アプリを終わらせる（エディタでは Play を止める）</summary>
        void Quit();
    }
}
