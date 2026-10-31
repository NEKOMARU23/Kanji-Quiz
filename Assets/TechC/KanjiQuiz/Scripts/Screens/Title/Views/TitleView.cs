using System.Collections.Generic;
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
    /// タイトル画面の表示
    /// 黒板にロゴをチョークで1画ずつ書き、メニューのマグネットを貼る。[はじめる] で黒板消しを上から下へ斜めに引いて消す
    /// 演出はクリックか Enter・スペースで飛ばせる
    /// </summary>
    public class TitleView : MonoBehaviour, ITitleView
    {
        [Tooltip("タイトルの中身（黒板の中に置く。ロゴ、副題、マグネット）")]
        [SerializeField] private GameObject root;
        [Tooltip("黒板の外に出すクレジットの文字")]
        [SerializeField] private GameObject credit;

        [Header("--- ロゴ ---")]
        [Tooltip("ロゴをチョークの線で書く")]
        [SerializeField] private ChalkStrokeGraphic logo;
        [Tooltip("書き順のデータがないときに出す、字の形のままのロゴ")]
        [SerializeField] private TMP_Text fallbackLogo;
        [Tooltip("ロゴの1字の大きさ（px）")]
        [Min(1f)] [SerializeField] private float glyphSize = 190f;
        [Tooltip("ロゴの字と字の間（px）")]
        [SerializeField] private float glyphSpacing = 20f;
        [Tooltip("1画を書く秒数")]
        [Min(0.01f)] [SerializeField] private float secondsPerStroke = 0.09f;
        [Tooltip("1画を書き終えてから次の画までの秒数")]
        [Min(0f)] [SerializeField] private float strokeInterval = 0.03f;
        [Tooltip("ロゴの下の副題")]
        [SerializeField] private CanvasGroup subtitle;

        [Header("--- メニュー（マグネット） ---")]
        [SerializeField] private CanvasGroup magnets;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [Tooltip("まだ作っていないメニュー（設定・漢字帳・クレジット）。薄く表示して押せないようにする")]
        [SerializeField] private Button[] unavailableButtons = new Button[0];
        [Tooltip("マグネットが貼られる・外れて落ちる秒数")]
        [Min(0.01f)] [SerializeField] private float magnetSeconds = 0.35f;
        [Tooltip("マグネットが落ちる距離（px）")]
        [SerializeField] private float magnetDropDistance = 80f;

        [Header("--- 黒板消し ---")]
        [Tooltip("消した跡を置く場所（黒板の中いっぱい、ロゴとマグネットより手前）")]
        [SerializeField] private RectTransform eraseLayer;
        [Tooltip("黒板消し（白い面を下にする）")]
        [SerializeField] private RectTransform eraser;
        [Tooltip("消した跡の色（黒板の色）")]
        [SerializeField] private Color eraseColor = new Color(0.184f, 0.29f, 0.227f, 1f);
        [Tooltip("黒板消しを引く回数")]
        [Min(1)] [SerializeField] private int eraseStrokes = 5;
        [Tooltip("1回引く秒数")]
        [Min(0.01f)] [SerializeField] private float secondsPerErase = 0.33f;
        [Tooltip("1回で消える帯の幅（黒板の幅に対する割合）")]
        [Range(0.05f, 1f)] [SerializeField] private float eraseBandWidth = 0.24f;
        [Tooltip("帯と帯の間隔（黒板の幅に対する割合）")]
        [Range(0.05f, 1f)] [SerializeField] private float eraseBandStep = 0.19f;
        [Tooltip("引く向きの傾き（度）。マイナスで左下がり")]
        [SerializeField] private float eraseAngle = -12f;

        private readonly Subject<Unit> startKeyPressed = new Subject<Unit>();
        private readonly List<RectTransform> eraseBands = new List<RectTransform>();
        private RectTransform magnetsRect;
        private Vector2 magnetsPosition;
        private RectTransform startRect;
        private bool isPlaying;
        private bool skipRequested;

        public Observable<Unit> OnStartClicked => startButton.OnClickAsObservable().Merge(startKeyPressed);
        public Observable<Unit> OnQuitClicked => quitButton.OnClickAsObservable();

        private void Awake()
        {
            startKeyPressed.AddTo(this);
            magnetsRect = (RectTransform)magnets.transform;
            magnetsPosition = magnetsRect.anchoredPosition;
            startRect = (RectTransform)startButton.transform;
            foreach (var button in unavailableButtons) button.interactable = false;
            if (eraser != null) eraser.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!root.activeInHierarchy || InputGate.IsBlocked) return;

            // 演出中のクリック・Enter・スペースは、演出を飛ばすのに使う
            if (isPlaying)
            {
                if (SkipInput.WasPressedThisFrame()) skipRequested = true;
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null || !magnets.interactable) return;
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                startKeyPressed.OnNext(Unit.Default);
            }
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
            if (credit != null) credit.SetActive(visible);
            if (!visible) ClearEraseBands();
        }

        public async UniTask PlayIntroAsync(IReadOnlyList<IReadOnlyList<string>> logoStrokePaths, CancellationToken ct)
        {
            ClearEraseBands();
            SetMagnetsShown(false);
            magnetsRect.anchoredPosition = magnetsPosition;
            subtitle.alpha = 0f;
            bool hasStrokes = PrepareLogo(logoStrokePaths);

            isPlaying = true;
            skipRequested = false;
            try
            {
                // ロゴを1画ずつ書く
                if (hasStrokes)
                {
                    for (int i = 0; i < logo.StrokeCount && !skipRequested; i++)
                    {
                        await AnimateAsync(secondsPerStroke, t => logo.Progress = i + t, ct);
                        if (!skipRequested) await AnimateAsync(strokeInterval, _ => { }, ct);
                    }
                    logo.Complete();
                }

                // 副題とマグネットを出す
                await AnimateAsync(magnetSeconds, t =>
                {
                    subtitle.alpha = t;
                    magnets.alpha = t;
                    magnetsRect.anchoredPosition = magnetsPosition + Vector2.up * (magnetDropDistance * 0.3f * (1f - t));
                }, ct);
            }
            finally
            {
                isPlaying = false;
            }

            subtitle.alpha = 1f;
            magnetsRect.anchoredPosition = magnetsPosition;
            SetMagnetsShown(true);
        }

        public async UniTask<bool> PlayExitAsync(CancellationToken ct)
        {
            magnets.interactable = false;
            magnets.blocksRaycasts = false;
            isPlaying = true;
            skipRequested = false;
            try
            {
                // [はじめる] を押した手応え → マグネットが外れて落ちる
                await AnimateAsync(0.2f, t => startRect.localScale = Vector3.one * (1f - 0.08f * Mathf.Sin(t * Mathf.PI)), ct);
                startRect.localScale = Vector3.one;
                await AnimateAsync(magnetSeconds, t =>
                {
                    magnets.alpha = 1f - t;
                    magnetsRect.anchoredPosition = magnetsPosition + Vector2.down * (magnetDropDistance * t * t);
                }, ct);
                magnets.alpha = 0f;

                // 黒板消しを上から下へ斜めに引いて、左から順に消す
                Rect area = eraseLayer.rect;
                if (eraser != null) eraser.gameObject.SetActive(true);
                for (int i = 0; i < eraseStrokes && !skipRequested; i++)
                {
                    var band = CreateEraseBand(area, i);
                    float bandHeight = area.height * 1.5f;
                    Vector2 top = new Vector2(band.anchoredPosition.x + area.width * eraseBandWidth * 0.5f, 0f);
                    Vector2 bottom = top + new Vector2(Mathf.Tan(eraseAngle * Mathf.Deg2Rad) * area.height, -area.height);
                    await AnimateAsync(secondsPerErase, t =>
                    {
                        float eased = t * t * (3f - 2f * t);
                        band.sizeDelta = new Vector2(band.sizeDelta.x, bandHeight * eased);
                        if (eraser != null) eraser.anchoredPosition = Vector2.Lerp(top, bottom, eased);
                    }, ct);
                    band.sizeDelta = new Vector2(band.sizeDelta.x, bandHeight);
                }
            }
            finally
            {
                isPlaying = false;
                if (eraser != null) eraser.gameObject.SetActive(false);
            }

            // 飛ばしたときは残りの帯もすぐに消す
            if (skipRequested)
            {
                Rect area = eraseLayer.rect;
                for (int i = eraseBands.Count; i < eraseStrokes; i++)
                {
                    var band = CreateEraseBand(area, i);
                    band.sizeDelta = new Vector2(band.sizeDelta.x, area.height * 1.5f);
                }
            }
            return skipRequested;
        }

        /// <summary>ロゴの字を並べる。線で書けない（データのない）字があれば、字の形のままのロゴを出す</summary>
        private bool PrepareLogo(IReadOnlyList<IReadOnlyList<string>> logoStrokePaths)
        {
            bool hasAll = logoStrokePaths != null && logoStrokePaths.Count > 0;
            if (hasAll)
            {
                foreach (var paths in logoStrokePaths)
                {
                    if (paths == null || paths.Count == 0) hasAll = false;
                }
            }

            logo.gameObject.SetActive(hasAll);
            if (fallbackLogo != null) fallbackLogo.gameObject.SetActive(!hasAll);
            if (!hasAll) return false;

            var glyphs = new List<IReadOnlyList<StrokeData>>();
            var centers = new List<Vector2>();
            float step = glyphSize + glyphSpacing;
            for (int g = 0; g < logoStrokePaths.Count; g++)
            {
                var strokes = new List<StrokeData>();
                foreach (var d in logoStrokePaths[g]) strokes.Add(StrokePathParser.Parse(d, glyphSize));
                glyphs.Add(strokes);
                centers.Add(new Vector2((g - (logoStrokePaths.Count - 1) / 2f) * step, 0f));
            }
            logo.SetGlyphs(glyphs, centers);
            return true;
        }

        private void SetMagnetsShown(bool shown)
        {
            magnets.alpha = shown ? 1f : 0f;
            magnets.interactable = shown;
            magnets.blocksRaycasts = shown;
        }

        /// <summary>i 回目に引いて消える帯（黒板の色の、斜めに傾けた長方形）を作る。最初は高さ 0</summary>
        private RectTransform CreateEraseBand(Rect area, int index)
        {
            var go = new GameObject($"EraseBand_{index}", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(eraseLayer, false);
            if (eraser != null) eraser.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(area.width * (eraseBandStep * index + 0.04f), area.height * 0.1f);
            rect.sizeDelta = new Vector2(area.width * eraseBandWidth, 0f);
            rect.localRotation = Quaternion.Euler(0f, 0f, eraseAngle);
            var image = go.GetComponent<Image>();
            image.color = eraseColor;
            image.raycastTarget = false;
            eraseBands.Add(rect);
            return rect;
        }

        private void ClearEraseBands()
        {
            foreach (var band in eraseBands)
            {
                if (band != null) Destroy(band.gameObject);
            }
            eraseBands.Clear();
        }

        /// <summary>
        /// seconds 秒かけて t を 0 から 1 まで進め、毎フレーム apply を呼ぶ。飛ばされたら、その場で 1 にして終える
        /// </summary>
        private async UniTask AnimateAsync(float seconds, System.Action<float> apply, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < seconds && !skipRequested)
            {
                apply(seconds > 0f ? elapsed / seconds : 1f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;
            }
            apply(1f);
        }
    }
}
