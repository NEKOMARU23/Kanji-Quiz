using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// StreamingAssets/KanjiVG の SVG から書き順データを読み込む
    /// UnityWebRequest を使うので、ビルド版（スマートフォンを含む）でも読める
    /// </summary>
    public sealed class StreamingAssetsKanjiStrokeSource : IKanjiStrokeSource, IKanjiSvgSource
    {
        /// <summary>StreamingAssets の中の KanjiVG のフォルダ名</summary>
        public const string FolderName = "KanjiVG";

        public async UniTask<IReadOnlyList<string>> LoadStrokePathsAsync(string kanji, CancellationToken ct)
        {
            string svgText = await LoadSvgTextAsync(kanji, ct);
            return svgText == null ? Array.Empty<string>() : KanjiSvgParser.ExtractStrokePaths(svgText);
        }

        public async UniTask<IReadOnlyList<KanjiSvgStroke>> LoadStrokesAsync(string kanji, CancellationToken ct)
        {
            string svgText = await LoadSvgTextAsync(kanji, ct);
            return svgText == null ? Array.Empty<KanjiSvgStroke>() : KanjiSvgParser.ExtractStrokes(svgText);
        }

        /// <returns>SVG のテキスト。読み込めなければ null</returns>
        private static async UniTask<string> LoadSvgTextAsync(string kanji, CancellationToken ct)
        {
            string path = Path.Combine(Application.streamingAssetsPath, FolderName, KanjiSvgParser.ToFileName(kanji));
            string url = path.Contains("://") ? path : new Uri(path).AbsoluteUri;

            using (var request = UnityWebRequest.Get(url))
            {
                try
                {
                    await request.SendWebRequest().WithCancellation(ct);
                }
                catch (UnityWebRequestException e)
                {
                    Debug.LogWarning($"[StreamingAssetsKanjiStrokeSource] 漢字 '{kanji}' の書き順データを読み込めません: {e.Message}");
                    return null;
                }

                return request.downloadHandler.text;
            }
        }
    }
}
