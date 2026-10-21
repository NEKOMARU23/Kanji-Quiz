namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// シーンを切り替える
    /// Presenter から使い、テストでは偽物に差し替える
    /// </summary>
    public interface ISceneLoader
    {
        /// <summary>指定したシーンへ切り替える</summary>
        void Load(string sceneName);
    }
}
