using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のシーンの切り替え。頼まれたシーンを記録し、切り替えの途中かどうかを外から決められる
    /// </summary>
    public class FakeSceneTransition : ISceneTransition
    {
        public bool IsPlaying { get; set; }

        public List<string> PlayedScenes { get; } = new List<string>();

        public void Play(string sceneName) => PlayedScenes.Add(sceneName);
    }
}
