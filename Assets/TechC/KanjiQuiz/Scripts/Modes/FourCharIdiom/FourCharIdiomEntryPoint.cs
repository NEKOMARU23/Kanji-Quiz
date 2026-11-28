using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 四字熟語づくりのシーンの組み立て役
    /// 問題を作り、Model・View・Presenter を作ってつなぐ
    /// </summary>
    public class FourCharIdiomEntryPoint : MonoBehaviour
    {
        [Tooltip("このモードの設定（名前、シーン、難易度ごとの問題数）。Data/Modes/Mode_FourCharIdiom")]
        [SerializeField] private QuizModeDefinition mode;

        [Header("--- View ---")]
        [SerializeField] private QuizFrameView frameView;
        [SerializeField] private FourCharIdiomView fourCharIdiomView;
        [Tooltip("問題の振り返りのときだけ表示する部品")]
        [SerializeField] private QuizReviewView reviewView;

        [Header("--- 出題設定 ---")]
        [Tooltip("難易度。モード選択画面などで QuizLaunchOptions.Difficulty が設定されていれば、そちらを優先する")]
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [Tooltip("難易度「簡単」の問題データ（Data/FourCharIdiom/FourCharIdiom_Easy）")]
        [SerializeField] private TextAsset easyQuestions;
        [Tooltip("難易度「常識」の問題データ（Data/FourCharIdiom/FourCharIdiom_Normal）。まだなければ空でよい")]
        [SerializeField] private TextAsset normalQuestions;
        [Tooltip("難易度「困難」の問題データ（Data/FourCharIdiom/FourCharIdiom_Hard）。まだなければ空でよい")]
        [SerializeField] private TextAsset hardQuestions;

        private readonly IWeakQuestionRepository weakRepository = new PlayerPrefsWeakQuestionRepository();
        private readonly IKanjiBookRepository kanjiBookRepository = new PlayerPrefsKanjiBookRepository();
        private ISceneLoader sceneLoader;
        private IAudioPlayer audioPlayer;
        private WeakQuestionList weakList;
        private QuizSession<FourCharIdiomQuestion> session;
        private FourCharIdiomPresenter presenter;
        private FourCharIdiomReviewPresenter reviewPresenter;

        private void Start()
        {
            if (mode == null)
            {
                Debug.LogError("[FourCharIdiomEntryPoint] モードの定義（mode）が設定されていません。Data/Modes/Mode_FourCharIdiom を設定してください。");
                return;
            }
            Initialize();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            reviewPresenter?.Dispose();
            session?.Dispose();
        }

        private void Initialize()
        {
            reviewView.SetVisible(false);
            audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;
            IBgmPlayer bgmPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullBgmPlayer.Instance;
            bgmPlayer.PlayBgm(BgmType.Quiz);
            sceneLoader = new SceneLoader(SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null);

            // 結果画面から振り返りを頼まれていれば、出題せずに振り返りを開く
            if (QuizReviewStore.TryTakeRequest<FourCharIdiomQuestion>(out var reviewQuestions, out var reviewAnswers, out int reviewIndex))
            {
                StartReview(reviewQuestions, reviewAnswers, reviewIndex);
                return;
            }

            if (QuizLaunchOptions.Difficulty.HasValue)
            {
                difficulty = QuizLaunchOptions.Difficulty.Value;
            }
            frameView.SetTitle(Title);

            var asset = GetQuestionAsset(difficulty);
            if (asset == null)
            {
                Debug.LogError($"[FourCharIdiomEntryPoint] 難易度「{difficulty.ToDisplayName()}」の問題データが設定されていません。");
                return;
            }
            var repository = new LocalQuestionRepository<FourCharIdiomEntry>(asset.text);
            var factory = new FourCharIdiomQuestionFactory(repository.LoadAll());

            // 前に間違えた熟語を優先して出す
            weakList = weakRepository.Load(RecordKey);
            var candidates = weakList.OrderCandidates(factory.Candidates, mode.WeakQuestionCount(difficulty));
            var questions = factory.CreateFromOrdered(candidates, FourCharIdiomRule.For(difficulty), mode.QuestionCount(difficulty));
            if (questions.Count == 0)
            {
                Debug.LogError($"[FourCharIdiomEntryPoint] 問題データ「{repository.SetId}」から問題を1問も作れませんでした。");
                return;
            }

            session = new QuizSession<FourCharIdiomQuestion>(questions);
            presenter = new FourCharIdiomPresenter(session, frameView, fourCharIdiomView, audioPlayer, weakList, DisplaySettings.Shared);
            presenter.OnFinished
                .Subscribe(this, static (_, self) => self.GoToResult())
                .AddTo(this);
            presenter.Initialize();
        }

        /// <summary>画面と結果に表示する名前（例：四字熟語づくり（簡単））</summary>
        private string Title => mode.Title(difficulty);

        /// <summary>自己ベストと苦手な熟語を保存するときのキー（例：FourCharIdiom_Easy）</summary>
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

        private void StartReview(IReadOnlyList<FourCharIdiomQuestion> questions, IReadOnlyList<AnswerRecord> answers, int startIndex)
        {
            // 振り返りの難易度は、そのプレイの結果の名前から引き継ぐ
            frameView.SetTitle(QuizResultStore.Last != null ? QuizResultStore.Last.ModeName : mode.DisplayName);

            reviewPresenter = new FourCharIdiomReviewPresenter(
                questions, answers, startIndex, frameView, reviewView, fourCharIdiomView, sceneLoader, audioPlayer,
                DisplaySettings.Shared);
            reviewPresenter.Initialize();
        }

        private void GoToResult()
        {
            var recorder = new QuizResultRecorder(weakRepository, kanjiBookRepository);
            recorder.Record(mode, difficulty, session, q => q.Word, weakList);

            sceneLoader.Load(SceneNames.Result);
        }
    }
}
