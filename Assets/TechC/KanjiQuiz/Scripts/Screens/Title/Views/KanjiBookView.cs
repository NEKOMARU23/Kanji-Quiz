using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// 漢字帳の表示（机の上に開いた、マス目の漢字練習帳）
    /// 左のページにタブと集めた字のマス目、右のページに選んだ字の内容と書き順のお手本を出す
    /// [← タイトルへ] か Esc で閉じる。← → でページをめくる
    /// 四字熟語のページでは、左のページに熟語の一覧、右のページに熟語の読み・意味・例文を出す
    /// </summary>
    public class KanjiBookView : MonoBehaviour, IKanjiBookView
    {
        [Tooltip("漢字帳の中身（画面いっぱい、タイトルより手前に置く）")]
        [SerializeField] private CanvasGroup root;
        [Tooltip("開くときに下ろすノート")]
        [SerializeField] private RectTransform notebook;
        [SerializeField] private Button backButton;
        [Tooltip("開くときの秒数")]
        [Min(0.01f)] [SerializeField] private float openSeconds = 0.25f;
        [Tooltip("開くときにノートが下りてくる距離（px）")]
        [SerializeField] private float openDropDistance = 40f;

        [Header("--- タブ ---")]
        [Tooltip("出題範囲のタブ（左から順に）")]
        [SerializeField] private Button[] tabButtons = new Button[0];
        [SerializeField] private Color tabColor = new Color(0.85f, 0.82f, 0.74f, 1f);
        [SerializeField] private Color selectedTabColor = new Color(0.98f, 0.97f, 0.93f, 1f);
        [SerializeField] private TMP_Text sectionSummaryText;

        [Header("--- マス目 ---")]
        [Tooltip("マスを並べる場所（GridLayoutGroup）")]
        [SerializeField] private RectTransform cellContainer;
        [Tooltip("マスのひな形（Button と、子に TMP_Text）。起動時に columns×rows 個に増やす")]
        [SerializeField] private Button cellTemplate;
        [Min(1)] [SerializeField] private int columns = 10;
        [Min(1)] [SerializeField] private int rows = 8;
        [Tooltip("選んだマスを囲む枠")]
        [SerializeField] private RectTransform selectionFrame;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TMP_Text pageText;

        [Header("--- 右のページ ---")]
        [Tooltip("字を選んだときに出すもの")]
        [SerializeField] private GameObject detailRoot;
        [Tooltip("字を選んでいないときに出す案内")]
        [SerializeField] private GameObject emptyMessage;
        [SerializeField] private TMP_Text kanjiText;
        [SerializeField] private TMP_Text readingText;
        [SerializeField] private TMP_Text meaningText;
        [SerializeField] private TMP_Text exampleText;
        [SerializeField] private TMP_Text wordsText;
        [Tooltip("正解したモードのハンコ（左から使う）")]
        [SerializeField] private TMP_Text[] modeStamps = new TMP_Text[0];
        [Tooltip("例文の中の、その字の色")]
        [SerializeField] private Color highlightColor = new Color(0.784f, 0.294f, 0.192f, 1f);

        [Header("--- 書き順のお手本 ---")]
        [SerializeField] private ChalkStrokeGraphic strokeGraphic;
        [Tooltip("お手本の字の大きさ（px）")]
        [Min(1f)] [SerializeField] private float strokeGlyphSize = 200f;
        [Tooltip("1画を書く秒数")]
        [Min(0.01f)] [SerializeField] private float secondsPerStroke = 0.35f;
        [SerializeField] private Button replayButton;

        [Header("--- 四字熟語のページ ---")]
        [Tooltip("熟語の一覧を並べる場所（GridLayoutGroup）。四字熟語のページのときだけ出す")]
        [SerializeField] private RectTransform idiomContainer;
        [Tooltip("熟語1つ分の行のひな形（Button と、子に TMP_Text）。起動時に idiomColumns×idiomRows 個に増やす")]
        [SerializeField] private Button idiomTemplate;
        [Min(1)] [SerializeField] private int idiomColumns = 2;
        [Min(1)] [SerializeField] private int idiomRows = 8;
        [Tooltip("まだ正解していない熟語の行に出す文字")]
        [SerializeField] private string unknownIdiomText = "？？？？";
        [Tooltip("まだ正解していない熟語の行の文字の色（集めた熟語より薄くする）")]
        [SerializeField] private Color unknownIdiomColor = new Color(0.71f, 0.68f, 0.62f, 1f);
        [Tooltip("熟語を選んだときに右のページに出すもの")]
        [SerializeField] private GameObject idiomDetailRoot;
        [Tooltip("熟語を選んでいないときに出す案内")]
        [SerializeField] private GameObject idiomEmptyMessage;
        [SerializeField] private TMP_Text idiomWordText;
        [SerializeField] private TMP_Text idiomReadingText;
        [SerializeField] private TMP_Text idiomMeaningText;
        [SerializeField] private TMP_Text idiomExampleText;

        private readonly Subject<Unit> escapePressed = new Subject<Unit>();
        private readonly Subject<int> pageKeyPressed = new Subject<int>();
        private readonly Subject<int> tabClicked = new Subject<int>();
        private readonly Subject<int> cellClicked = new Subject<int>();
        private readonly List<Button> cells = new List<Button>();
        private readonly List<TMP_Text> cellLabels = new List<TMP_Text>();
        private readonly List<Image> cellImages = new List<Image>();
        private readonly Subject<int> idiomClicked = new Subject<int>();
        private readonly List<Button> idiomButtons = new List<Button>();
        private readonly List<TMP_Text> idiomLabels = new List<TMP_Text>();
        private bool isIdiomMode;
        private Vector2 notebookPosition;
        private float openElapsed;
        private bool isStrokePlaying;
        private float strokeProgress;

        public Observable<Unit> OnBackClicked => backButton.OnClickAsObservable().Merge(escapePressed);
        public Observable<int> OnTabClicked => tabClicked;
        public Observable<int> OnPageMoved =>
            prevPageButton.OnClickAsObservable().Select(_ => -1)
                .Merge(nextPageButton.OnClickAsObservable().Select(_ => 1))
                .Merge(pageKeyPressed);
        public Observable<int> OnCellClicked => cellClicked;
        public int CellsPerPage => columns * rows;
        public Observable<int> OnIdiomClicked => idiomClicked;
        public int IdiomsPerPage => idiomColumns * idiomRows;

        private void Awake()
        {
            escapePressed.AddTo(this);
            pageKeyPressed.AddTo(this);
            tabClicked.AddTo(this);
            cellClicked.AddTo(this);
            idiomClicked.AddTo(this);
            if (notebook != null) notebookPosition = notebook.anchoredPosition;

            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                tabButtons[i].OnClickAsObservable().Subscribe(this, (_, self) => self.tabClicked.OnNext(index)).AddTo(this);
            }

            CreateCells();
            CreateIdiomRows();
            if (replayButton != null) replayButton.OnClickAsObservable().Subscribe(this, static (_, self) => self.RestartStrokes()).AddTo(this);
        }

        /// <summary>ひな形から columns×rows 個のマスを作る</summary>
        private void CreateCells()
        {
            cellTemplate.gameObject.SetActive(false);
            if (cellContainer.TryGetComponent<GridLayoutGroup>(out var grid))
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = columns;
            }

            for (int i = 0; i < CellsPerPage; i++)
            {
                var cell = Instantiate(cellTemplate, cellContainer);
                cell.name = $"Cell_{i}";
                cell.gameObject.SetActive(true);
                int index = i;
                cell.OnClickAsObservable().Subscribe(this, (_, self) => self.cellClicked.OnNext(index)).AddTo(this);
                cells.Add(cell);
                cellLabels.Add(cell.GetComponentInChildren<TMP_Text>(true));
                cellImages.Add(cell.GetComponent<Image>());
            }
        }

        /// <summary>ひな形から idiomColumns×idiomRows 個の熟語の行を作る</summary>
        private void CreateIdiomRows()
        {
            if (idiomTemplate == null || idiomContainer == null) return;
            idiomTemplate.gameObject.SetActive(false);
            if (idiomContainer.TryGetComponent<GridLayoutGroup>(out var grid))
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = idiomColumns;
            }

            for (int i = 0; i < IdiomsPerPage; i++)
            {
                var row = Instantiate(idiomTemplate, idiomContainer);
                row.name = $"Idiom_{i}";
                row.gameObject.SetActive(true);
                int index = i;
                row.OnClickAsObservable().Subscribe(this, (_, self) => self.idiomClicked.OnNext(index)).AddTo(this);
                idiomButtons.Add(row);
                idiomLabels.Add(row.GetComponentInChildren<TMP_Text>(true));
            }
            idiomContainer.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!root.gameObject.activeInHierarchy) return;

            if (openElapsed < openSeconds)
            {
                openElapsed += Time.unscaledDeltaTime;
                ApplyOpen(Mathf.Clamp01(openElapsed / openSeconds));
            }

            if (isStrokePlaying)
            {
                strokeProgress += Time.unscaledDeltaTime / secondsPerStroke;
                if (strokeProgress >= strokeGraphic.StrokeCount)
                {
                    strokeGraphic.Complete();
                    isStrokePlaying = false;
                }
                else
                {
                    strokeGraphic.Progress = strokeProgress;
                }
            }

            if (InputGate.IsBlocked) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) escapePressed.OnNext(Unit.Default);
            else if (keyboard.leftArrowKey.wasPressedThisFrame) pageKeyPressed.OnNext(-1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) pageKeyPressed.OnNext(1);
        }

        public void SetVisible(bool visible)
        {
            root.gameObject.SetActive(visible);
            openElapsed = 0f;
            ApplyOpen(visible ? 0f : 1f);
        }

        private void ApplyOpen(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            root.alpha = eased;
            if (notebook != null) notebook.anchoredPosition = notebookPosition + Vector2.up * (openDropDistance * (1f - eased));
        }

        public void SetTabs(IReadOnlyList<string> titles, int selectedIndex)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool exists = i < titles.Count;
                tabButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;

                var label = tabButtons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = titles[i];
                if (tabButtons[i].targetGraphic != null) tabButtons[i].targetGraphic.color = i == selectedIndex ? selectedTabColor : tabColor;
            }
        }

        public void SetSectionSummary(string title, int collectedCount, int totalCount)
        {
            sectionSummaryText.text = $"{title}　{collectedCount} / {totalCount}";
        }

        public void SetPage(IReadOnlyList<string> cellTexts, int pageIndex, int pageCount)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                string text = i < cellTexts.Count ? cellTexts[i] : null;
                cellImages[i].enabled = text != null;
                cellLabels[i].text = text ?? string.Empty;
                cells[i].interactable = !string.IsNullOrEmpty(text);
            }
            pageText.text = $"{pageIndex + 1} / {pageCount}";
            prevPageButton.interactable = pageIndex > 0;
            nextPageButton.interactable = pageIndex < pageCount - 1;
        }

        public void SetSelectedCell(int index)
        {
            bool shown = index >= 0 && index < cells.Count;
            selectionFrame.gameObject.SetActive(shown);
            if (!shown) return;

            selectionFrame.SetParent(cells[index].transform, false);
            selectionFrame.anchorMin = Vector2.zero;
            selectionFrame.anchorMax = Vector2.one;
            selectionFrame.offsetMin = selectionFrame.offsetMax = Vector2.zero;
            selectionFrame.SetAsLastSibling();
        }

        public void ShowDetail(KanjiBookDetail detail)
        {
            if (isIdiomMode) return;
            detailRoot.SetActive(detail != null);
            emptyMessage.SetActive(detail == null);
            if (detail == null) return;

            kanjiText.text = detail.Kanji;
            readingText.text = string.IsNullOrEmpty(detail.Reading) ? string.Empty : $"読み　{detail.Reading}";
            meaningText.text = detail.Meaning;
            exampleText.text = Highlight(detail.Example, detail.Kanji);
            wordsText.text = detail.Words.Count > 0 ? "使用例　" + string.Join("・", detail.Words) : string.Empty;
            for (int i = 0; i < modeStamps.Length; i++)
            {
                bool exists = i < detail.ModeNames.Count;
                modeStamps[i].transform.parent.gameObject.SetActive(exists);
                if (exists) modeStamps[i].text = detail.ModeNames[i];
            }
        }

        /// <summary>文の中の字を目立つ色にする</summary>
        private string Highlight(string text, string kanji)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(kanji)) return text;
            string color = ColorUtility.ToHtmlStringRGB(highlightColor);
            return text.Replace(kanji, $"<color=#{color}>{kanji}</color>");
        }

        public void SetIdiomMode(bool idiomMode)
        {
            isIdiomMode = idiomMode;
            cellContainer.gameObject.SetActive(!idiomMode);
            if (idiomContainer != null) idiomContainer.gameObject.SetActive(idiomMode);
            if (idiomMode)
            {
                detailRoot.SetActive(false);
                emptyMessage.SetActive(false);
            }
            else
            {
                if (idiomDetailRoot != null) idiomDetailRoot.SetActive(false);
                if (idiomEmptyMessage != null) idiomEmptyMessage.SetActive(false);
            }
        }

        public void SetIdiomPage(IReadOnlyList<string> idioms, int pageIndex, int pageCount)
        {
            for (int i = 0; i < idiomButtons.Count; i++)
            {
                string text = i < idioms.Count ? idioms[i] : null;
                idiomButtons[i].gameObject.SetActive(text != null);
                if (text == null) continue;
                idiomLabels[i].text = text.Length > 0
                    ? text
                    : $"<color=#{ColorUtility.ToHtmlStringRGB(unknownIdiomColor)}>{unknownIdiomText}</color>";
                idiomButtons[i].interactable = text.Length > 0;
            }
            pageText.text = $"{pageIndex + 1} / {pageCount}";
            prevPageButton.interactable = pageIndex > 0;
            nextPageButton.interactable = pageIndex < pageCount - 1;
        }

        public void SetSelectedIdiom(int index)
        {
            bool shown = index >= 0 && index < idiomButtons.Count;
            selectionFrame.gameObject.SetActive(shown);
            if (!shown) return;

            selectionFrame.SetParent(idiomButtons[index].transform, false);
            selectionFrame.anchorMin = Vector2.zero;
            selectionFrame.anchorMax = Vector2.one;
            selectionFrame.offsetMin = selectionFrame.offsetMax = Vector2.zero;
            selectionFrame.SetAsLastSibling();
        }

        public void ShowIdiomDetail(IdiomBookDetail detail)
        {
            if (idiomDetailRoot != null) idiomDetailRoot.SetActive(detail != null);
            if (idiomEmptyMessage != null) idiomEmptyMessage.SetActive(detail == null);
            if (detail == null) return;

            idiomWordText.text = detail.Word;
            idiomReadingText.text = string.IsNullOrEmpty(detail.Reading) ? string.Empty : $"読み　{detail.Reading}";
            idiomMeaningText.text = detail.Meaning;
            idiomExampleText.text = Highlight(detail.Example, detail.Word);
        }

        public void PlayStrokes(IReadOnlyList<string> strokePaths)
        {
            var strokes = new List<StrokeData>();
            if (strokePaths != null)
            {
                foreach (var d in strokePaths) strokes.Add(StrokePathParser.Parse(d, strokeGlyphSize));
            }
            strokeGraphic.SetGlyphs(new[] { strokes }, new[] { Vector2.zero });
            if (replayButton != null) replayButton.gameObject.SetActive(strokes.Count > 0);
            RestartStrokes();
        }

        private void RestartStrokes()
        {
            strokeProgress = 0f;
            strokeGraphic.Progress = 0f;
            isStrokePlaying = strokeGraphic.StrokeCount > 0;
        }
    }
}
