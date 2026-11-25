using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// 「ひらがなで表示」にしたとき、画面の文字に漢字が残らないことを確かめる
    /// シーン・プレハブ・モードの定義に書いた文字に、読みの印（{漢字|よみ}）のない漢字がないかを調べる
    /// </summary>
    public class UiTextReadingTests
    {
        private static readonly string Root = Path.Combine(UnityEngine.Application.dataPath, "TechC", "KanjiQuiz");

        /// <summary>画面に出る文字が入っている項目の名前</summary>
        private static readonly HashSet<string> UiKeys = new HashSet<string>
        {
            "m_text", "value", "unavailableMessage", "eraseDescription", "erasedMessage",
            "displayName", "description", "easyRange", "normalRange", "hardRange", "questionNoun",
        };

        /// <summary>
        /// 漢字のままでよい文字
        /// コードが実行時に入れ替えるひな形の文字（出題の字、例文など）と、固有名詞。ロゴは線で描くので、この文字は出ない
        /// </summary>
        private static readonly HashSet<string> Placeholders = new HashSet<string>
        {
            "1. 漢", "カイ", "一", "二", "鳥", "海", "漢", "一石二鳥", "漢字クイズ",
            "On-Jin ～音人～",
            "この問題の（かいとう）を書く。",
            "ひとつのことで、ふたつの得をすること。",
            "意味：ひとつのことで、ふたつの得をすること。",
            "歩いて通えば体もきたえられて一石二鳥だ。",
            "正解は「一石二鳥」（いっせきにちょう）\n例文：歩いて通えば体もきたえられて一石二鳥だ。",
            "本物は「海」（カイ）\n使用例：海外（かいがい）、北海道（ほっかいどう）、海上（かいじょう）",
            "読み　いっせきにちょう",
            "氵＋舎（捨の右側）",
        };

        private static readonly Regex Scalar = new Regex(
            "^[ \\t]*(?:- )?(?<key>[A-Za-z_]\\w*): \"(?<body>(?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Multiline | RegexOptions.Singleline);

        private static readonly Regex Mark = new Regex("\\{(?<kanji>[^|}]+)\\|(?<reading>[^}]*)\\}");

        private static readonly Regex HiraganaOnly = new Regex("^[ぁ-ゖー　 ]+$");

        private static IEnumerable<string> UiFiles()
        {
            foreach (var file in Directory.GetFiles(Path.Combine(Root, "Scenes"), "*.unity", SearchOption.AllDirectories)) yield return file;
            foreach (var file in Directory.GetFiles(Path.Combine(Root, "Prefabs"), "*.prefab", SearchOption.AllDirectories)) yield return file;
            foreach (var file in Directory.GetFiles(Path.Combine(Root, "Data", "Modes"), "*.asset")) yield return file;
        }

        private static IEnumerable<(string file, string text)> UiTexts()
        {
            foreach (var file in UiFiles())
            {
                string yaml = File.ReadAllText(file).Replace("\r\n", "\n");
                foreach (Match match in Scalar.Matches(yaml))
                {
                    if (!UiKeys.Contains(match.Groups["key"].Value)) continue;
                    yield return (Path.GetFileName(file), DecodeYaml(match.Groups["body"].Value));
                }
            }
        }

        /// <summary>YAML の "…" の中身を文字にする（\uXXXX、\xXX、\n、折りたたまれた改行）</summary>
        private static string DecodeYaml(string body)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];
                if (c == '\\' && i + 1 < body.Length)
                {
                    char d = body[++i];
                    switch (d)
                    {
                        case 'u':
                            builder.Append((char)System.Convert.ToInt32(body.Substring(i + 1, 4), 16));
                            i += 4;
                            break;
                        case 'x':
                            builder.Append((char)System.Convert.ToInt32(body.Substring(i + 1, 2), 16));
                            i += 2;
                            break;
                        case 'n': builder.Append('\n'); break;
                        case '\n':
                            while (i + 1 < body.Length && (body[i + 1] == ' ' || body[i + 1] == '\t')) i++;
                            break;
                        default: builder.Append(d); break;
                    }
                }
                else if (c == '\n')
                {
                    // 行の途中で折り返した改行は、空白1つとして読む（次の行の頭の空白は捨てる）
                    while (i + 1 < body.Length && (body[i + 1] == ' ' || body[i + 1] == '\t')) i++;
                    builder.Append(' ');
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        [Test]
        public void 画面の文字を見つけられる()
        {
            var texts = UiTexts().ToList();

            Assert.Greater(texts.Count, 100, "シーンやプレハブの文字が読めていない");
            Assert.IsTrue(texts.Any(t => t.text == "{次|つぎ}へ →"));
        }

        [Test]
        public void 画面の文字の漢字には読みの印がついている()
        {
            var problems = new List<string>();
            foreach (var (file, text) in UiTexts())
            {
                if (Placeholders.Contains(text)) continue;
                var unmarked = RubyText.FindUnmarkedKanji(text, string.Empty);
                if (unmarked.Count > 0) problems.Add($"{file}: 「{text.Replace("\n", "\\n")}」の {string.Join("", unmarked)}");
            }

            Assert.IsEmpty(problems, "読みの印（{漢字|よみ}）のない漢字がある（ひらがなにできない）:\n" + string.Join("\n", problems));
        }

        [Test]
        public void 読みの印の読みは全部ひらがな()
        {
            var problems = new List<string>();
            foreach (var (file, text) in UiTexts())
            {
                foreach (Match mark in Mark.Matches(text))
                {
                    string reading = mark.Groups["reading"].Value;
                    if (!HiraganaOnly.IsMatch(reading)) problems.Add($"{file}: {mark.Value}");
                }
            }

            Assert.IsEmpty(problems, "読みがひらがなではない:\n" + string.Join("\n", problems));
        }

        [Test]
        public void ひらがなにすると画面の文字に漢字が残らない()
        {
            var problems = new List<string>();
            foreach (var (file, text) in UiTexts())
            {
                if (Placeholders.Contains(text)) continue;
                string hiragana = RubyText.Render(text, useHiragana: true);
                if (RubyText.FindUnmarkedKanji(hiragana, string.Empty).Count > 0) problems.Add($"{file}: {hiragana}");
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void 漢字のままにすると印がすべて外れる()
        {
            foreach (var (file, text) in UiTexts())
            {
                string kanji = RubyText.Render(text, useHiragana: false);
                Assert.IsFalse(kanji.Contains("{") && kanji.Contains("|"), $"{file}: 印が残っている {kanji}");
            }
        }

        [TestCase(Difficulty.Easy)]
        [TestCase(Difficulty.Normal)]
        [TestCase(Difficulty.Hard)]
        public void 難易度の名前の印を外すと元の名前になる(Difficulty difficulty)
        {
            Assert.AreEqual(difficulty.ToDisplayName(), RubyText.Render(difficulty.ToMarkedName(), useHiragana: false));
            Assert.IsEmpty(RubyText.FindUnmarkedKanji(difficulty.ToMarkedName(), string.Empty));
        }

        [Test]
        public void 級の名前の印を外すと元の名前になる()
        {
            for (int level = 0; level <= 10; level++)
            {
                string name = KanjiRank.NameOf(level);
                string marked = KanjiRank.ToMarked(name);

                Assert.AreEqual(name, RubyText.Render(marked, useHiragana: false));
                Assert.IsEmpty(RubyText.FindUnmarkedKanji(marked, string.Empty), name);
                Assert.IsTrue(HiraganaOnly.IsMatch(RubyText.Render(marked, useHiragana: true)), name);
            }
        }

        [Test]
        public void 級の名前でなければそのまま返す()
        {
            Assert.AreEqual("まだ", KanjiRank.ToMarked("まだ"));
            Assert.IsNull(KanjiRank.ToMarked(null));
        }
    }
}
