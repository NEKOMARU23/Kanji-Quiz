using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Screens.Result
{
    /// <summary>
    /// 結果画面のシーンの組み立て役
    /// </summary>
    public class ResultEntryPoint : MonoBehaviour
    {
        [SerializeField] private ResultView resultView;

        private ResultPresenter presenter;

        private void Start()
        {
            presenter = new ResultPresenter(QuizResultStore.Last, resultView, new SceneLoader());
            presenter.Initialize();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }
    }
}
