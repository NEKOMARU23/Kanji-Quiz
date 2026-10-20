using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// StreamingAssets/KanjiVG の SVG から書き順データを読み込む
    /// UnityWebRequest を使うので、ビルド版（スマートフォンを含む）でも読める
    /// </summary>
    public sealed class StreamingAssetsKanjiStrokeSource : IKanjiStrokeSource
    {
        /// <summary>StreamingAssets の中の KanjiVG のフォルダ名</summary>
        public const string FolderName = "KanjiVG";

        public async UniTask<IReadOnlyList<string>> LoadStrokePathsAsync(string kanji, CancellationToken ct)
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
                    return Array.Empty<string>();
                }

                return KanjiSvgParser.ExtractStrokePaths(request.downloadHandler.text);
            }
        }
    }
}
