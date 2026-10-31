using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 黒板の字を、チョークで1字ずつ書いているように出す
    /// 字は左から右へ少しずつ濃くなって現れ、チョークの先の画像を書いている字の位置へ動かす
    /// 字の中身は先に TMP_Text に入れておき、WriteAsync で書き始める
    /// </summary>
    public class ChalkTextWriter : MonoBehaviour
    {
        [Serializable]
        private sealed class Line
        {
            public TMP_Text text;
            [Tooltip("1字を書く秒数")]
            [Min(0.01f)] public float secondsPerChar = 0.06f;
        }

        [Tooltip("書く順に並べた字。行ごとに書く速さを変えられる")]
        [SerializeField] private Line[] lines = Array.Empty<Line>();
        [Tooltip("チョークの先の画像（下の端が書いている場所に来るように、Pivot を下の真ん中にする）。不要なら空でよい")]
        [SerializeField] private RectTransform chalkTip;
        [Tooltip("行と行の間の秒数")]
        [Min(0f)] [SerializeField] private float lineInterval = 0.12f;

        /// <summary>書いている途中かどうか</summary>
        public bool IsWriting { get; private set; }

        private void Awake()
        {
            if (chalkTip != null) chalkTip.gameObject.SetActive(false);
        }

        /// <summary>
        /// 字をすべて隠す（書く前の状態）
        /// </summary>
        public void Clear()
        {
            foreach (var line in lines)
            {
                if (line.text != null) line.text.maxVisibleCharacters = 0;
            }
            if (chalkTip != null) chalkTip.gameObject.SetActive(false);
        }

        /// <summary>
        /// 字をすべて書き終えた状態にする（演出を飛ばすとき）
        /// </summary>
        public void Complete()
        {
            foreach (var line in lines)
            {
                if (line.text == null) continue;
                line.text.maxVisibleCharacters = int.MaxValue;
                line.text.ForceMeshUpdate();
            }
            if (chalkTip != null) chalkTip.gameObject.SetActive(false);
            IsWriting = false;
        }

        /// <summary>
        /// 1字ずつ書く。キャンセルされたら、そこで止める（続きを出すなら Complete を呼ぶ）
        /// </summary>
        public async UniTask WriteAsync(CancellationToken ct)
        {
            Clear();
            IsWriting = true;
            try
            {
                foreach (var line in lines)
                {
                    if (line.text == null) continue;
                    await WriteLineAsync(line, ct);
                    await UniTask.Delay(TimeSpan.FromSeconds(lineInterval), cancellationToken: ct);
                }
            }
            finally
            {
                IsWriting = false;
                if (chalkTip != null) chalkTip.gameObject.SetActive(false);
            }
        }

        private async UniTask WriteLineAsync(Line line, CancellationToken ct)
        {
            var text = line.text;
            // 字を隠した状態だと、どの字も「見えない字」と扱われるので、全部見せた状態で空白かどうかを調べておく
            text.maxVisibleCharacters = int.MaxValue;
            text.ForceMeshUpdate();
            int count = text.textInfo.characterCount;
            var visible = new bool[count];
            for (int i = 0; i < count; i++) visible[i] = text.textInfo.characterInfo[i].isVisible;
            text.maxVisibleCharacters = 0;

            for (int i = 0; i < count; i++)
            {
                text.maxVisibleCharacters = i + 1;
                if (!visible[i]) continue;

                float elapsed = 0f;
                while (elapsed < line.secondsPerChar)
                {
                    float t = elapsed / line.secondsPerChar;
                    text.ForceMeshUpdate();
                    FadeInCharacter(text, i, t);
                    MoveChalkTip(text, i, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    elapsed += Time.deltaTime;
                }
                text.ForceMeshUpdate();
            }
        }

        /// <summary>書いている字を、左から右へ濃くする（左の頂点から先に濃くなる）</summary>
        private static void FadeInCharacter(TMP_Text text, int index, float t)
        {
            var info = text.textInfo.characterInfo[index];
            var colors = text.textInfo.meshInfo[info.materialReferenceIndex].colors32;
            int v = info.vertexIndex;
            byte left = (byte)(info.color.a * Mathf.Clamp01(t * 2f));
            byte right = (byte)(info.color.a * Mathf.Clamp01(t * 2f - 1f));
            // TMP の頂点の並び：0 左下、1 左上、2 右上、3 右下
            colors[v].a = left;
            colors[v + 1].a = left;
            colors[v + 2].a = right;
            colors[v + 3].a = right;
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        /// <summary>チョークの先を、書いている字の下のほうの、左から右へ t の位置に置く</summary>
        private void MoveChalkTip(TMP_Text text, int index, float t)
        {
            if (chalkTip == null) return;

            var info = text.textInfo.characterInfo[index];
            float y = Mathf.Lerp(info.bottomLeft.y, info.topLeft.y, 0.25f);
            Vector3 local = new Vector3(Mathf.Lerp(info.bottomLeft.x, info.bottomRight.x, t), y, 0f);
            Vector3 world = text.rectTransform.TransformPoint(local);
            chalkTip.gameObject.SetActive(true);
            chalkTip.position = world;
        }
    }
}
