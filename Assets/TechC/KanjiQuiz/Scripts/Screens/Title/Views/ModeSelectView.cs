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
    /// 時間割でモードを選ぶと右の黒板に内容をチョークで書き、[この授業を受ける] で黒板を画面いっぱいに広げて難易度の列を書く
    /// 黒板はタイトル画面と使い回す。タイトルから来たときは、黒板がタイトルの大きさから縮み、時間割表が貼られる
    /// 演出はクリックか Enter・スペースで飛ばせる
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
        [Tooltip("黒板（枠を含む）。タイトルの場所・時間割の横・画面いっぱいの間で動かす。モード選択画面を隠しても見えるように root の外に置く")]
        [SerializeField] private RectTransform board;
        [Tooltip("タイトル画面のときの黒板の位置と大きさ（黒板と同じ大きさの親に置いた空のオブジェクト）")]
        [SerializeField] private RectTransform titleBoardArea;
        [Tooltip("時間割の横に置くときの黒板の位置と大きさ（黒板と同じ親に置いた空のオブジェクト）")]
        [SerializeField] private RectTransform smallBoardArea;
        [Tooltip("画面いっぱいに広げたときの黒板の位置と大きさ")]
        [SerializeField] private RectTransform largeBoardArea;
        [Tooltip("黒板を広げる・戻すのにかける秒数")]
        [Min(0f)] [SerializeField] private float boardMoveSeconds = 0.35f;

        [Header("--- タイトルから来たときの演出 ---")]
        [Tooltip("黒板がタイトルの大きさから時間割の横の大きさへ縮む秒数")]
        [Min(0f)] [SerializeField] private float boardShrinkSeconds = 0.65f;
        [Tooltip("黒板が縮み始めてから、時間割表が貼られ始めるまでの秒数")]
        [Min(0f)] [SerializeField] private float timetableDelay = 0.45f;
        [Tooltip("時間割表が画面の外から貼られるまでの秒数")]
        [Min(0.01f)] [SerializeField] private float timetableSeconds = 0.6f;
        [Tooltip("時間割表が動き始める位置（貼る位置からのずれ）")]
        [SerializeField] private Vector2 timetableStartOffset = new Vector2(-1150f, 0f);
        [Tooltip("時間割表が動き始めるときの傾き（度）")]
        [SerializeField] private float timetableStartAngle = 8f;
        [Tooltip("[この授業を受ける] がはずむように出る秒数")]
        [Min(0.01f)] [SerializeField] private float attendPopSeconds = 0.35f;

        [Header("--- 黒板（時間割の横） ---")]
        [SerializeField] private GameObject smallContent;
        [SerializeField] private TMP_Text periodText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [Tooltip("難易度ごとの級（例：簡単 5級　常識 6級　困難 ―）")]
        [SerializeField] private TMP_Text ranksText;
        [SerializeField] private Button attendButton;
        [Tooltip("時間割の横の黒板の字（時限・名前・説明・級）をチョークで書く")]
        [SerializeField] private ChalkTextWriter chalkWriter;
        [Tooltip("準備中のモードを選んだときに説明の代わりに出す文")]
        [TextArea] [SerializeField] private string unavailableMessage = "この{授業|じゅぎょう}は{準備中|じゅんびちゅう}です。\nできあがるまで、もう{少|すこ}し{待|ま}ってください。";
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
        private CancellationTokenSource writeCts;
        private RectTransform timetableRect;
        private Vector2 timetablePosition;
        private Quaternion timetableRotation;
        private bool isBoardOpen;
        private bool isTransitioning;
        private bool skipRequested;
        private bool isAttendRevealed;
        private bool canAttend;

        public Observable<int> OnModeClicked => modeClicked;
        public Observable<int> OnModeMoved => modeMoved;
        public Observable<Unit> OnAttendClicked => attendButton.OnClickAsObservable().Merge(attendKeyPressed);
        public Observable<Unit> OnBackToTitleClicked => backToTitleButton.OnClickAsObservable().Merge(backToTitleKeyPressed);
        public Observable<int> OnDifficultyClicked => difficultyClicked;
        public Observable<int> OnDifficultyMoved => difficultyMoved;
        public Observable<Unit> OnStartClicked => startButton.OnClickAsObservable().Merge(startKeyPressed);
        public Observable<Unit> OnBackToTimetableClicked => backToTimetableButton.OnClickAsObservable().Merge(backToTimetableKeyPressed);
        public Observable<bool> OnChalkWriting => chalkWriter != null ? chalkWriter.Writing : Observable.Return(false);

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

            timetableRect = (RectTransform)timetablePaper.transform;
            timetablePosition = timetableRect.anchoredPosition;
            timetableRotation = timetableRect.localRotation;
        }

        private void Update()
        {
            if (!root.activeInHierarchy || InputGate.IsBlocked) return;

            // 演出中のクリック・Enter・スペースは、演出を飛ばすのに使う
            if (isTransitioning)
            {
                if (SkipInput.WasPressedThisFrame()) skipRequested = true;
                return;
            }
            if (chalkWriter != null && chalkWriter.IsWriting && SkipInput.WasPressedThisFrame())
            {
                CompleteWriting();
                return;
            }

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
            CancelWriting();
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
            if (visible) return;

            // タイトルに戻るときは、黒板をタイトルの大きさに戻して中身を消す
            CancelBoardMove();
            CancelWriting();
            isTransitioning = false;
            isBoardOpen = false;
            isAttendRevealed = false;
            smallContent.SetActive(false);
            largeContent.SetActive(false);
            if (chalkWriter != null) chalkWriter.Clear();
            SnapBoard(titleBoardArea);
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

            periodText.text = $"{mode.Period}{{時間目|じかんめ}}";
            nameText.text = mode.Definition.DisplayName;
            descriptionText.text = mode.IsAvailable ? mode.Definition.Description : unavailableMessage;
            ranksText.text = mode.IsAvailable ? FormatRanks(mode) : string.Empty;
            this.canAttend = canAttend;
            attendButton.interactable = canAttend;
            attendButton.gameObject.SetActive(canAttend && isAttendRevealed);

            // タイトルからの演出中は、演出が終わってから書く
            if (!isTransitioning) StartWriting();
        }

        public void ShowDifficultyBoard(ModeSummary mode)
        {
            CompleteWriting();
            isBoardOpen = true;
            boardPeriodText.text = $"{mode.Period}{{時間目|じかんめ}}";
            boardNameText.text = mode.Definition.DisplayName;
            boardDescriptionText.text = mode.Definition.Description;
            SetColumns(mode.Records, mode.Definition.QuestionNoun);

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

        public void ShowTimetable(bool fromTitle)
        {
            bool wasBoardOpen = isBoardOpen;
            isBoardOpen = false;
            largeContent.SetActive(false);
            startButton.interactable = false;
            timetablePaper.SetActive(true);
            backToTitleButton.gameObject.SetActive(true);

            if (fromTitle)
            {
                TransitionFromTitleAsync().Forget();
            }
            else if (wasBoardOpen)
            {
                MoveBoardAsync(smallBoardArea, smallContent).Forget();
            }
            else
            {
                // タイトルを飛ばして開いたとき（結果画面の [モード選択へ] など）は、演出せずに置く
                CancelBoardMove();
                SnapBoard(smallBoardArea);
                ResetTimetablePose();
                smallContent.SetActive(true);
                isAttendRevealed = true;
            }
        }

        private void SetColumns(IReadOnlyList<DifficultyRecord> records, string questionNoun)
        {
            columnSubscriptions.Clear();
            foreach (Transform child in columnContainer) Destroy(child.gameObject);
            columns.Clear();

            for (int i = 0; i < records.Count; i++)
            {
                var column = Instantiate(columnPrefab, columnContainer);
                column.Set(records[i], isFirst: i == 0, questionNoun);
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
            if (contentToShow == smallContent) attendButton.gameObject.SetActive(canAttend && isAttendRevealed);
        }

        /// <summary>
        /// タイトルから来たときの演出：黒板が縮み、時間割表が貼られ、黒板の字をチョークで書いて、[この授業を受ける] を出す
        /// </summary>
        private async UniTaskVoid TransitionFromTitleAsync()
        {
            CancelBoardMove();
            boardMoveCts = new CancellationTokenSource();
            var ct = boardMoveCts.Token;

            isTransitioning = true;
            skipRequested = false;
            isAttendRevealed = false;
            attendButton.gameObject.SetActive(false);
            smallContent.SetActive(true);
            if (chalkWriter != null) chalkWriter.Clear();

            Vector2 fromPosition = titleBoardArea.anchoredPosition;
            Vector2 fromSize = titleBoardArea.sizeDelta;
            float total = Mathf.Max(boardShrinkSeconds, timetableDelay + timetableSeconds);
            float elapsed = 0f;
            while (elapsed < total && !skipRequested)
            {
                float boardT = boardShrinkSeconds > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / boardShrinkSeconds)) : 1f;
                board.anchoredPosition = Vector2.Lerp(fromPosition, smallBoardArea.anchoredPosition, boardT);
                board.sizeDelta = Vector2.Lerp(fromSize, smallBoardArea.sizeDelta, boardT);
                SetTimetablePose(Mathf.Clamp01((elapsed - timetableDelay) / timetableSeconds));

                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
                elapsed += Time.unscaledDeltaTime;
            }

            SnapBoard(smallBoardArea);
            ResetTimetablePose();
            isTransitioning = false;

            if (skipRequested)
            {
                // 飛ばしたときは字もすぐに全部出す
                if (chalkWriter != null) chalkWriter.Complete();
                RevealAttend(animate: false);
                return;
            }

            StartWriting();
            if (chalkWriter != null)
            {
                await UniTask.WaitWhile(() => chalkWriter.IsWriting, cancellationToken: ct).SuppressCancellationThrow();
                if (ct.IsCancellationRequested) return;
            }
            RevealAttend(animate: true);
        }

        /// <summary>時間割表を、画面の外（t = 0）から貼る位置（t = 1）までの途中に置く。最後に少し行き過ぎて戻る</summary>
        private void SetTimetablePose(float t)
        {
            timetablePaper.SetActive(t > 0f || timetableDelay <= 0f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = Mathf.Sin(t * Mathf.PI) * 0.03f;
            timetableRect.anchoredPosition = Vector2.LerpUnclamped(timetablePosition + timetableStartOffset, timetablePosition, eased + overshoot);
            timetableRect.localRotation = Quaternion.Slerp(Quaternion.Euler(0f, 0f, timetableStartAngle), timetableRotation, eased);
        }

        private void ResetTimetablePose()
        {
            timetablePaper.SetActive(true);
            timetableRect.anchoredPosition = timetablePosition;
            timetableRect.localRotation = timetableRotation;
        }

        private void SnapBoard(RectTransform target)
        {
            board.anchoredPosition = target.anchoredPosition;
            board.sizeDelta = target.sizeDelta;
        }

        /// <summary>[この授業を受ける] を出す（演出のあとは、はずむように出す）</summary>
        private void RevealAttend(bool animate)
        {
            isAttendRevealed = true;
            attendButton.gameObject.SetActive(canAttend);
            if (animate && canAttend) PopAsync((RectTransform)attendButton.transform, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid PopAsync(RectTransform target, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < attendPopSeconds)
            {
                float t = elapsed / attendPopSeconds;
                float scale = t < 0.7f ? Mathf.Lerp(0.6f, 1.08f, t / 0.7f) : Mathf.Lerp(1.08f, 1f, (t - 0.7f) / 0.3f);
                target.localScale = Vector3.one * scale;
                if (await UniTask.Yield(PlayerLoopTiming.Update, ct).SuppressCancellationThrow()) return;
                elapsed += Time.unscaledDeltaTime;
            }
            target.localScale = Vector3.one;
        }

        /// <summary>時間割の横の黒板の字を、チョークで書き直す</summary>
        private void StartWriting()
        {
            if (chalkWriter == null) return;

            CancelWriting();
            writeCts = new CancellationTokenSource();
            chalkWriter.WriteAsync(writeCts.Token).SuppressCancellationThrow().Forget();
        }

        /// <summary>書いている途中の字をすぐに全部出す</summary>
        private void CompleteWriting()
        {
            CancelWriting();
            if (chalkWriter != null) chalkWriter.Complete();
        }

        private void CancelWriting()
        {
            if (writeCts == null) return;
            writeCts.Cancel();
            writeCts.Dispose();
            writeCts = null;
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
                builder.Append(record.Difficulty.ToMarkedName()).Append(' ');
                builder.Append(record.HasRecord ? $"<color=#{color}>{KanjiRank.ToMarked(record.Rank)}</color>" : "―");
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
