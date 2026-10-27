using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// プリントを回収して次を配るようにシーンを切り替える
    /// 前の画面を撮ったプリントが上へ回収され、入れ替わりに次の画面のプリントが下から配られる。間から机が見える
    /// 前の画面は切り替える瞬間をカメラで1枚描いて残し、次の画面はカメラの映像をそのまま映し続けるので、
    /// 始まってすぐ動く画面でも途切れない
    /// UI をカメラの映像に入れるため、各シーンの Canvas は Screen Space - Camera にしておく
    /// シーンをまたいで1つだけ置く。最前面に出すため、自分の Canvas（Screen Space - Overlay）を持つ
    /// </summary>
    public sealed class SceneTransitionView : SingletonMonoBehaviour<SceneTransitionView>, ISceneTransition
    {
        [Tooltip("演出の間だけ表示するもの（机と2枚のプリント）。表示している間はクリックを受け付けない")]
        [SerializeField] private GameObject stage;
        [Tooltip("前の画面のプリント（回収される）")]
        [SerializeField] private RectTransform oldSheet;
        [SerializeField] private RawImage oldContent;
        [SerializeField] private CanvasGroup oldShadow;
        [Tooltip("次の画面のプリント（配られる）")]
        [SerializeField] private RectTransform newSheet;
        [SerializeField] private RawImage newContent;
        [SerializeField] private CanvasGroup newShadow;
        [Tooltip("紙の音を鳴らす。不要なら空でよい")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip paperClip;

        [Header("動き")]
        [Tooltip("前のプリントが回収されるまでの秒数")]
        [SerializeField] private float collectDuration = 0.69f;
        [Tooltip("回収が始まってから、次のプリントを配り始めるまでの秒数（読み込みが遅ければもっと待つ）")]
        [SerializeField] private float dealDelay = 0.38f;
        [Tooltip("次のプリントが配られるまでの秒数")]
        [SerializeField] private float dealDuration = 0.69f;
        [Tooltip("配られたプリントが机に落ち着く（影が消える）までの秒数")]
        [SerializeField] private float settleDuration = 0.19f;
        [Tooltip("回収されるプリントの傾き（度）")]
        [SerializeField] private float collectAngle = 4.6f;
        [Tooltip("配られるプリントの最初の傾き（度）")]
        [SerializeField] private float dealAngle = -5.2f;

        private bool isPlaying;

        public bool IsPlaying => isPlaying;

        protected override void OnAwake()
        {
            stage.SetActive(false);
        }

        public void Play(string sceneName)
        {
            if (isPlaying) return;

            PlayAsync(sceneName, destroyCancellationToken).Forget();
        }

        private async UniTaskVoid PlayAsync(string sceneName, CancellationToken ct)
        {
            isPlaying = true;
            // 切り替えの途中は、後ろの画面がキーに反応しないようにする
            InputGate.Block(this);
            RenderTexture oldView = null;
            RenderTexture newView = null;
            Camera newCamera = null;
            try
            {
                // 前の画面をカメラで1枚描いて残す
                oldView = RenderSnapshot(Camera.main);
                oldContent.texture = oldView;
                oldSheet.gameObject.SetActive(true);
                SetSheet(oldSheet, oldShadow, Vector2.zero, 0f, 1f, 0f);
                newSheet.gameObject.SetActive(false);
                stage.SetActive(true);

                // 回収しながら、次のシーンを読み込む
                float startTime = Time.unscaledTime;
                PlayPaperSound();
                var collect = CollectAsync(ct);
                await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: ct);
                // 次のシーンの Start を済ませてから、カメラの映像をプリントに映す
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                newCamera = Camera.main;
                if (newCamera != null)
                {
                    newView = RedirectToTexture(newCamera);
                    newContent.texture = newView;

                    float wait = dealDelay - (Time.unscaledTime - startTime);
                    if (wait > 0f) await UniTask.Delay(System.TimeSpan.FromSeconds(wait), ignoreTimeScale: true, cancellationToken: ct);
                    PlayPaperSound();
                    await DealAsync(ct);
                }
                await collect;
            }
            finally
            {
                // 同じフレームでカメラを画面に戻し、演出を隠すので、つなぎ目は見えない
                if (newCamera != null) newCamera.targetTexture = null;
                ReleaseTexture(newView);
                ReleaseTexture(oldView);
                if (stage != null) stage.SetActive(false);
                InputGate.Unblock(this);
                isPlaying = false;
            }
        }

        /// <summary>前のプリントを上へ回収する</summary>
        private async UniTask CollectAsync(CancellationToken ct)
        {
            var size = CanvasSize;
            await AnimateAsync(collectDuration, t =>
            {
                float e = t * t * t; // だんだん速く（ease in）
                float lift = Smooth(Mathf.Clamp01(t * 4f)); // 持ち上げると影が出る
                var position = new Vector2(-0.047f * size.x * e, 1.15f * size.y * e);
                SetSheet(oldSheet, oldShadow, position, collectAngle * e, 1f + 0.02f * lift, lift);
            }, ct);
            oldSheet.gameObject.SetActive(false);
        }

        /// <summary>次のプリントを下から配り、机に落ち着かせる</summary>
        private async UniTask DealAsync(CancellationToken ct)
        {
            var size = CanvasSize;
            newSheet.gameObject.SetActive(true);
            await AnimateAsync(dealDuration, t =>
            {
                float rest = Mathf.Pow(1f - t, 3f); // だんだん遅く（ease out）の残り
                var position = new Vector2(0.031f * size.x * rest, -1.1f * size.y * rest);
                SetSheet(newSheet, newShadow, position, dealAngle * rest, 1.03f, 1f);
            }, ct);
            await AnimateAsync(settleDuration, t =>
            {
                float s = Smooth(t);
                SetSheet(newSheet, newShadow, Vector2.zero, 0f, 1f + 0.03f * (1f - s), 1f - s);
            }, ct);
        }

        private static async UniTask AnimateAsync(float duration, System.Action<float> apply, CancellationToken ct)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                apply(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.unscaledDeltaTime;
            }
            apply(1f);
        }

        private static void SetSheet(RectTransform sheet, CanvasGroup shadow, Vector2 position, float angle, float scale, float shadowAlpha)
        {
            sheet.anchoredPosition = position;
            sheet.localEulerAngles = new Vector3(0f, 0f, angle);
            sheet.localScale = Vector3.one * scale;
            shadow.alpha = shadowAlpha;
        }

        /// <summary>
        /// カメラの映像を画面ではなくテクスチャに描かせる。カメラがなければ null を返す
        /// </summary>
        private static RenderTexture RedirectToTexture(Camera camera)
        {
            if (camera == null) return null;

            var texture = new RenderTexture(Screen.width, Screen.height, 24);
            camera.targetTexture = texture;
            return texture;
        }

        /// <summary>
        /// カメラの今の映像を1枚だけテクスチャに描く。カメラはそのまま画面に描き続ける。カメラがなければ null を返す
        /// </summary>
        private static RenderTexture RenderSnapshot(Camera camera)
        {
            if (camera == null) return null;

            var texture = new RenderTexture(Screen.width, Screen.height, 24);
            camera.targetTexture = texture;
            camera.Render();
            camera.targetTexture = null;
            return texture;
        }

        private static void ReleaseTexture(RenderTexture texture)
        {
            if (texture == null) return;

            texture.Release();
            Destroy(texture);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private Vector2 CanvasSize => ((RectTransform)transform).rect.size;

        private void PlayPaperSound()
        {
            if (audioSource != null && paperClip != null) audioSource.PlayOneShot(paperClip);
        }
    }
}
