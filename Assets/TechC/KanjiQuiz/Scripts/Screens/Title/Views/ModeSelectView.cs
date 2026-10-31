using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// モード選択画面（教室の時間割表と黒板）の表示
    /// 時間割でモードを選ぶと右の黒板に内容を書き、[この授業を受ける] で黒板を画面いっぱいに広げて難易度の列を書く
    /// </summary>
    public class ModeSelectView : MonoBehaviour, IModeSelectView
    {
        [Tooltip("モード選択画面全体（タイトル画面と切り替える）")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button backToTitleButton;

        [Header("--- 時間割 ---")]
        [Tooltip("時間割表の紙。黒板を広げている間は隠す")]
        [SerializeField] private GameObject timetablePaper;
        [SerializeField] private RectTransform rowContainer;
        [SerializeField] private TimetableRowView rowPrefab;
        [Tooltip("昼休みの行。lunchBreakAfter 時間目のあとに入れる")]
        [SerializeField] private GameObject lunchBreakPrefab;
        [Min(1)] [SerializeField] private int lunchBreakAfter = 4;

        [Header("--- 黒板 ---")]
        [Tooltip("黒板（枠を含む）。小さい場所と画面いっぱいの場所の間で動かす")]
        [SerializeField] private RectTransform board;
        [Tooltip("時間割の横に置くときの黒板の位置と大きさ（黒板と同じ親に置いた空のオブジェクト）")]
        [SerializeField] private RectTransform smallBoardArea;
        [Tooltip("画面いっぱいに広げたときの黒板の位置と大きさ")]
        [SerializeField] private RectTransform largeBoardArea;
        [Tooltip("黒板を広げる・戻すのにかける秒数")]
        [Min(0f)] [SerializeField] private float boardMoveSeconds = 0.35f;

        [Header("--- 黒板（時間割の横） ---")]
        [SerializeField] private GameObject smallContent;
        [SerializeField] private TMP_Text periodText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [Tooltip("難易度ごとの級（例：簡単 5級　常識 6級　困難 ―）")]
        [SerializeField] private TMP_Text ranksText;
        [SerializeField] private Button attendButton;
        [Tooltip("準備中のモードを選んだときに説明の代わりに出す文")]
        [TextArea] [SerializeField] private string unavailableMessage = "この授業は準備中です。\nできあがるまで、もう少し待ってください。";
        [SerializeField] private Color rankColor = new Color(0.95f, 0.66f, 0.63f, 1f);

        [Header("--- 黒板（画面いっぱい） ---")]
        [SerializeField] private GameObject largeContent;
        [SerializeField] private TMP_Text boardPeriodText;
        [SerializeField] private TMP_Text boardNameText;
        [SerializeField] private TMP_Text boardDescriptionText;
        [SerializeField] private RectTransform columnContainer;
        [SerializeField] private DifficultyColumnView columnPrefab;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private Button backToTimetableButton;

        private readonly List<TimetableRowView> rows = new List<TimetableRowView>();
        private readonly List<DifficultyColumnView> columns = new List<DifficultyColumnView>();
        private readonly Subject<int> modeClicked = new Subject<int>();
        private readonly Subject<int> modeMoved = new Subject<int>();
        private readonly Subject<Unit> attendKeyPressed = new Subject<Unit>();
        private readonly Subject<Unit> backToTitleKeyPressed = new Subject<Unit>();
        private readonly Subject<int> difficultyClicked = new Subject<int>();
        private readonly Subject<int> difficultyMoved = new Subject<int>();
        private readonly Subject<Unit> startKeyPressed = new Subject<Unit>();
        private readonly Subject<Unit> backToTimetableKeyPressed = new Subject<Unit>();
        private readonly CompositeDisposable columnSubscriptions = new CompositeDisposable();
        private CancellationTokenSource boardMoveCts;
        private bool isBoardOpen;

        public Observable<int> OnModeClicked => modeClicked;
        public Observable<int> OnModeMoved => modeMoved;
        public Observable<Unit> OnAttendClicked => attendButton.OnClickAsObservable().Merge(attendKeyPressed);
        public Observable<Unit> OnBackToTitleClicked => backToTitleButton.OnClickAsObservable().Merge(backToTitleKeyPressed);
        public Observable<int> OnDifficultyClicked => difficultyClicked;
        public Observable<int> OnDifficultyMoved => difficultyMoved;
        public Observable<Unit> OnStartClicked => startButton.OnClickAsObservable().Merge(startKeyPressed);
        public Observable<Unit> OnBackToTimetableClicked => backToTimetableButton.OnClickAsObservable().Merge(backToTimetableKeyPressed);

        private void Awake()
        {
            modeClicked.AddTo(this);
            modeMoved.AddTo(this);
            attendKeyPressed.AddTo(this);
            backToTitleKeyPressed.AddTo(this);
            difficultyClicked.AddTo(this);
            difficultyMoved.AddTo(this);
            startKeyPressed.AddTo(this);
            backToTimetableKeyPressed.AddTo(this);
            columnSubscriptions.AddTo(this);
        }

        private void Update()
        {
            if (!root.activeInHierarchy || InputGate.IsBlocked) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (isBoardOpen)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame) difficultyMoved.OnNext(-1);
                if (keyboard.rightArrowKey.wasPressedThisFrame) difficultyMoved.OnNext(1);
                int digit = PressedDigit(keyboard, columns.Count);
                if (digit >= 0) difficultyClicked.OnNext(digit);
                if (IsSubmitPressed(keyboard) && startButton.interactable) startKeyPressed.OnNext(Unit.Default);
                if (keyboard.escapeKey.wasPressedThisFrame) backToTimetableKeyPressed.OnNext(Unit.Default);
            }
            else
            {
                if (keyboard.upArrowKey.wasPressedThisFrame) modeMoved.OnNext(-1);
                if (keyboard.downArrowKey.wasPressedThisFrame) modeMoved.OnNext(1);
                int digit = PressedDigit(keyboard, rows.Count);
                if (digit >= 0) modeClicked.OnNext(digit);
                if (IsSubmitPressed(keyboard) && attendButton.interactable) attendKeyPressed.OnNext(Unit.Default);
                if (keyboard.escapeKey.wasPressedThisFrame) backToTitleKeyPressed.OnNext(Unit.Default);
            }
        }

        private void OnDestroy()
        {
            CancelBoardMove();
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        public void SetTimetable(IReadOnlyList<ModeSummary> modes)
        {
            foreach (Transform child in rowContainer) Destroy(child.gameObject);
            rows.Clear();

            for (int i = 0; i < modes.Count; i++)
            {
                if (i == lunchBreakAfter && lunchBreakPrefab != null) Instantiate(lunchBreakPrefab, rowContainer);

                var row = Instantiate(rowPrefab, rowContainer);
                row.Set(modes[i]);
                int index = i;
                row.OnClicked.Subscribe(this, (_, self) => self.modeClicked.OnNext(index)).AddTo(row);
                rows.Add(row);
            }
        }

        public void SelectMode(int index, ModeSummary mode, bool canAttend)
        {
            for (int i = 0; i < rows.Count; i++) rows[i].SetSelected(i == index);

            periodText.text = $"{mode.Period}時間目";
            nameText.text = mode.Definition.DisplayName;
            descriptionText.text = mode.IsAvailable ? mode.Definition.Description : unavailableMessage;
            ranksText.text = mode.IsAvailable ? FormatRanks(mode) : string.Empty;
            attendButton.interactable = canAttend;
            attendButton.gameObject.SetActive(canAttend);
        }

        public void ShowDifficultyBoard(ModeSummary mode)
        {
            isBoardOpen = true;
            boardPeriodText.text = $"{mode.Period}時間目";
            boardNameText.text = mode.Definition.DisplayName;
            boardDescriptionText.text = mode.Definition.Description;
            SetColumns(mode.Records);

            timetablePaper.SetActive(false);
            backToTitleButton.gameObject.SetActive(false);
            smallContent.SetActive(false);
            attendButton.gameObject.SetActive(false);
            startButton.interactable = true;
            MoveBoardAsync(largeBoardArea, largeContent).Forget();
        }

        public void SelectDifficulty(int index, string startLabelText)
        {
            for (int i = 0; i < columns.Count; i++) columns[i].SetSelected(i == index);
            startLabel.text = startLabelText;
        }

        public void ShowTimetable()
        {
            isBoardOpen = false;
            largeContent.SetActive(false);
            startButton.interactable = false;
            timetablePaper.SetActive(true);
            backToTitleButton.gameObject.SetActive(true);
            MoveBoardAsync(smallBoardArea, smallContent).Forget();
        }

        private void SetColumns(IReadOnlyList<DifficultyRecord> records)
        {
            columnSubscriptions.Clear();
            foreach (Transform child in columnContainer) Destroy(child.gameObject);
            columns.Clear();

            for (int i = 0; i < records.Count; i++)
            {
                var column = Instantiate(columnPrefab, columnContainer);
                column.Set(records[i], isFirst: i == 0);
                int index = i;
                column.OnClicked.Subscribe(this, (_, self) => self.difficultyClicked.OnNext(index)).AddTo(columnSubscriptions);
                columns.Add(column);
            }
        }

        /// <summary>
        /// 黒板を指定した場所へ動かし、動き終わったら中身を見せる
        /// </summary>
        private async UniTaskVoid MoveBoardAsync(RectTransform target, GameObject contentToShow)
        {
            CancelBoardMove();
            boardMoveCts = new CancellationTokenSource();
            var ct = boardMoveCts.Token;

            contentToShow.SetActive(false);
            Vector2 fromPosition = board.anchoredPosition;
            Vector2 fromSize = board.sizeDelta;
            float elapsed = 0f;
            while (elapsed < boardMoveSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / boardMoveSeconds));
                board.anchoredPosition = Vector2.Lerp(fromPosition, target.anchoredPosition, t);
                board.sizeDelta = Vector2.Lerp(fromSize, target.sizeDelta, t);
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
            }

            board.anchoredPosition = target.anchoredPosition;
            board.sizeDelta = target.sizeDelta;
            contentToShow.SetActive(true);
            if (contentToShow == smallContent) attendButton.gameObject.SetActive(attendButton.interactable);
        }

        private void CancelBoardMove()
        {
            if (boardMoveCts == null) return;
            boardMoveCts.Cancel();
            boardMoveCts.Dispose();
            boardMoveCts = null;
        }

        private string FormatRanks(ModeSummary mode)
        {
            string color = ColorUtility.ToHtmlStringRGBA(rankColor);
            var builder = new StringBuilder();
            foreach (var record in mode.Records)
            {
                if (builder.Length > 0) builder.Append("　");
                builder.Append(record.Difficulty.ToDisplayName()).Append(' ');
                builder.Append(record.HasRecord ? $"<color=#{color}>{record.Rank}</color>" : "―");
            }
            return builder.ToString();
        }

        private static bool IsSubmitPressed(Keyboard keyboard)
        {
            return keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        }

        /// <summary>押された数字キー（1〜count）を 0 始まりの番号で返す。押されていなければ -1</summary>
        private static int PressedDigit(Keyboard keyboard, int count)
        {
            for (int i = 0; i < count && i < 9; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame || keyboard[Key.Numpad1 + i].wasPressedThisFrame) return i;
            }
            return -1;
        }
    }
}
