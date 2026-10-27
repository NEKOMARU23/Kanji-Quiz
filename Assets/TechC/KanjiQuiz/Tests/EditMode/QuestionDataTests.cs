using System;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.RealKanji;

namespace TechC.KanjiQuiz.Tests
{
    public class QuestionDataTests
    {
        [Test]
        public void 問題セットのJSONから問題データを読む()
        {
            var repository = new LocalQuestionRepository<RealKanjiEntry>(@"{
""id"": ""Elementary"",
""sources"": [""test""],
""questions"": [
{""kanji"":""海"",""reading"":""カイ"",""radical"":""氵"",""position"":""left"",""radicalGroups"":[""kvg:06d77-g1""],""fakes"":[{""part"":""舎"",""donor"":""捨"",""groups"":[""kvg:06368-g2""],""reading"":""シャ"",""box"":[50.5,14.0,40.25,76.0]}]}
]
}");
            var entries = repository.LoadAll();

            Assert.AreEqual("Elementary", repository.SetId);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("海", entries[0].kanji);
            Assert.AreEqual("カイ", entries[0].reading);
            CollectionAssert.AreEqual(new[] { "kvg:06d77-g1" }, entries[0].radicalGroups);
            Assert.AreEqual("捨", entries[0].fakes[0].donor);
            CollectionAssert.AreEqual(new[] { "kvg:06368-g2" }, entries[0].fakes[0].groups);
            CollectionAssert.AreEqual(new[] { 50.5f, 14.0f, 40.25f, 76.0f }, entries[0].fakes[0].box);
        }

        [Test]
        public void 問題のない問題セットのJSONは読めない()
        {
            Assert.Throws<FormatException>(() => new LocalQuestionRepository<RealKanjiEntry>(@"{ ""id"": ""x"" }"));
            Assert.Throws<ArgumentException>(() => new LocalQuestionRepository<RealKanjiEntry>(""));
        }

        [Test]
        public void 熟語のJSONをまとめて読み漢字ごとに引ける()
        {
            var dictionary = KanjiWordDictionary.FromJson(
                @"{ ""id"": ""Elementary"", ""source"": ""test"", ""entries"": [ {""kanji"":""海"",""words"":[{""word"":""海外"",""reading"":""かいがい""},{""word"":""海上"",""reading"":""かいじょう""}]} ] }",
                @"{ ""id"": ""Hyogai"", ""source"": ""test"", ""entries"": [ {""kanji"":""鞄"",""words"":[{""word"":""鞄"",""reading"":""かばん""}]}, {""kanji"":""珂"",""words"":[]} ] }");

            Assert.AreEqual(3, dictionary.Count);
            var words = dictionary.GetWords("海");
            Assert.AreEqual(2, words.Count);
            Assert.AreEqual("海外", words[0].Word);
            Assert.AreEqual("かいがい", words[0].Reading);
            Assert.AreEqual("海外（かいがい）", words[0].ToString());
            Assert.AreEqual("鞄", dictionary.GetWords("鞄")[0].Word);
        }

        [Test]
        public void 熟語のない漢字と載っていない漢字は空のリストを返す()
        {
            var dictionary = KanjiWordDictionary.FromJson(@"{ ""id"": ""Hyogai"", ""entries"": [ {""kanji"":""珂"",""words"":[]} ] }");

            Assert.IsEmpty(dictionary.GetWords("珂"));
            Assert.IsEmpty(dictionary.GetWords("海"));
            Assert.IsEmpty(dictionary.GetWords(null));
        }
    }
}
