using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Screens.Title;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用のモード選択画面。表示の指示を記録し、入力をテストから送れる
    /// </summary>
    public class FakeModeSelectView : IModeSelectView
    {
        public readonly Subject<int> ModeClicked = new Subject<int>();
        public readonly Subject<int> ModeMoved = new Subject<int>();
        public readonly Subject<Unit> AttendClicked = new Subject<Unit>();
        public readonly Subject<Unit> BackToTitleClicked = new Subject<Unit>();
        public readonly Subject<int> DifficultyClicked = new Subject<int>();
        public readonly Subject<int> DifficultyMoved = new Subject<int>();
        public readonly Subject<Unit> StartClicked = new Subject<Unit>();
        public readonly Subject<Unit> BackToTimetableClicked = new Subject<Unit>();
        public readonly Subject<bool> ChalkWriting = new Subject<bool>();

        public Observable<int> OnModeClicked => ModeClicked;
        public Observable<int> OnModeMoved => ModeMoved;
        public Observable<Unit> OnAttendClicked => AttendClicked;
        public Observable<Unit> OnBackToTitleClicked => BackToTitleClicked;
        public Observable<int> OnDifficultyClicked => DifficultyClicked;
        public Observable<int> OnDifficultyMoved => DifficultyMoved;
        public Observable<Unit> OnStartClicked => StartClicked;
        public Observable<Unit> OnBackToTimetableClicked => BackToTimetableClicked;
        public Observable<bool> OnChalkWriting => ChalkWriting;

        public bool IsVisible { get; private set; }
        public IReadOnlyList<ModeSummary> Timetable { get; private set; }
        public int SelectedMode { get; private set; } = -1;
        public bool CanAttend { get; private set; }
        public bool IsBoardShown { get; private set; }
        public ModeSummary BoardMode { get; private set; }
        public int SelectedDifficulty { get; private set; } = -1;
        public string StartLabel { get; private set; }

        public void SetVisible(bool visible) => IsVisible = visible;

        public void SetTimetable(IReadOnlyList<ModeSummary> modes) => Timetable = modes;

        public void SelectMode(int index, ModeSummary mode, bool canAttend)
        {
            SelectedMode = index;
            CanAttend = canAttend;
        }

        public void ShowDifficultyBoard(ModeSummary mode)
        {
            IsBoardShown = true;
            BoardMode = mode;
        }

        public void SelectDifficulty(int index, string startLabel)
        {
            SelectedDifficulty = index;
            StartLabel = startLabel;
        }

        public bool LastShownFromTitle { get; private set; }

        public void ShowTimetable(bool fromTitle)
        {
            IsBoardShown = false;
            LastShownFromTitle = fromTitle;
        }
    }
}
