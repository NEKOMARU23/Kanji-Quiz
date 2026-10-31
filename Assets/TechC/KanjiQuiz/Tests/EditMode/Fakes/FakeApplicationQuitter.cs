using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のアプリの終わらせ方。呼ばれた回数を数える
    /// </summary>
    public class FakeApplicationQuitter : IApplicationQuitter
    {
        public int QuitCount { get; private set; }

        public void Quit() => QuitCount++;
    }
}
