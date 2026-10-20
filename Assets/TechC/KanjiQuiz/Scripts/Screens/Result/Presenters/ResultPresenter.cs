using System;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面の Presenter
    /// </summary>
    public sealed class ResultPresenter : IDisposable
    {
        private readonly QuizResult result;
        private readonly IResultView view;
        private readonly ISceneLoader sceneLoader;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        /// <param name="result">表示する結果。直接このシーンを開いた場合は null</param>
        public ResultPresenter(QuizResult result, IResultView view, ISceneLoader sceneLoader)
        {
            this.result = result;
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        }

        /// <summary>
        /// 結果を表示し、ボタンの購読を始める
        /// </summary>
        public void Initialize()
        {
            if (result == null)
            {
                view.ShowNoResult();
                return;
            }

            view.SetModeName(result.ModeName);
            view.SetScore(result.CorrectCount, result.Total);
            view.SetItems(result.Items);

            view.OnRetryClicked
                .Subscribe(this, static (_, self) => self.sceneLoader.Load(self.result.RetrySceneName))
                .AddTo(disposables);
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
