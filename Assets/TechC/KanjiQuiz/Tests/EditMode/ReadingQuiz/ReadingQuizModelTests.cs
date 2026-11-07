using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Modes.ReadingQuiz;

namespace TechC.KanjiQuiz.Tests
{
    public class ReadingQuizModelTests
    {
        private static ReadingQuizEntry Entry(string word, string[] readings, params string[] wrong)
        {
            return new ReadingQuizEntry { word = word, readings = readings, wrong = wrong, common = true };
        }

        [Test]
        public void ChoiceCount_SingleAnswerIsThreeOrFour()
        {
            Assert.AreEqual(3, ReadingQuizRule.ChoiceCount(Difficulty.Easy, 1));
            Assert.AreEqual(4, ReadingQuizRule.ChoiceCount(Difficulty.Normal, 1));
            Assert.AreEqual(4, ReadingQuizRule.ChoiceCount(Difficulty.Hard, 1));
        }

        [Test]
        public void ChoiceCount_MultipleAnswerAddsTwo()
        {
            Assert.AreEqual(5, ReadingQuizRule.ChoiceCount(Difficulty.Easy, 2));
            Assert.AreEqual(6, ReadingQuizRule.ChoiceCount(Difficulty.Normal, 2));
            Assert.AreEqual(6, ReadingQuizRule.ChoiceCount(Difficulty.Hard, 3));
        }

