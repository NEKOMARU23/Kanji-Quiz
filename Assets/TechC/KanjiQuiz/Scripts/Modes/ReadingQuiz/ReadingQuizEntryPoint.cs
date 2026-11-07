using System.Collections.Generic;
using System.Linq;
using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当てのシーンの組み立て役
    /// 問題を作り、Model・View・Presenter を作ってつなぐ
    /// 振り返りは、まだ対応していない（結果画面からは開かない）
    /// </summary>
    public class ReadingQuizEntryPoint : MonoBehaviour
    {
        [Tooltip("このモードの設定（名前、シーン、難易度ごとの問題数）。Data/Modes/Mode_ReadingQuiz")]
        [SerializeField] private QuizModeDefinition mode;

        [Header("--- View ---")]
        [SerializeField] private QuizFrameView frameView;
        [SerializeField] private ReadingQuizView readingQuizView;

        [Header("--- 出題設定 ---")]
        [Tooltip("難易度。モード選択画面などで QuizLaunchOptions.Difficulty が設定されていれば、そちらを優先する")]
        [SerializeField] private Difficulty difficulty = Difficulty.Easy;
        [Tooltip("難易度「簡単」の問題データ（Data/ReadingQuiz/ReadingQuiz_Easy）")]
        [SerializeField] private TextAsset easyQuestions;
        [Tooltip("難易度「常識」の問題データ（Data/ReadingQuiz/ReadingQuiz_Normal）")]
        [SerializeField] private TextAsset normalQuestions;
        [Tooltip("難易度「困難」の問題データ（Data/ReadingQuiz/ReadingQuiz_Hard）")]
        [SerializeField] private TextAsset hardQuestions;

        private readonly IWeakQuestionRepository weakRepository = new PlayerPrefsWeakQuestionRepository();
        private readonly IKanjiBookRepository kanjiBookRepository = new PlayerPrefsKanjiBookRepository();
        private ISceneLoader sceneLoader;
        private IAudioPlayer audioPlayer;
        private WeakQuestionList weakList;
        private QuizSession<ReadingQuizQuestion> session;
        private ReadingQuizPresenter presenter;

        private void Start()
        {
            if (mode == null)
            {
                Debug.LogError("[ReadingQuizEntryPoint] モードの定義（mode）が設定されていません。Data/Modes/Mode_ReadingQuiz を設定してください。");
                return;
            }
            Initialize();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            session?.Dispose();
        }

        private void Initialize()
        {
            audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;
            sceneLoader = new SceneLoader(SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null);

            if (QuizLaunchOptions.Difficulty.HasValue)
            {
                difficulty = QuizLaunchOptions.Difficulty.Value;
            }
            frameView.SetTitle(Title);

            var repository = new LocalQuestionRepository<ReadingQuizEntry>(GetQuestionAsset(difficulty).text);
            var entries = repository.LoadAll();

            // 苦手な熟語を先頭に並べて、そこから出題する（先に作れた問題を優先する）
            weakList = weakRepository.Load(RecordKey);
            var shuffledEntries = ShuffleUtility.ShuffledCopy(entries);
            var words = shuffledEntries.Select(entry => entry.word).ToList();
            var orderedWords = weakList.OrderCandidates(words, mode.WeakQuestionCount(difficulty));
            var byWord = new Dictionary<string, ReadingQuizEntry>();
            foreach (var entry in shuffledEntries)
            {
                if (!byWord.ContainsKey(entry.word)) byWord.Add(entry.word, entry);
            }
            var orderedEntries = orderedWords.Where(byWord.ContainsKey).Select(word => byWord[word]);

            var questions = ReadingQuizQuestionFactory.CreateInOrder(orderedEntries, difficulty, mode.QuestionCount(difficulty));
            if (questions.Count == 0)
            {
                Debug.LogError($"[ReadingQuizEntryPoint] 問題データ「{repository.SetId}」から問題を1問も作れませんでした。");
                return;
            }

            session = new QuizSession<ReadingQuizQuestion>(questions);
            presenter = new ReadingQuizPresenter(session, frameView, readingQuizView, audioPlayer);
            presenter.OnFinished
                .Subscribe(this, static (_, self) => self.GoToResult())
                .AddTo(this);
            presenter.Initialize();
        }

        /// <summary>画面と結果に表示する名前（例：読み当て（簡単））</summary>
        private string Title => mode.Title(difficulty);

        /// <summary>自己ベストと苦手な熟語を保存するときのキー（例：ReadingQuiz_Easy）</summary>
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
            // 振り返りには対応していないので、結果には振り返りのシーンを渡さない
            var result = QuizResult.FromSession(
                Title, mode.SceneName, session, q => q.Word, null, difficulty, RecordKey);
            QuizResultStore.Set(result);

            // 間違えた熟語を苦手に加え、正解した熟語を外す
            weakList.RecordResult(result);
            weakRepository.Save(RecordKey, weakList);

            // 正解した熟語を漢字帳に集める（四字熟語づくりと同じく、熟語のまま）
            KanjiBook.Collect(kanjiBookRepository, mode.ModeId, result);

            sceneLoader.Load(SceneNames.Result);
        }
    }
}
