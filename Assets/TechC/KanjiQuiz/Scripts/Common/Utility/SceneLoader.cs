using UnityEngine.SceneManagement;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// シーンを切り替える。切り替えの演出があれば、演出つきで切り替える
    /// </summary>
    public sealed class SceneLoader : ISceneLoader
    {
        private readonly ISceneTransition transition;

        /// <param name="transition">切り替えの演出。null なら演出なしですぐに切り替える</param>
        public SceneLoader(ISceneTransition transition = null)
        {
            this.transition = transition;
        }

        public void Load(string sceneName)
        {
            if (transition != null)
            {
                transition.Play(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
