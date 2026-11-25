using System;
using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// モード選択画面の Presenter
    /// 時間割でモードを選び、黒板で難易度を選んで、そのモードのシーンを開く
    /// </summary>
    public sealed class ModeSelectPresenter : IDisposable
    {
        private IReadOnlyList<ModeSummary> modes;
        private readonly IModeSelectView view;
        private readonly ISceneLoader sceneLoader;
        private readonly IAudioPlayer audioPlayer;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly Subject<Unit> backToTitle = new Subject<Unit>();
        private int modeIndex;
        private int difficultyIndex;
        private bool isOpen;
        private bool isBoardOpen;
        private bool isStarting;
        private bool isChalkSoundPlaying;

        /// <summary>[← タイトルへ] が押されたとき</summary>
        public Observable<Unit> OnBackToTitle => backToTitle;

        /// <summary>今選んでいるモードの番号（0 始まり）</summary>
        public int SelectedModeIndex => modeIndex;

        /// <summary>難易度の黒板を開いているかどうか</summary>
        public bool IsBoardOpen => isBoardOpen;

        /// <param name="modes">時間割に並べるモード（ModeSummary.Load で作る）</param>
        /// <param name="audioPlayer">効果音を鳴らす。鳴らさないなら NullAudioPlayer</param>
        public ModeSelectPresenter(
            IReadOnlyList<ModeSummary> modes, IModeSelectView view, ISceneLoader sceneLoader, IAudioPlayer audioPlayer)
        {
            this.modes = modes ?? throw new ArgumentNullException(nameof(modes));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        }

        /// <summary>
        /// 時間割を並べ、ボタンの購読を始める。画面はまだ見せない
        /// </summary>
        public void Initialize()
        {
            view.SetTimetable(modes);
            view.SetVisible(false);

            view.OnModeClicked.Subscribe(this, static (i, self) => self.SelectMode(i, playSound: true)).AddTo(disposables);
            view.OnModeMoved.Subscribe(this, static (d, self) => self.MoveMode(d)).AddTo(disposables);
            view.OnAttendClicked.Subscribe(this, static (_, self) => self.OpenBoard()).AddTo(disposables);
            view.OnBackToTitleClicked.Subscribe(this, static (_, self) => self.BackToTitle()).AddTo(disposables);
            view.OnDifficultyClicked.Subscribe(this, static (i, self) => self.SelectDifficulty(i, playSound: true)).AddTo(disposables);
            view.OnDifficultyMoved.Subscribe(this, static (d, self) => self.MoveDifficulty(d)).AddTo(disposables);
            view.OnStartClicked.Subscribe(this, static (_, self) => self.Start()).AddTo(disposables);
            view.OnBackToTimetableClicked.Subscribe(this, static (_, self) => self.CloseBoard()).AddTo(disposables);
            view.OnChalkWriting.Subscribe(this, static (writing, self) => self.SetChalkSound(writing)).AddTo(disposables);
        }

        /// <summary>
        /// 時間割を並べ直す（記録を消したあとなど、記録を読み直したとき）
        /// </summary>
        /// <param name="modes">時間割に並べるモード（ModeSummary.Load で作る）</param>
        public void ReloadModes(IReadOnlyList<ModeSummary> modes)
        {
            this.modes = modes ?? throw new ArgumentNullException(nameof(modes));
            modeIndex = 0;
            view.SetTimetable(modes);
        }

        /// <summary>
        /// モード選択画面（時間割）を見せる。最初は遊べる一番上のモードを選ぶ
        /// </summary>
        /// <param name="fromTitle">タイトルから来たかどうか。true なら黒板が縮んで時間割表が貼られる演出をする</param>
        public void Open(bool fromTitle = false)
        {
            isOpen = true;
            isBoardOpen = false;
            view.ShowTimetable(fromTitle);
            view.SetVisible(true);
            if (modes.Count > 0) SelectMode(FirstAvailableIndex(), playSound: false);
        }

        private void SelectMode(int index, bool playSound)
        {
            if (!isOpen || isBoardOpen || index < 0 || index >= modes.Count) return;

            modeIndex = index;
            var mode = modes[index];
            view.SelectMode(index, mode, mode.IsAvailable);
            if (playSound) audioPlayer.PlaySe(SeType.Select);
        }

        private void MoveMode(int delta)
        {
            if (modes.Count == 0) return;
            SelectMode(Math.Clamp(modeIndex + delta, 0, modes.Count - 1), playSound: true);
        }

        private void OpenBoard()
        {
            if (!isOpen || isBoardOpen || modes.Count == 0) return;

            var mode = modes[modeIndex];
            if (!mode.IsAvailable) return;

            isBoardOpen = true;
            view.ShowDifficultyBoard(mode);
            audioPlayer.PlaySe(SeType.PageTurn);
            SelectDifficulty(FirstUnclearedIndex(mode), playSound: false);
        }

        private void CloseBoard()
        {
            if (!isBoardOpen || isStarting) return;

            isBoardOpen = false;
            view.ShowTimetable(fromTitle: false);
            audioPlayer.PlaySe(SeType.PageTurn);
            SelectMode(modeIndex, playSound: false);
        }

        private void SelectDifficulty(int index, bool playSound)
        {
            if (!isBoardOpen || isStarting) return;

            var records = modes[modeIndex].Records;
            if (index < 0 || index >= records.Count) return;

            difficultyIndex = index;
            view.SelectDifficulty(index, $"{records[index].Difficulty.ToMarkedName()}で{{始|はじ}}める");
            if (playSound) audioPlayer.PlaySe(SeType.Select);
        }

        private void MoveDifficulty(int delta)
        {
            if (!isBoardOpen) return;
            int count = modes[modeIndex].Records.Count;
            SelectDifficulty(Math.Clamp(difficultyIndex + delta, 0, count - 1), playSound: true);
        }

        private void Start()
        {
            if (!isBoardOpen || isStarting) return;

            isStarting = true;
            var mode = modes[modeIndex];
            QuizLaunchOptions.Difficulty = mode.Records[difficultyIndex].Difficulty;
            sceneLoader.Load(mode.Definition.SceneName);
        }

        private void BackToTitle()
        {
            if (!isOpen || isBoardOpen) return;

            isOpen = false;
            view.SetVisible(false);
            backToTitle.OnNext(Unit.Default);
        }

        private int FirstAvailableIndex()
        {
            for (int i = 0; i < modes.Count; i++)
            {
                if (modes[i].IsAvailable) return i;
            }
            return 0;
        }

        /// <summary>
        /// 黒板を開いたときに選んでおく難易度。まだ全問正解していない一番やさしい難易度にする
        /// （全部全問正解していれば一番難しいもの）
        /// </summary>
        private static int FirstUnclearedIndex(ModeSummary mode)
        {
            for (int i = 0; i < mode.Records.Count; i++)
            {
                var record = mode.Records[i];
                if (!record.HasRecord || record.BestCorrectCount < record.QuestionCount) return i;
            }
            return mode.Records.Count - 1;
        }

        /// <summary>黒板に字を書いている間だけ、チョークの音をループで鳴らす</summary>
        private void SetChalkSound(bool writing)
        {
            if (writing == isChalkSoundPlaying) return;
            isChalkSoundPlaying = writing;
            if (writing) audioPlayer.PlayLoop(SeType.Chalk);
            else audioPlayer.StopLoop();
        }

        public void Dispose()
        {
            disposables.Dispose();
            backToTitle.Dispose();
            // AudioManager はシーンをまたいで残るので、書いている途中で片付けたら音を止める
            SetChalkSound(false);
        }
    }
}
