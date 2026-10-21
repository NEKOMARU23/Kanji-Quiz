using UnityEngine.SceneManagement;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// SceneManager でシーンを切り替える
    /// </summary>
    public sealed class SceneLoader : ISceneLoader
    {
        public void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
