using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using UnityEditor;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// 同梱している意味と例文のデータ（Data/KanjiMeanings）を、ゲームと同じ読み方で確かめる
    /// </summary>
    public class KanjiMeaningDataTests
    {
        private const string Folder = "Assets/TechC/KanjiQuiz/Data/KanjiMeanings/";

        [TestCase("Elementary", 737)]
        [TestCase("JoyoCommon", 498)]
        [TestCase("JoyoRare", 495)]
        public void 出題する字の意味と例文がすべてあり読みのない漢字がない(string level, int expectedCount)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + $"KanjiMeanings_{level}.json");
            Assert.IsNotNull(asset, "データのファイルがない");

            var questions = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/TechC/KanjiQuiz/Data/RealKanji/RealKanji_{level}.json");
            var entries = new LocalQuestionRepository<Modes.RealKanji.RealKanjiEntry>(questions.text).LoadAll();
            var dictionary = KanjiMeaningDictionary.FromJson(asset.text);
            Assert.AreEqual(expectedCount, dictionary.Count);

            foreach (var entry in entries)
            {
                var meaning = dictionary.Get(entry.kanji);
                Assert.IsFalse(meaning.IsEmpty, $"{entry.kanji} の意味と例文がない");
                CollectionAssert.IsEmpty(RubyText.FindUnmarkedKanji(meaning.Meaning, entry.kanji), $"{entry.kanji} の意味に読みのない漢字がある");
                CollectionAssert.IsEmpty(RubyText.FindUnmarkedKanji(meaning.Example, entry.kanji), $"{entry.kanji} の例文に読みのない漢字がある");
                StringAssert.Contains(entry.kanji, RubyText.Render(meaning.Example, useHiragana: true), $"{entry.kanji} の例文に出題する字がない");
            }
        }
    }
}
