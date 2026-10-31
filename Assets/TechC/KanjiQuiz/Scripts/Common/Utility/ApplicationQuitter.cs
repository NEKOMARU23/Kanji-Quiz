using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// アプリを終わらせる。エディタでは Play を止める
    /// </summary>
    public sealed class ApplicationQuitter : IApplicationQuitter
    {
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
