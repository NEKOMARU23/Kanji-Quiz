using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.KanjiUsage;
using UnityEditor;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// 同梱している使い分けの問題データ（Data/KanjiUsage）を、ゲームと同じ読み方で確かめる
    /// 文・候補・意味の中身は、Tools/generate_kanji_usage.py が確かめている。ここでは、JSON の項目名と、問題を作れることを確かめる
    /// </summary>
    public class KanjiUsageDataTests
    {
        [TestCase("Easy", 3, 3)]
        [TestCase("Normal", 4, 4)]
        [TestCase("Hard", 3, 4)]
        public void 同梱の問題データのすべてから問題を作れて選択肢の数と中身が決まりどおりになる(string level, int minChoices, int maxChoices)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>($"Assets/TechC/KanjiQuiz/Data/KanjiUsage/KanjiUsage_{level}.json");
            Assert.IsNotNull(asset, level);
            var repository = new LocalQuestionRepository<KanjiUsageEntry>(asset.text);
            var entries = repository.LoadAll();
            Assert.AreEqual(level, repository.SetId);
            Assert.GreaterOrEqual(entries.Count, 100, $"{level} の問題の数");

            foreach (var entry in entries)
            {
                var question = KanjiUsageQuestionFactory.CreateOrNull(entry);
                Assert.IsNotNull(question, $"{level} {entry.answer} から問題を作れない");

                Assert.GreaterOrEqual(question.ChoiceCount, minChoices, $"{level} {entry.answer} の選択肢の数");
                Assert.LessOrEqual(question.ChoiceCount, maxChoices, $"{level} {entry.answer} の選択肢の数");
                Assert.AreEqual(entry.answer, question.Answer, entry.answer);
                Assert.AreEqual(entry.meanings[0], question.Choices[question.CorrectIndex].Meaning, $"{entry.answer} の意味");
                Assert.IsTrue(question.Choices.All(choice => !string.IsNullOrEmpty(choice.Meaning)), $"{entry.answer} の意味がない候補がある");
                Assert.IsNotEmpty(question.Reading, entry.answer);

                // 文と意味の漢字には読みがついていて、読みの印は表示で取り除ける
                CollectionAssert.IsEmpty(RubyText.FindUnmarkedKanji(entry.sentence, string.Empty), $"{entry.answer} の文に読みのない漢字がある");
                StringAssert.DoesNotContain("{", RubyText.Render(entry.sentence, false), entry.answer);
                StringAssert.DoesNotContain("{", RubyText.Render(entry.sentence, true), entry.answer);
            }

            // 苦手な言葉を正解の言葉で覚えるので、同じ難易度で正解の言葉が重ならない
            var answers = entries.Select(entry => entry.answer).ToList();
            CollectionAssert.AllItemsAreUnique(answers, $"{level} の正解の言葉が重なっている");
        }
    }
}