        [Test]
        public void ChoiceCount_ZeroCorrectThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ReadingQuizRule.ChoiceCount(Difficulty.Easy, 0));
        }

        [Test]
        public void Answer_EncodeDecodeRoundTrip()
        {
            int mask = ReadingQuizAnswer.Encode(new[] { 0, 3, 5 });

            Assert.AreEqual(new List<int> { 0, 3, 5 }, ReadingQuizAnswer.Decode(mask));
            Assert.IsTrue(ReadingQuizAnswer.IsSelected(mask, 3));
            Assert.IsFalse(ReadingQuizAnswer.IsSelected(mask, 1));
        }

        [Test]
        public void Answer_OrderDoesNotMatter()
        {
            Assert.AreEqual(ReadingQuizAnswer.Encode(new[] { 1, 4 }), ReadingQuizAnswer.Encode(new[] { 4, 1 }));
        }

        [Test]
        public void Answer_UnansweredDecodesToNothing()
        {
            Assert.IsEmpty(ReadingQuizAnswer.Decode(-1));
            Assert.IsEmpty(ReadingQuizAnswer.Decode(0));
            Assert.IsFalse(ReadingQuizAnswer.IsSelected(-1, 0));
        }

        [Test]
        public void Answer_OutOfRangeThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ReadingQuizAnswer.Encode(new[] { 6 }));
        }

        [Test]
        public void Question_IsCorrectOnlyWhenEverySelectionMatches()
        {
            var question = new ReadingQuizQuestion("今日", new[] { "きょう", "こんにち", "あした" }, ReadingQuizAnswer.Encode(new[] { 0, 1 }));

            Assert.IsTrue(question.IsMultiple);
            Assert.AreEqual(2, question.CorrectCount);
            Assert.IsTrue(question.IsCorrect(ReadingQuizAnswer.Encode(new[] { 0, 1 })));
            Assert.IsFalse(question.IsCorrect(ReadingQuizAnswer.Encode(new[] { 0 })), "一部だけ選んだら不正解");
            Assert.IsFalse(question.IsCorrect(ReadingQuizAnswer.Encode(new[] { 0, 1, 2 })), "まちがいを足したら不正解");
        }

        [Test]
        public void Question_CorrectMaskOutsideChoicesThrows()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new ReadingQuizQuestion("熟語", new[] { "よみ", "ちがう" }, 1 << 3));
        }

        [Test]
        public void Factory_SingleAnswerHasCorrectReadingAndRightChoiceCount()
        {
            var entry = Entry("紫陽花", new[] { "あじさい" }, "むらさき", "ようか", "しよう");

            var question = ReadingQuizQuestionFactory.CreateOrNull(entry, Difficulty.Normal);

            Assert.IsNotNull(question);
            Assert.AreEqual(4, question.ChoiceCount);
            Assert.IsFalse(question.IsMultiple);
            Assert.Contains("あじさい", question.Choices.ToList());
            Assert.AreEqual(1 << question.Choices.ToList().IndexOf("あじさい"), question.CorrectMask);
        }

        [Test]
        public void Factory_MultipleAnswerHasAllCorrectReadings()
        {
            var entry = Entry("万華鏡", new[] { "まんげきょう", "ばんかきょう" }, "かんきょう", "たまきょう", "まんがきょう", "ばんげきょう", "まんかきょう");

            var question = ReadingQuizQuestionFactory.CreateOrNull(entry, Difficulty.Hard);

            Assert.IsNotNull(question);
            Assert.AreEqual(6, question.ChoiceCount, "複数回答は選択肢が2つ多い");
            Assert.AreEqual(2, question.CorrectCount);
            foreach (string reading in entry.readings)
            {
                Assert.Contains(reading, question.Choices.ToList());
            }
            Assert.AreEqual(2, ReadingQuizAnswer.Decode(question.CorrectMask).Count);
        }

        [Test]
        public void Factory_WrongReadingsNeverMatchCorrectOnes()
        {
            // 正しい読みをまちがいにも入れておいても、まちがいには使わない
            var entry = Entry("一人", new[] { "ひとり", "いちにん" }, "ひとり", "いちにん", "ふたり", "さんにん", "よにん", "ごにん");

            var question = ReadingQuizQuestionFactory.CreateOrNull(entry, Difficulty.Easy);

            Assert.IsNotNull(question);
            var wrongChoices = question.Choices.Where((_, i) => !ReadingQuizAnswer.IsSelected(question.CorrectMask, i)).ToList();
            CollectionAssert.DoesNotContain(wrongChoices, "ひとり");
            CollectionAssert.DoesNotContain(wrongChoices, "いちにん");
        }

        [Test]
        public void Question_CorrectReadingsAreTheCorrectChoices()
        {
            var question = new ReadingQuizQuestion("今日", new[] { "きょう", "こんじつ", "こんにち" }, (1 << 0) | (1 << 2));

            CollectionAssert.AreEqual(new[] { "きょう", "こんにち" }, question.CorrectReadings.ToArray());
        }

        [Test]
        public void Factory_ReturnsNullWhenNotEnoughWrongReadings()
        {
            var entry = Entry("熟語", new[] { "よみ" }, "ちがう1");

            Assert.IsNull(ReadingQuizQuestionFactory.CreateOrNull(entry, Difficulty.Easy), "簡単は選択肢が3つ、まちがいが2つ要る");
        }

        [Test]
        public void Factory_CreateReturnsRequestedCountAndSkipsBadEntries()
        {
            var entries = new List<ReadingQuizEntry>
            {
                Entry("一", new[] { "いち" }, "ひと", "ふた", "み"),
                Entry("二", new[] { "に" }, "さん"), // まちがいが足りない
                Entry("三", new[] { "さん" }, "いち", "に", "よん"),
                Entry("四", new[] { "よん" }, "いち", "に", "さん"),
            };

            var questions = ReadingQuizQuestionFactory.Create(entries, Difficulty.Easy, 2);

            Assert.AreEqual(2, questions.Count);
            CollectionAssert.DoesNotContain(questions.Select(q => q.Word).ToList(), "二");
        }

        [Test]
        public void Factory_ChoicesAreFixedAfterCreation()
        {
            var entry = Entry("長女", new[] { "ちょうじょ" }, "ながめ", "ながじょ", "ちょうめ");
            var question = ReadingQuizQuestionFactory.CreateOrNull(entry, Difficulty.Easy);

            var first = question.Choices.ToList();
            var second = question.Choices.ToList();

            CollectionAssert.AreEqual(first, second);
        }
    }
}
