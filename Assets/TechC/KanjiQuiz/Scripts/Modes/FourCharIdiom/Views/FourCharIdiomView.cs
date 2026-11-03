using System.Collections.Generic;
using System.Text;
using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 四字熟語づくり固有の表示
    /// 意味、4つのマス、候補の字（最大 12）を出し、押されたことを知らせる
    /// キー操作：1〜9・0・-・^ で候補（1〜12 番目）、Backspace でいちばん後ろに入れた字を戻す
    /// </summary>
    public class FourCharIdiomView : MonoBehaviour, IFourCharIdiomView
    {
        /// <summary>候補の番号に対応するキーの表示（1〜12 番目）</summary>
        private static readonly string[] KeyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "^" };

        [Tooltip("熟語の意味（ヒント）")]
        [SerializeField] private TMP_Text meaningText;

        [Header("--- マス ---")]
        [Tooltip("4つのマス（左から）。押すと入れた字を候補に戻す")]
        [SerializeField] private Button[] cellButtons = new Button[4];
        [Tooltip("マスの字（cellButtons と同じ順）")]
        [SerializeField] private TMP_Text[] cellTexts = new TMP_Text[4];
        [Tooltip("マスの背景（cellButtons と同じ順）")]
        [SerializeField] private Image[] cellImages = new Image[4];
        [SerializeField] private Color fixedColor = Color.white;
        [SerializeField] private Color blankColor = new Color(1f, 0.98f, 0.96f, 1f);
        [SerializeField] private Color filledColor = new Color(1f, 0.98f, 0.96f, 1f);
        [SerializeField] private Color correctColor = new Color(0.86f, 0.94f, 0.85f, 1f);
        [SerializeField] private Color incorrectColor = new Color(0.97f, 0.85f, 0.83f, 1f);
        [Tooltip("はじめから入っている字の色")]
        [SerializeField] private Color fixedTextColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        [Tooltip("プレイヤーが入れた字の色")]
        [SerializeField] private Color filledTextColor = new Color(0.18f, 0.35f, 0.66f, 1f);
        [Tooltip("空いたマスに出す字")]
        [SerializeField] private string blankText = "？";
        [Tooltip("空いたマスの字の色")]
        [SerializeField] private Color blankTextColor = new Color(0.78f, 0.29f, 0.19f, 1f);

        [Header("--- 候補 ---")]
        [Tooltip("候補を並べる親（GridLayoutGroup など）")]
        [SerializeField] private RectTransform tileContainer;
        [Tooltip("候補1つ分のひな形（Button。子に字の Label と、キーの KeyLabel の TextMeshPro）。起動時に 12 個に増やす")]
        [SerializeField] private Button tileTemplate;
        [Tooltip("使った候補の不透明度")]
        [Range(0f, 1f)] [SerializeField] private float usedTileAlpha = 0.25f;

        [Header("--- そのほか ---")]
        [Tooltip("問題文。振り返りでは、同じ場所に正解の熟語と例文を出すので隠す。不要なら空でよい")]
        [SerializeField] private GameObject questionText;
        [Tooltip("苦手な熟語のときに出す「苦手」の印。不要なら空でよい")]
        [SerializeField] private GameObject weakMark;
        [Tooltip("振り返りで、正解の熟語・読み・例文を出すテキスト。振り返りのときだけ表示される場所に置く")]
        [SerializeField] private TMP_Text answerInfoText;
        [Tooltip("例文の中の熟語の色")]
        [SerializeField] private Color highlightColor = new Color(0.78f, 0.29f, 0.19f, 1f);

        private readonly Subject<int> tileClicked = new Subject<int>();
        private readonly Subject<int> cellClicked = new Subject<int>();
        private readonly List<Button> tiles = new List<Button>();
        private readonly List<CanvasGroup> tileGroups = new List<CanvasGroup>();
        private bool isInteractable = true;

        public Observable<int> OnTileClicked => tileClicked;
        public Observable<int> OnCellClicked => cellClicked;

        private void Awake()
        {
            tileClicked.AddTo(this);
            cellClicked.AddTo(this);

            for (int i = 0; i < cellButtons.Length; i++)
            {
                int cell = i;
                if (cellButtons[i] != null)
                {
                    cellButtons[i].OnClickAsObservable().Subscribe(this, (_, self) => self.cellClicked.OnNext(cell)).AddTo(this);
                }
            }

            // 候補のひな形を 12 個に増やす
            if (tileTemplate != null)
            {
                tileTemplate.gameObject.SetActive(false);
                for (int i = 0; i < FourCharIdiomAnswer.MaxTileCount; i++)
                {
                    var tile = Instantiate(tileTemplate, tileContainer);
                    tile.name = $"Tile_{i + 1}";
                    int index = i;
                    tile.OnClickAsObservable().Subscribe(this, (_, self) => self.tileClicked.OnNext(index)).AddTo(this);
                    if (!tile.TryGetComponent<CanvasGroup>(out var group)) group = tile.gameObject.AddComponent<CanvasGroup>();
                    var key = tile.transform.Find("KeyLabel");
                    if (key != null && key.TryGetComponent<TMP_Text>(out var keyText)) keyText.text = KeyLabels[i];
                    tiles.Add(tile);
                    tileGroups.Add(group);
                }
            }
        }

        private void Update()
        {
            if (!isInteractable || InputGate.IsBlocked) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            for (int i = 0; i < tiles.Count; i++)
            {
                if (!tiles[i].gameObject.activeSelf) continue;
                if (WasTileKeyPressed(keyboard, i))
                {
                    tileClicked.OnNext(i);
                    return;
                }
            }
            if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame)
            {
                cellClicked.OnNext(-1);
            }
        }

        public void SetMeaning(string meaning)
        {
            if (meaningText != null) meaningText.text = string.IsNullOrEmpty(meaning) ? string.Empty : $"意味：{meaning}";
        }

        public void SetCells(IReadOnlyList<FourCharIdiomCell> cells)
        {
            for (int i = 0; i < cellTexts.Length && i < cells.Count; i++)
            {
                var cell = cells[i];
                bool isEmpty = string.IsNullOrEmpty(cell.Character);
                if (cellTexts[i] != null)
                {
                    cellTexts[i].text = isEmpty ? blankText : cell.Character;
                    cellTexts[i].color = isEmpty ? blankTextColor
                        : cell.Kind == FourCharIdiomCellKind.Fixed ? fixedTextColor : filledTextColor;
                }
                if (cellImages[i] != null) cellImages[i].color = CellColor(cell.Kind);
            }
        }

        public void SetTiles(IReadOnlyList<string> characters, IReadOnlyList<bool> used)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                bool visible = i < characters.Count;
                tiles[i].gameObject.SetActive(visible);
                if (!visible) continue;

                var label = tiles[i].transform.Find("Label");
                if (label != null && label.TryGetComponent<TMP_Text>(out var text)) text.text = characters[i];
                bool isUsed = used != null && i < used.Count && used[i];
                tileGroups[i].alpha = isUsed ? usedTileAlpha : 1f;
                tiles[i].interactable = isInteractable && !isUsed;
            }
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            foreach (var button in cellButtons)
            {
                if (button != null) button.interactable = interactable;
            }
            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].interactable = interactable && tileGroups[i].alpha >= 1f;
            }
        }

        public void SetWeakMarkVisible(bool visible)
        {
            if (weakMark != null) weakMark.SetActive(visible);
        }

        public void ShowAnswerInfo(string word, string reading, string example)
        {
            if (questionText != null) questionText.SetActive(string.IsNullOrEmpty(word));
            if (answerInfoText == null) return;
            if (string.IsNullOrEmpty(word))
            {
                answerInfoText.text = string.Empty;
                return;
            }

            var text = new StringBuilder();
            text.Append($"正解は「{word}」（{reading}）");
            if (!string.IsNullOrEmpty(example))
            {
                // 例文の中の熟語を強調する
                string color = ColorUtility.ToHtmlStringRGB(highlightColor);
                text.Append("\n例文：").Append(example.Replace(word, $"<color=#{color}>{word}</color>"));
            }
            answerInfoText.text = text.ToString();
        }

        private Color CellColor(FourCharIdiomCellKind kind)
        {
            switch (kind)
            {
                case FourCharIdiomCellKind.Blank: return blankColor;
                case FourCharIdiomCellKind.Filled: return filledColor;
                case FourCharIdiomCellKind.Correct: return correctColor;
                case FourCharIdiomCellKind.Incorrect: return incorrectColor;
                default: return fixedColor;
            }
        }

        private static bool WasTileKeyPressed(Keyboard keyboard, int index)
        {
            switch (index)
            {
                case 0: return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
                case 1: return keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame;
                case 2: return keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame;
                case 3: return keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame;
                case 4: return keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame;
                case 5: return keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame;
                case 6: return keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame;
                case 7: return keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame;
                case 8: return keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame;
                case 9: return keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame;
                case 10: return keyboard.minusKey.wasPressedThisFrame;
                case 11: return keyboard.equalsKey.wasPressedThisFrame;
                default: return false;
            }
        }
    }
}
