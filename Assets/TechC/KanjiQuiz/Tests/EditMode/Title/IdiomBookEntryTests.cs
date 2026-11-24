using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Screens.Title;
using UnityEditor;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>漢字帳の語のタブに出す語を、読み当て・使い分けの問題データから作る処理のテスト</summary>
    public class IdiomBookEntryTests
    {
        [Test]
        public void 読み当てのデータから語と読みを読み正しい読みをすべてつなぐ()
        {
            var entries = IdiomBookEntry.FromReadingQuizJson(
                "{\"id\":\"Easy\",\"questions\":[" +
                "{\"word\":\"今日\",\"readings\":[\"きょう\",\"こんにち\"],\"wrong\":[\"こんじつ\"],\"common\":true}," +
                "{\"word\":\"明日\",\"readings\":[\"あした\"],\"wrong\":[],\"common\":true}," +
                "{\"word\":\"今日\",\"readings\":[\"ちがう\"],\"wrong\":[],\"common\":false}]}");

            Assert.AreEqual(2, entries.Count, "同じ語は最初の1つにまとめる");
            Assert.AreEqual("今日", entries[0].Word);
            Assert.AreEqual("きょう、こんにち", entries[0].Reading);
            Assert.AreEqual(string.Empty, entries[0].Meaning, "意味のデータを渡さなければ空");
            Assert.AreEqual(string.Empty, entries[0].Example);
        }

        [Test]
        public void 読み当ての意味と例文のデータがあれば語ごとにつけ_ない語は空にする()
        {
            var entries = IdiomBookEntry.FromReadingQuizJson(
                "{\"entries\":[{\"word\":\"明日\",\"meaning\":\"{今日|きょう}の次の日。\",\"example\":\"明日は{晴|は}れる。\"}," +
                "{\"word\":\"明日\",\"meaning\":\"2つ目は使わない\",\"example\":\"x\"}]}",
                "{\"questions\":[" +
                "{\"word\":\"今日\",\"readings\":[\"きょう\"],\"wrong\":[],\"common\":true}," +
                "{\"word\":\"明日\",\"readings\":[\"あした\"],\"wrong\":[],\"common\":true}]}");

            Assert.AreEqual(2, entries.Count, "意味のデータだけの語は並べない");
            Assert.AreEqual(string.Empty, entries[0].Meaning, "データにない語は空");
            Assert.AreEqual(string.Empty, entries[0].Example);
            Assert.AreEqual("{今日|きょう}の次の日。", entries[1].Meaning, "意味のデータが先に渡されても、語につく。同じ語は最初の1つ");
            Assert.AreEqual("明日は{晴|は}れる。", entries[1].Example);
        }

        [Test]
        public void 使い分けのデータから正解の言葉と意味と空いたところに正解を入れた例文を読む()
        {
            var entries = IdiomBookEntry.FromKanjiUsageJson(
                "{\"id\":\"Easy\",\"questions\":[" +
                "{\"reading\":\"かいとう\",\"answer\":\"回答\",\"sentence\":\"{質問|しつもん}の＿を{書|か}く。\",\"wrong\":[\"解答\",\"解凍\"],\"meanings\":[\"{答|こた}えること。\",\"意味2\",\"意味3\"]}]}",
                "{\"id\":\"Hard\",\"questions\":[" +
                "{\"reading\":\"かいとう\",\"answer\":\"回答\",\"sentence\":\"別の＿。\",\"wrong\":[],\"meanings\":[\"重なり\"]}]}");

            Assert.AreEqual(1, entries.Count, "ファイルをまたいで同じ言葉は、最初の1つにまとめる");
            Assert.AreEqual("回答", entries[0].Word);
            Assert.AreEqual("かいとう", entries[0].Reading);
            Assert.AreEqual("{答|こた}えること。", entries[0].Meaning, "正解の言葉の意味（先頭）");
            Assert.AreEqual("{質問|しつもん}の回答を{書|か}く。", entries[0].Example, "空いたところに正解を入れる。言葉には読みの印をつけない");
        }

        [Test]
        public void データが空やないときは空の一覧を返す()
        {
            Assert.IsEmpty(IdiomBookEntry.FromReadingQuizJson());
            Assert.IsEmpty(IdiomBookEntry.FromReadingQuizJson(null, string.Empty, "{\"questions\":[]}"));
            Assert.IsEmpty(IdiomBookEntry.FromKanjiUsageJson(null, string.Empty, "{\"questions\":[]}"));
        }

        [TestCase("Easy")]
        [TestCase("Normal")]
        [TestCase("Hard")]
        public void 同梱の読み当てと使い分けのデータから語を作れる(string level)
        {
            var reading = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/TechC/KanjiQuiz/Data/ReadingQuiz/ReadingQuiz_{level}.json");
            var usage = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/TechC/KanjiQuiz/Data/KanjiUsage/KanjiUsage_{level}.json");
            Assert.IsNotNull(reading, level);
            Assert.IsNotNull(usage, level);

            var readingEntries = IdiomBookEntry.FromReadingQuizJson(reading.text);
            var usageEntries = IdiomBookEntry.FromKanjiUsageJson(usage.text);

            Assert.GreaterOrEqual(readingEntries.Count, 100, level);
            Assert.IsTrue(readingEntries.All(e => !string.IsNullOrEmpty(e.Word) && !string.IsNullOrEmpty(e.Reading)), $"{level} の読み当ての語に読みがある");
            Assert.GreaterOrEqual(usageEntries.Count, 100, level);
            Assert.IsTrue(usageEntries.All(e => !string.IsNullOrEmpty(e.Reading) && !string.IsNullOrEmpty(e.Meaning)), $"{level} の使い分けの言葉に読みと意味がある");
            Assert.IsTrue(usageEntries.All(e => e.Example.Contains(e.Word) && !e.Example.Contains("＿")), $"{level} の例文に正解の言葉が入っている");
        }
    }
}
