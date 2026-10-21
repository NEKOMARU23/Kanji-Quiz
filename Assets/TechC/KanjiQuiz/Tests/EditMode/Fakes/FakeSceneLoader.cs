using System.Collections.Generic;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のシーン切り替え。読み込もうとしたシーンの名前を記録する
    /// </summary>
    public class FakeSceneLoader : ISceneLoader
    {
        public List<string> LoadedScenes { get; } = new List<string>();

        public void Load(string sceneName) => LoadedScenes.Add(sceneName);
    }
}
