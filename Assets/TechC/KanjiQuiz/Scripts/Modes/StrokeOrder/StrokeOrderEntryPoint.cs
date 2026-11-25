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
        [Tooltip("このモードの設定（名前、シーン、難易度ごとの問題数）。Data/Modes/Mode_StrokeOrder")]
        [SerializeField] private QuizModeDefinition mode;

        [Header("--- View ---")]
        [SerializeField] private QuizFrameView frameView;
        [SerializeField] private ChoiceListView choiceListView;
        [SerializeField] private StrokeOrderView strokeOrderView;
        [Tooltip("問題の振り返りのときだけ表示する部品")]
        [SerializeField] private QuizReviewView reviewView;

        [Header("--- 出題設定 ---")]
        [Tooltip("難易度。モード選択画面などで QuizLaunchOptions.Difficulty が設定されていれば、そちらを優先する")]
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [Tooltip("難易度「簡単」の出題範囲（小学漢字）")]
        [SerializeField] private TextAsset easyKanjiSet;
        [Tooltip("難易度「常識」の出題範囲（小学校で学ばない常用漢字のうち、よく使う半分。KanjiSet_JoyoCommon）")]
        [SerializeField] private TextAsset normalKanjiSet;
        [Tooltip("難易度「困難」の出題範囲（小学校で学ばない常用漢字のうち、あまり使わない半分。KanjiSet_JoyoRare）")]
        [SerializeField] private TextAsset hardKanjiSet;
        [Range(2, 4)]
        [SerializeField] private int choiceCount = 3;

        private readonly IWeakQuestionRepository weakRepository = new PlayerPrefsWeakQuestionRepository();
        private readonly IKanjiBookRepository kanjiBookRepository = new PlayerPrefsKanjiBookRepository();
        private ISceneLoader sceneLoader;
        private IAudioPlayer audioPlayer;
        private WeakQuestionList weakList;
        private QuizSession<StrokeOrderQuestion> session;
        private StrokeOrderPresenter presenter;
        private StrokeOrderReviewPresenter reviewPresenter;

        private void Start()
        {
            if (mode == null)
            {
                Debug.LogError("[StrokeOrderEntryPoint] モードの定義（mode）が設定されていません。Data/Modes/Mode_StrokeOrder を設定してください。");
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
            IBgmPlayer bgmPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullBgmPlayer.Instance;
            bgmPlayer.PlayBgm(BgmType.Quiz);
            sceneLoader = new SceneLoader(SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null);

            // 結果画面から振り返りを頼まれていれば、出題せずに振り返りを開く
            if (QuizReviewStore.TryTakeRequest<StrokeOrderQuestion>(out var reviewQuestions, out var reviewAnswers, out int reviewIndex))
            {
                StartReview(reviewQuestions, reviewAnswers, reviewIndex);
                return;
            }

            if (QuizLaunchOptions.Difficulty.HasValue)
            {
                difficulty = QuizLaunchOptions.Difficulty.Value;
            }
            frameView.SetTitle(Title);

            var kanjiSet = KanjiSet.FromJson(GetKanjiSetAsset(difficulty).text);
            var factory = new StrokeOrderQuestionFactory(new StreamingAssetsKanjiStrokeSource());

            // 前に間違えた漢字を優先して出す
            weakList = weakRepository.Load(RecordKey);
            var candidates = weakList.OrderCandidates(kanjiSet.Kanji, mode.WeakQuestionCount(difficulty));
            List<StrokeOrderQuestion> questions;
            try
            {
                questions = await factory.CreateFromOrderedAsync(candidates, difficulty, choiceCount, mode.QuestionCount(difficulty), ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (questions.Count == 0)
            {
                Debug.LogError($"[StrokeOrderEntryPoint] 出題範囲「{kanjiSet.Title}」から問題を1問も作れませんでした。");
                return;
            }

            session = new QuizSession<StrokeOrderQuestion>(questions);
            presenter = new StrokeOrderPresenter(session, frameView, choiceListView, strokeOrderView, audioPlayer, weakList);
            presenter.OnFinished
                .Subscribe(this, static (_, self) => self.GoToResult())
                .AddTo(this);
            presenter.Initialize();
        }

        /// <summary>画面と結果に表示する名前（例：書き順クイズ（簡単））</summary>
        private string Title => mode.Title(difficulty);

        /// <summary>自己ベストと苦手な漢字を保存するときのキー（例：StrokeOrder_Easy）</summary>
        private string RecordKey => mode.RecordKey(difficulty);

        private TextAsset GetKanjiSetAsset(Difficulty value)
        {
            switch (value)
            {
                case Difficulty.Normal: return normalKanjiSet;
                case Difficulty.Hard: return hardKanjiSet;
                default: return easyKanjiSet;
            }
        }

        private void StartReview(IReadOnlyList<StrokeOrderQuestion> questions, IReadOnlyList<AnswerRecord> answers, int startIndex)
        {
            // 振り返りの難易度は、そのプレイの結果の名前から引き継ぐ
            frameView.SetTitle(QuizResultStore.Last != null ? QuizResultStore.Last.ModeName : mode.DisplayName);

            reviewPresenter = new StrokeOrderReviewPresenter(
                questions, answers, startIndex, frameView, choiceListView, reviewView, strokeOrderView, sceneLoader, audioPlayer);
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

            // 正解した漢字を漢字帳に集める
            KanjiBook.Collect(kanjiBookRepository, mode.ModeId, result);

            sceneLoader.Load(SceneNames.Result);
        }
    }
}
