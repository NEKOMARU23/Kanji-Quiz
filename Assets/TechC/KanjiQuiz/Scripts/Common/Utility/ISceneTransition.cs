namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 演出つきでシーンを切り替える
    /// </summary>
    public interface ISceneTransition
    {
        /// <summary>切り替えの途中かどうか。次のシーンが見えきるまで true</summary>
        bool IsPlaying { get; }

        /// <summary>
        /// 演出つきでシーンを読み込み、新しいシーンを見せる
        /// 切り替えの途中で呼ばれたら何もしない
        /// </summary>
        void Play(string sceneName);
    }
}
