using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしのシーンの組み立て役
    /// 問題を作り、Model・View・Presenter を作ってつなぐ
    /// </summary>
    public class RealKanjiEntryPoint : MonoBehaviour
    {
        [Tooltip("このモードの設定（名前、シーン、難易度ごとの問題数）。Data/Modes/Mode_RealKanji")]
        [SerializeField] private QuizModeDefinition mode;

        [Header("--- View ---")]
        [SerializeField] private QuizFrameView frameView;
        [SerializeField] private ChoiceListView choiceListView;
        [SerializeField] private RealKanjiView realKanjiView;
        [Tooltip("問題の振り返りのときだけ表示する部品")]
        [SerializeField] private QuizReviewView reviewView;

        [Header("--- 出題設定 ---")]
        [Tooltip("難易度。モード選択画面などで QuizLaunchOptions.Difficulty が設定されていれば、そちらを優先する")]
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [Tooltip("難易度「簡単」の問題データ（Data/RealKanji/RealKanji_Elementary）")]
        [SerializeField] private TextAsset easyQuestions;
        [Tooltip("難易度「常識」の問題データ（Data/RealKanji/RealKanji_Joyo）")]
        [SerializeField] private TextAsset normalQuestions;
        [Tooltip("難易度「困難」の問題データ（Data/RealKanji/RealKanji_Hyogai）")]
        [SerializeField] private TextAsset hardQuestions;
        [Tooltip("振り返りで出す使用例（Data/KanjiWords の3つ）")]
        [SerializeField] private TextAsset[] wordAssets;
        [Range(2, 4)]
        [SerializeField] private int choiceCount = 4;

        private readonly IWeakQuestionRepository weakRepository = new PlayerPrefsWeakQuestionRepository();
        private ISceneLoader sceneLoader;
        private IAudioPlayer audioPlayer;
        private WeakQuestionList weakList;
        private QuizSession<RealKanjiQuestion> session;
        private RealKanjiPresenter presenter;
        private RealKanjiReviewPresenter reviewPresenter;

        private void Start()
        {
            if (mode == null)
            {
                Debug.LogError("[RealKanjiEntryPoint] モードの定義（mode）が設定されていません。Data/Modes/Mode_RealKanji を設定してください。");
                return;
            }
            InitializeAsync(destroyCancellationToken).Forget();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            reviewPresenter?.Dispose();
            session?.Dispose();
        }

        private async UniTaskVoid InitializeAsync(CancellationToken ct)
        {
            reviewView.SetVisible(false);
            audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;
            sceneLoader = new SceneLoader(SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null);

            // 結果画面から振り返りを頼まれていれば、出題せずに振り返りを開く
            if (QuizReviewStore.TryTakeRequest<RealKanjiQuestion>(out var reviewQuestions, out var reviewAnswers, out int reviewIndex))
            {
                StartReview(reviewQuestions, reviewAnswers, reviewIndex);
                return;
            }

            if (QuizLaunchOptions.Difficulty.HasValue)
            {
                difficulty = QuizLaunchOptions.Difficulty.Value;
            }
            frameView.SetTitle(Title);

            var repository = new LocalQuestionRepository<RealKanjiEntry>(GetQuestionAsset(difficulty).text);
            var factory = new RealKanjiQuestionFactory(new StreamingAssetsKanjiStrokeSource(), repository.LoadAll(), LoadWords());

            // 前に間違えた漢字を優先して出す
            weakList = weakRepository.Load(RecordKey);
            var candidates = weakList.OrderCandidates(factory.Candidates, mode.WeakQuestionCount(difficulty));
            List<RealKanjiQuestion> questions;
            try
            {
                questions = await factory.CreateFromOrderedAsync(candidates, choiceCount, mode.QuestionCount(difficulty), ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (questions.Count == 0)
            {
                Debug.LogError($"[RealKanjiEntryPoint] 問題データ「{repository.SetId}」から問題を1問も作れませんでした。");
                return;
            }

            session = new QuizSession<RealKanjiQuestion>(questions);
            presenter = new RealKanjiPresenter(session, frameView, choiceListView, realKanjiView, audioPlayer, weakList);
            presenter.OnFinished
                .Subscribe(this, static (_, self) => self.GoToResult())
                .AddTo(this);
            presenter.Initialize();
        }

        /// <summary>画面と結果に表示する名前（例：本物の漢字さがし（簡単））</summary>
        private string Title => mode.Title(difficulty);

        /// <summary>自己ベストと苦手な漢字を保存するときのキー（例：RealKanji_Easy）</summary>
        private string RecordKey => mode.RecordKey(difficulty);

        private TextAsset GetQuestionAsset(Difficulty value)
        {
            switch (value)
            {
                case Difficulty.Normal: return normalQuestions;
                case Difficulty.Hard: return hardQuestions;
                default: return easyQuestions;
            }
        }

        private KanjiWordDictionary LoadWords()
        {
            var jsons = new List<string>();
            foreach (var asset in wordAssets ?? Array.Empty<TextAsset>())
            {
                if (asset != null) jsons.Add(asset.text);
            }
            return KanjiWordDictionary.FromJson(jsons.ToArray());
        }

        private void StartReview(IReadOnlyList<RealKanjiQuestion> questions, IReadOnlyList<AnswerRecord> answers, int startIndex)
        {
            // 振り返りの難易度は、そのプレイの結果の名前から引き継ぐ
            frameView.SetTitle(QuizResultStore.Last != null ? QuizResultStore.Last.ModeName : mode.DisplayName);

            reviewPresenter = new RealKanjiReviewPresenter(
                questions, answers, startIndex, frameView, choiceListView, reviewView, realKanjiView, sceneLoader, audioPlayer);
            reviewPresenter.Initialize();
        }

        private void GoToResult()
        {
            QuizReviewStore.Set(session.Questions, session.Answers);
            var result = QuizResult.FromSession(
                Title, mode.SceneName, session, q => q.Kanji, mode.SceneName, difficulty, RecordKey);
            QuizResultStore.Set(result);

            // 間違えた漢字を苦手に加え、正解した漢字を外す
            weakList.RecordResult(result);
            weakRepository.Save(RecordKey, weakList);

            sceneLoader.Load(SceneNames.Result);
        }
    }
}
