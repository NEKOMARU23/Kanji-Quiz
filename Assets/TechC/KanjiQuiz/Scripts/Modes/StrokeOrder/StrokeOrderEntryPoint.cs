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
        [Header("--- View ---")]
        [SerializeField] private QuizFrameView frameView;
        [SerializeField] private ChoiceListView choiceListView;
        [SerializeField] private StrokeOrderView strokeOrderView;
        [Tooltip("全問回答後に表示する画面（結果画面ができるまでの仮）")]
        [SerializeField] private GameObject finishedPanel;

        [Header("--- 出題設定 ---")]
        [Tooltip("出題する漢字のリスト。書き順データがない漢字や、難易度の条件を満たさない漢字は除外される")]
        [SerializeField] private List<string> questionKanjiList = new List<string> { "令", "漢", "字" };
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [Range(2, 4)]
        [SerializeField] private int choiceCount = 3;

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
            if (finishedPanel != null) finishedPanel.SetActive(false);

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
                .Subscribe(this, static (_, self) => self.ShowFinished())
                .AddTo(this);
            presenter.Initialize();
        }

        private void ShowFinished()
        {
            Debug.Log($"[StrokeOrderEntryPoint] 全問終了：{session.Score.CurrentValue} / {session.Count} 問正解");
            if (finishedPanel != null) finishedPanel.SetActive(true);
        }
    }
}
