using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// SVG のパスデータ（d 属性）を、描画用の座標の列に変換する
    /// 対応コマンド：M, L, H, V, C, S, Z（大文字・小文字とも）
    /// </summary>
    public static class StrokePathParser
    {
        /// <summary>KanjiVG の座標系の大きさ（109×109）</summary>
        public const float KanjiVgSize = 109f;

        /// <summary>曲線を近似するときの1区間あたりの分割数</summary>
        public const int BezierSegments = 30;

        private static readonly Regex CommandRegex = new Regex(@"([MmLlHhVvCcSsZz])([^MmLlHhVvCcSsZz]*)", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new Regex(@"-?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);

        /// <summary>
        /// パスデータを座標の列に変換する
        /// 座標は KanjiVG の座標系の中心を原点にし、上を +Y にして scale 倍する
        /// </summary>
        /// <param name="d">SVG の d 属性の文字列</param>
        /// <param name="scale">拡大率。1 のとき1文字の幅が 1 になる</param>
        public static StrokeData Parse(string d, float scale = 3f)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));

            var points = new List<Vector3>();
            Vector2 current = Vector2.zero;
            Vector2 start = Vector2.zero;
            Vector2 lastControl = Vector2.zero;
            bool hasLastControl = false;

            foreach (Match match in CommandRegex.Matches(d))
            {
                char command = match.Groups[1].Value[0];
                bool isRelative = char.IsLower(command);
                List<float> args = ParseNumbers(match.Groups[2].Value);
                int idx = 0;

                switch (char.ToUpperInvariant(command))
                {
                    case 'M':
                        while (idx + 1 < args.Count)
                        {
                            current = ToPoint(isRelative, current, args[idx], args[idx + 1]);
                            if (idx == 0) start = current;
                            idx += 2;
                        }
                        hasLastControl = false;
                        break;

                    case 'L':
                        while (idx + 1 < args.Count)
                        {
                            var next = ToPoint(isRelative, current, args[idx], args[idx + 1]);
                            AddLine(points, current, next, scale);
                            current = next;
                            idx += 2;
                        }
                        hasLastControl = false;
                        break;

                    case 'H':
                        while (idx < args.Count)
                        {
                            var next = new Vector2(isRelative ? current.x + args[idx] : args[idx], current.y);
                            AddLine(points, current, next, scale);
                            current = next;
                            idx++;
                        }
                        hasLastControl = false;
                        break;

                    case 'V':
                        while (idx < args.Count)
                        {
                            var next = new Vector2(current.x, isRelative ? current.y + args[idx] : args[idx]);
                            AddLine(points, current, next, scale);
                            current = next;
                            idx++;
                        }
                        hasLastControl = false;
                        break;

                    case 'C':
                        while (idx + 5 < args.Count)
                        {
                            var p1 = ToPoint(isRelative, current, args[idx], args[idx + 1]);
                            var p2 = ToPoint(isRelative, current, args[idx + 2], args[idx + 3]);
                            var p3 = ToPoint(isRelative, current, args[idx + 4], args[idx + 5]);
                            AddBezier(points, current, p1, p2, p3, scale);
                            current = p3;
                            lastControl = p2;
                            hasLastControl = true;
                            idx += 6;
                        }
                        break;

                    case 'S':
                        while (idx + 3 < args.Count)
                        {
                            var p1 = hasLastControl ? 2 * current - lastControl : current;
                            var p2 = ToPoint(isRelative, current, args[idx], args[idx + 1]);
                            var p3 = ToPoint(isRelative, current, args[idx + 2], args[idx + 3]);
                            AddBezier(points, current, p1, p2, p3, scale);
                            current = p3;
                            lastControl = p2;
                            hasLastControl = true;
                            idx += 4;
                        }
                        break;

                    case 'Z':
                        AddLine(points, current, start, scale);
                        current = start;
                        hasLastControl = false;
                        break;
                }
            }

            return new StrokeData(points);
        }

        /// <summary>
        /// KanjiVG の座標を、中心が原点で上が +Y の座標に変換する
        /// </summary>
        public static Vector3 Normalize(Vector2 p, float scale)
        {
            float half = KanjiVgSize / 2f;
            float x = (p.x - half) / KanjiVgSize * scale;
            float y = -(p.y - half) / KanjiVgSize * scale;
            return new Vector3(x, y, 0f);
        }

        private static List<float> ParseNumbers(string text)
        {
            var numbers = new List<float>();
            foreach (Match m in NumberRegex.Matches(text))
            {
                numbers.Add(float.Parse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture));
            }
            return numbers;
        }

        private static Vector2 ToPoint(bool isRelative, Vector2 current, float x, float y)
        {
            var p = new Vector2(x, y);
            return isRelative ? current + p : p;
        }

        private static void AddLine(List<Vector3> points, Vector2 from, Vector2 to, float scale)
        {
            points.Add(Normalize(from, scale));
            points.Add(Normalize(to, scale));
        }

        private static void AddBezier(List<Vector3> points, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float scale)
        {
            for (int i = 0; i <= BezierSegments; i++)
            {
                float t = i / (float)BezierSegments;
                float mt = 1f - t;
                Vector2 point = mt * mt * mt * p0
                                + 3f * mt * mt * t * p1
                                + 3f * mt * t * t * p2
                                + t * t * t * p3;
                points.Add(Normalize(point, scale));
            }
        }
    }
}
