using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 書き順クイズのシーンの組み立て役
    /// 問題を作り、Model・View・Presenter を作ってつなぐ
    /// </summary>
    public class StrokeOrderEntryPoint : MonoBehaviour
    {
        /// <summary>結果画面に表示するモードの名前</summary>
        public const string ModeName = "書き順クイズ";

        [Header("--- View ---")]
        [SerializeField] private QuizFrameView frameView;
        [SerializeField] private ChoiceListView choiceListView;
        [SerializeField] private StrokeOrderView strokeOrderView;

        [Header("--- 出題設定 ---")]
        [Tooltip("出題する漢字のリスト。書き順データがない漢字や、難易度の条件を満たさない漢字は除外される")]
        [SerializeField] private List<string> questionKanjiList = new List<string> { "令", "漢", "字" };
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [Range(2, 4)]
        [SerializeField] private int choiceCount = 3;

        private readonly ISceneLoader sceneLoader = new SceneLoader();
        private QuizSession<StrokeOrderQuestion> session;
        private StrokeOrderPresenter presenter;

        private void Start()
        {
            InitializeAsync(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            session?.Dispose();
        }

        private async UniTaskVoid InitializeAsync(CancellationToken ct)
        {
            var factory = new StrokeOrderQuestionFactory(new StreamingAssetsKanjiStrokeSource());
            List<StrokeOrderQuestion> questions;
            try
            {
                questions = await factory.CreateAsync(questionKanjiList, difficulty, choiceCount, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (questions.Count == 0)
            {
                Debug.LogError("[StrokeOrderEntryPoint] 出題できる問題が1問もありません。出題する漢字と難易度の設定を確認してください。");
                return;
            }

            session = new QuizSession<StrokeOrderQuestion>(questions);
            presenter = new StrokeOrderPresenter(session, frameView, choiceListView, strokeOrderView);
            presenter.OnFinished
                .Subscribe(this, static (_, self) => self.GoToResult())
                .AddTo(this);
            presenter.Initialize();
        }

        private void GoToResult()
        {
            QuizResultStore.Set(QuizResult.FromSession(ModeName, SceneNames.StrokeOrder, session, q => q.Kanji));
            sceneLoader.Load(SceneNames.Result);
        }
    }
}
