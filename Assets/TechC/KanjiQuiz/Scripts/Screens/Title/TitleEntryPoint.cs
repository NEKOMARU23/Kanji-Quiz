using System.Collections.Generic;
using System.Linq;
using R3;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// Title シーン（タイトル画面とモード選択画面）の組み立て役
    /// </summary>
    public class TitleEntryPoint : MonoBehaviour
    {
        [SerializeField] private TitleView titleView;
        [SerializeField] private ModeSelectView modeSelectView;
        [SerializeField] private SettingsView settingsView;
        [SerializeField] private KanjiBookView kanjiBookView;
        [SerializeField] private CreditsView creditsView;
        [Tooltip("時間割に並べるモードの一覧（Data/Modes/ModeCatalog）")]
        [SerializeField] private QuizModeCatalog modeCatalog;
        [Tooltip("黒板にチョークで書くロゴの字")]
        [SerializeField] private string logoText = "漢字クイズ";

        [Header("--- 漢字帳 ---")]
        [Tooltip("タブに並べる出題範囲（Data/KanjiSets。小学漢字・常用漢字（よく使う）・常用漢字（あまり使わない））")]
        [SerializeField] private TextAsset[] kanjiSetAssets;
        [Tooltip("読み（Data/RealKanji の問題データ）")]
        [SerializeField] private TextAsset[] readingAssets;
        [Tooltip("意味と例文（Data/KanjiMeanings）")]
        [SerializeField] private TextAsset[] meaningAssets;
        [Tooltip("使用例の熟語（Data/KanjiWords）")]
        [SerializeField] private TextAsset[] wordAssets;
        [Tooltip("四字熟語のページに並べる熟語（Data/FourCharIdiom の Easy・Normal・Hard。この順に並べる）")]
        [SerializeField] private TextAsset[] idiomAssets;

        private TitlePresenter presenter;

        private void Start()
        {
            IAudioPlayer audioPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullAudioPlayer.Instance;
            IBgmPlayer bgmPlayer = AudioManager.HasInstance ? AudioManager.Instance : NullBgmPlayer.Instance;
            bgmPlayer.PlayBgm(BgmType.Menu);
            ISceneTransition transition = SceneTransitionView.HasInstance ? SceneTransitionView.Instance : null;

            // 前のプレイの難易度が残らないように消しておく（モード選択で選び直す）
            QuizLaunchOptions.Clear();

            var bestRecords = new PlayerPrefsBestRecordRepository();
            var weakQuestions = new PlayerPrefsWeakQuestionRepository();
            var modeSelect = new ModeSelectPresenter(
                ModeSummary.Load(modeCatalog.Modes, bestRecords, weakQuestions), modeSelectView, new SceneLoader(transition), audioPlayer);

            var kanjiBookRepository = new PlayerPrefsKanjiBookRepository();
            var strokeSource = new StreamingAssetsKanjiStrokeSource();
            var settings = new SettingsPresenter(
                settingsView, audioPlayer, new PlayerPrefsVolumeRepository(), DisplaySettings.Shared,
                new QuizRecordEraser(modeCatalog.Modes, bestRecords, weakQuestions, kanjiBookRepository), bgmPlayer);
            // 記録を消したら、時間割と黒板の記録を読み直す
            settings.OnRecordsErased
                .Subscribe(_ => modeSelect.ReloadModes(ModeSummary.Load(modeCatalog.Modes, bestRecords, weakQuestions)))
                .AddTo(this);
            var kanjiBook = new KanjiBookPresenter(
                kanjiBookView, audioPlayer, kanjiSetAssets.Select(a => KanjiSet.FromJson(a.text)).ToList(), modeCatalog.Modes,
                () => KanjiBook.Load(kanjiBookRepository, modeCatalog.Modes.Select(m => m.ModeId)),
                LoadKanjiInfo, strokeSource, DisplaySettings.Shared, IdiomBookEntry.FromJson(Texts(idiomAssets ?? new TextAsset[0])));
            var subScreens = new TitleSubScreens(
                settings: settings, kanjiBook: kanjiBook, credits: new CreditsPresenter(creditsView, audioPlayer));
            presenter = new TitlePresenter(
                titleView, modeSelect, subScreens, new ApplicationQuitter(), audioPlayer,
                strokeSource, logoText, TitleLaunchOptions.TakeOpenModeSelect());
            presenter.Initialize();
        }

        /// <summary>漢字帳の字の情報を読む（漢字帳で最初に字を選んだときに呼ばれる）</summary>
        private KanjiInfo LoadKanjiInfo()
        {
            return new KanjiInfo(
                KanjiReadingDictionary.FromJson(Texts(readingAssets)),
                KanjiMeaningDictionary.FromJson(Texts(meaningAssets)),
                KanjiWordDictionary.FromJson(Texts(wordAssets)));
        }

        private static string[] Texts(IEnumerable<TextAsset> assets)
        {
            return assets.Where(a => a != null).Select(a => a.text).ToArray();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
        }
    }
}
