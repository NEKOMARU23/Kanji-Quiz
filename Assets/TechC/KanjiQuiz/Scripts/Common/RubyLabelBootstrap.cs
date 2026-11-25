using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// ゲームを始めるときに、画面の文字の漢字／ひらがなの切り替え（<see cref="RubyLabelView"/>）を1つ作ってつなぐ
    /// どのシーンから始めても、シーンを切り替えても、同じものが働く
    /// </summary>
    public static class RubyLabelBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var host = new GameObject(nameof(RubyLabelView));
            Object.DontDestroyOnLoad(host);
            var view = host.AddComponent<RubyLabelView>();
            // 作ったものは、ゲームを閉じるまで使う（消えるのはアプリごと）
            new RubyLabelPresenter(view, DisplaySettings.Shared);
        }
    }
}
