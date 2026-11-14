using System.Collections.Generic;
using System.Linq;
using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分けのシーンの組み立て役
    /// 問題を作り、Model・View・Presenter を作ってつなぐ
    /// </summary>
    public class KanjiUsageEntryPoint : MonoBehaviour
    {
        [Tooltip("このモードの設定（名前、シーン、難易度ごとの問題数）。Data/Modes/Mode_KanjiUsage")]
        [SerializeField] private QuizModeDefinition mode;

        [Header("--- View ---")]
        [SerializeField] private QuizFrameView frameView;
        [SerializeField] private ChoiceListView choiceListView;
        [SerializeField] private KanjiUsageView kanjiUsageView;
        [Tooltip("問題の振り返りのときだけ表示する部品")]
        [SerializeField] private QuizReviewView reviewView;

        [Header("--- 出題設定 ---")]
        [Tooltip("難易度。モード選択画面などで QuizLaunchOptions.Difficulty が設定されていれば、そちらを優先する")]
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [Tooltip("難易度「簡単」の問題データ（Data/KanjiUsage/KanjiUsage_Easy）")]
        [SerializeField] private TextAsset easyQuestions;
        [Tooltip("難易度「常識」の問題データ（Data/KanjiUsage/KanjiUsage_Normal）")]
        [SerializeField] private TextAsset normalQuestions;
        [Tooltip("難易度「困難」の問題データ（Data/KanjiUsage/KanjiUsage_Hard）")]
        [SerializeField] private TextAsset hardQuestions;

        private readonly IWeakQuestionRepository weakRepository = new PlayerPrefsWeakQuestionRepository();
        private readonly IKanjiBookRepository kanjiBookRepository = new PlayerPrefsKanjiBookRepository();
        private ISceneLoader sceneLoader;
        private IAudioPlayer audioPlayer;
        private WeakQuestionList weakList;
        private QuizSession<KanjiUsageQuestion> session;
        private KanjiUsagePresenter presenter;
        private KanjiUsageReviewPresenter reviewPresenter;

        private void Start()
        {
            if (mode == null)
            {
                Debug.LogError("[KanjiUsageEntryPoint] モードの定義（mode）が設定されていません。Data/Modes/Mode_KanjiUsage を設定してください。");
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
            audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;
            sceneLoader = new SceneLoader(SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null);
            reviewView.SetVisible(false);

            // 結果画面から振り返りを頼まれていれば、出題せずに振り返りを開く
            if (QuizReviewStore.TryTakeRequest<KanjiUsageQuestion>(out var reviewQuestions, out var reviewAnswers, out int reviewIndex))
            {
                StartReview(reviewQuestions, reviewAnswers, reviewIndex);
                return;
            }

            if (QuizLaunchOptions.Difficulty.HasValue)
            {
                difficulty = QuizLaunchOptions.Difficulty.Value;
            }
            frameView.SetTitle(Title);

            var repository = new LocalQuestionRepository<KanjiUsageEntry>(GetQuestionAsset(difficulty).text);
            var entries = repository.LoadAll();

            // 苦手な言葉を先頭に並べて、そこから出題する（先に作れた問題を優先する）
            weakList = weakRepository.Load(RecordKey);
            var shuffledEntries = ShuffleUtility.ShuffledCopy(entries);
            var answers = shuffledEntries.Select(entry => entry.answer).ToList();
            var orderedAnswers = weakList.OrderCandidates(answers, mode.WeakQuestionCount(difficulty));
            var byAnswer = new Dictionary<string, KanjiUsageEntry>();
            foreach (var entry in shuffledEntries)
            {
                if (!byAnswer.ContainsKey(entry.answer)) byAnswer.Add(entry.answer, entry);
            }
            var orderedEntries = orderedAnswers.Where(byAnswer.ContainsKey).Select(answer => byAnswer[answer]);

            var questions = KanjiUsageQuestionFactory.CreateInOrder(orderedEntries, mode.QuestionCount(difficulty));
            if (questions.Count == 0)
            {
                Debug.LogError($"[KanjiUsageEntryPoint] 問題データ「{repository.SetId}」から問題を1問も作れませんでした。");
                return;
            }

            session = new QuizSession<KanjiUsageQuestion>(questions);
            presenter = new KanjiUsagePresenter(
                session, frameView, choiceListView, kanjiUsageView, audioPlayer, weakList, DisplaySettings.Shared);
            presenter.OnFinished
                .Subscribe(this, static (_, self) => self.GoToResult())
                .AddTo(this);
            presenter.Initialize();
        }

        private void StartReview(IReadOnlyList<KanjiUsageQuestion> questions, IReadOnlyList<AnswerRecord> answers, int startIndex)
        {
            // 振り返りの題名は、そのプレイの結果の名前を引き継ぐ
            frameView.SetTitle(QuizResultStore.Last != null ? QuizResultStore.Last.ModeName : Title);
            reviewPresenter = new KanjiUsageReviewPresenter(
                questions, answers, startIndex, frameView, choiceListView, reviewView, kanjiUsageView, sceneLoader, audioPlayer,
                DisplaySettings.Shared);
            reviewPresenter.Initialize();
        }

        /// <summary>画面と結果に表示する名前（例：使い分け（簡単））</summary>
        private string Title => mode.Title(difficulty);

        /// <summary>自己ベストと苦手な言葉を保存するときのキー（例：KanjiUsage_Easy）</summary>
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

        private void GoToResult()
        {
            // 振り返りに使う問題と回答を置き、結果から同じシーンを振り返りとして開けるようにする
            QuizReviewStore.Set(session.Questions, session.Answers);
            var result = QuizResult.FromSession(
                Title, mode.SceneName, session, q => q.Answer, mode.SceneName, difficulty, RecordKey);
            QuizResultStore.Set(result);

            // 間違えた言葉を苦手に加え、正解した言葉を外す
            weakList.RecordResult(result);
            weakRepository.Save(RecordKey, weakList);

            // 正解した言葉を漢字帳に集める（読み当てと同じく、言葉のまま）
            KanjiBook.Collect(kanjiBookRepository, mode.ModeId, result);

            sceneLoader.Load(SceneNames.Result);
        }
    }
}
