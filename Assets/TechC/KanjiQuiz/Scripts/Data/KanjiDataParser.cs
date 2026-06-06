using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

namespace TechC.KanjiQuiz.Data
{
    public class KanjiDataParser
    {
        private float scaleFactor = 3.0f;

        // 漢字からSVGファイルの絶対パスを生成する
        public string MakeSingleKanjiStrokePath(string kanji)
        {
            var code = $"{char.ConvertToUtf32(kanji, 0):X4}";
            return $"{Application.dataPath}/StreamingAssets/SVG/0{code.ToLower()}.svg";
        }

        // 漢字データ（SVG）を読み込み、ストロークごとのテキストデータを抽出する
        public List<string> LoadSingleKanjiStrokes(string kanji)
        {
            var path = MakeSingleKanjiStrokePath(kanji);
            return LoadStrokeSVG(path);
        }

        private List<string> LoadStrokeSVG(string path)
        {
            List<string> strokePaths = new List<string>();
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList paths = doc.GetElementsByTagName("path");

            foreach (XmlNode node in paths)
            {
                var dAttr = node.Attributes["d"];
                var idAttr = node.Attributes["id"];
                if (dAttr != null && idAttr != null && idAttr.Value.Contains("-s"))
                    strokePaths.Add(dAttr.Value);
            }

            return strokePaths;
        }

        // ストロークのテキストデータ（d属性）を、Unityの座標（Vector3）のリストに変換する
        public List<List<Vector3>> GenerateStrokePoints(List<string> strokes, bool correctOrder)
        {
            List<string> strokeCopy = new List<string>(strokes);
            if (!correctOrder) ShuffleList(strokeCopy);

            List<List<Vector3>> strokePointsList = new List<List<Vector3>>();
            foreach (string d in strokeCopy)
                strokePointsList.Add(ParsePath(d));

            return strokePointsList;
        }

        private List<Vector3> ParsePath(string d)
        {
            List<Vector3> points = new List<Vector3>();
            try
            {
                MatchCollection matches = Regex.Matches(d, @"([MmLlHhVvCcSsZz])([^MmLlHhVvCcSsZz]*)");
                Vector2 currentPos = Vector2.zero;
                Vector2 startPoint = Vector2.zero;
                Vector2 lastControlPoint = Vector2.zero;
                bool lastControlPointSet = false;

                foreach (Match match in matches)
                {
                    string command = match.Groups[1].Value;
                    string argsStr = match.Groups[2].Value.Trim();
                    List<float> args = new List<float>();
                    foreach (Match m in Regex.Matches(argsStr, @"-?\d+\.?\d*"))
                        args.Add(float.Parse(m.Value));

                    int idx = 0;

                    switch (command)
                    {
                        case "M":
                        case "m":
                            while (idx + 1 < args.Count)
                            {
                                Vector2 p = (command == "M") ? new Vector2(args[idx], args[idx + 1]) : currentPos + new Vector2(args[idx], args[idx + 1]);
                                currentPos = p;
                                if (idx == 0) startPoint = currentPos;
                                idx += 2;
                            }
                            lastControlPointSet = false;
                            break;
                        case "L":
                        case "l":
                            while (idx + 1 < args.Count)
                            {
                                Vector2 p = (command == "L") ? new Vector2(args[idx], args[idx + 1]) : currentPos + new Vector2(args[idx], args[idx + 1]);
                                points.Add(Normalize(currentPos));
                                points.Add(Normalize(p));
                                currentPos = p;
                                idx += 2;
                            }
                            lastControlPointSet = false;
                            break;
                        case "H":
                        case "h":
                            while (idx < args.Count)
                            {
                                Vector2 p = (command == "H") ? new Vector2(args[idx], currentPos.y) : currentPos + new Vector2(args[idx], 0);
                                points.Add(Normalize(currentPos));
                                points.Add(Normalize(p));
                                currentPos = p;
                                idx++;
                            }
                            lastControlPointSet = false;
                            break;
                        case "V":
                        case "v":
                            while (idx < args.Count)
                            {
                                Vector2 p = (command == "V") ? new Vector2(currentPos.x, args[idx]) : currentPos + new Vector2(0, args[idx]);
                                points.Add(Normalize(currentPos));
                                points.Add(Normalize(p));
                                currentPos = p;
                                idx++;
                            }
                            lastControlPointSet = false;
                            break;
                        case "C":
                        case "c":
                            while (idx + 5 < args.Count)
                            {
                                Vector2 p0 = currentPos;
                                Vector2 p1 = (command == "C") ? new Vector2(args[idx], args[idx + 1]) : currentPos + new Vector2(args[idx], args[idx + 1]);
                                Vector2 p2 = (command == "C") ? new Vector2(args[idx + 2], args[idx + 3]) : currentPos + new Vector2(args[idx + 2], args[idx + 3]);
                                Vector2 p3 = (command == "C") ? new Vector2(args[idx + 4], args[idx + 5]) : currentPos + new Vector2(args[idx + 4], args[idx + 5]);
                                points.AddRange(SampleBezier(p0, p1, p2, p3));
                                currentPos = p3;
                                lastControlPoint = p2;
                                lastControlPointSet = true;
                                idx += 6;
                            }
                            break;
                        case "S":
                        case "s":
                            while (idx + 3 < args.Count)
                            {
                                Vector2 p0 = currentPos;
                                Vector2 p1 = lastControlPointSet ? 2 * currentPos - lastControlPoint : currentPos;
                                Vector2 p2 = (command == "S") ? new Vector2(args[idx], args[idx + 1]) : currentPos + new Vector2(args[idx], args[idx + 1]);
                                Vector2 p3 = (command == "S") ? new Vector2(args[idx + 2], args[idx + 3]) : currentPos + new Vector2(args[idx + 2], args[idx + 3]);
                                points.AddRange(SampleBezier(p0, p1, p2, p3));
                                currentPos = p3;
                                lastControlPoint = p2;
                                lastControlPointSet = true;
                                idx += 4;
                            }
                            break;
                        case "Z":
                        case "z":
                            points.Add(Normalize(currentPos));
                            points.Add(Normalize(startPoint));
                            currentPos = startPoint;
                            lastControlPointSet = false;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ParsePath エラー: " + e.Message);
            }
            return points;
        }

        private Vector3 Normalize(Vector2 p)
        {
            float size = 109f;
            float x = (p.x - size / 2f) / size * scaleFactor;
            float y = -(p.y - size / 2f) / size * scaleFactor;
            return new Vector3(x, y, 0);
        }

        private List<Vector3> SampleBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int segments = 30)
        {
            List<Vector3> sampledPoints = new List<Vector3>();
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float mt = 1 - t;
                Vector2 point = mt * mt * mt * p0 +
                                3 * mt * mt * t * p1 +
                                3 * mt * t * t * p2 +
                                t * t * t * p3;
                sampledPoints.Add(Normalize(point));
            }
            return sampledPoints;
        }

        private void ShuffleList<T>(List<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}