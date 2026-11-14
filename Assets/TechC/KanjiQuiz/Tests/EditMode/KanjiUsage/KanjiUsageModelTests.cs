using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TechC.KanjiQuiz.Modes.KanjiUsage;

namespace TechC.KanjiQuiz.Tests
{
    public class KanjiUsageModelTests
    {
        private const string Sentence = "この{問題|もんだい}の＿を書く。";

        private static KanjiUsageEntry Entry(string answer, string[] wrong, string sentence = Sentence, string[] meanings = null)
        {
            return new KanjiUsageEntry
            {
                reading = "かいとう",
                answer = answer,
                sentence = sentence,
                wrong = wrong,
                meanings = meanings ?? new[] { "意味1", "意味2", "意味3", "意味4", "意味5", "意味6" },
            };
        }

        [Test]
        public void 選択肢の数はまちがいの数に1を足した数になる()
        {
            Assert.AreEqual(3, KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍" })).ChoiceCount);
            Assert.AreEqual(4, KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍", "会頭" })).ChoiceCount);
            Assert.AreEqual(6, KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "a", "b", "c", "d", "e" })).ChoiceCount);
        }

        [Test]
        public void 正解の番号の選択肢が正解の言葉になる()
        {
            for (int i = 0; i < 20; i++)
            {
                var question = KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍", "会頭" }));

                Assert.AreEqual("回答", question.Choices[question.CorrectIndex].Text);
                Assert.AreEqual("回答", question.Answer);
            }
        }

        [Test]
        public void シャッフルしても言葉と意味の組は変わらない()
        {
            var expected = new Dictionary<string, string>
            {
                { "回答", "意味1" }, { "解答", "意味2" }, { "解凍", "意味3" }, { "会頭", "意味4" },
            };

            for (int i = 0; i < 30; i++)
            {
                var question = KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍", "会頭" }));

                foreach (var choice in question.Choices)
                {
                    Assert.AreEqual(expected[choice.Text], choice.Meaning, $"{choice.Text} の意味");
                }
            }
        }

        [Test]
        public void 作った問題の選択肢の並びは変わらない()
        {
            var question = KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍" }));

            var first = question.Choices.Select(c => c.Text).ToList();
            var second = question.Choices.Select(c => c.Text).ToList();

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void 文の空いたところが1つでなければ作れない()
        {
            Assert.IsNull(KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍" }, "空いたところがない文。")));
            Assert.IsNull(KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍" }, "＿が＿つある。")));
        }

        [Test]
        public void まちがいの数が少なすぎても多すぎても作れない()
        {
            Assert.IsNull(KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答" })), "選択肢は3つ以上");
            Assert.IsNull(KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "a", "b", "c", "d", "e", "f" })), "選択肢は6つまで");
            Assert.IsNull(KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", null)));
        }

        [Test]
        public void 空の言葉と重なりと正解と同じ言葉はまちがいから取り除く()
        {
            // 取り除いたあとのまちがいは「解答」「解凍」の2つ
            var question = KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "", "解答", "回答", "解凍" }));

            Assert.IsNotNull(question);
            CollectionAssert.AreEquivalent(new[] { "回答", "解答", "解凍" }, question.Choices.Select(c => c.Text).ToList());
        }

        [Test]
        public void 意味が足りなくても問題は作れて足りない分は空になる()
        {
            var question = KanjiUsageQuestionFactory.CreateOrNull(Entry("回答", new[] { "解答", "解凍" }, meanings: new[] { "答えること" }));

            Assert.IsNotNull(question);
            Assert.AreEqual("答えること", question.Choices.First(c => c.Text == "回答").Meaning);
            Assert.AreEqual(string.Empty, question.Choices.First(c => c.Text == "解答").Meaning);
        }

        [Test]
        public void 指定した数まで作れた問題を返し作れない問題は飛ばす()
        {
            var entries = new List<KanjiUsageEntry>
            {
                Entry("一", new[] { "二", "三" }),
                Entry("四", new[] { "五" }), // まちがいが足りない
                Entry("六", new[] { "七", "八" }),
                Entry("九", new[] { "十", "百" }),
            };

            var questions = KanjiUsageQuestionFactory.Create(entries, 2);

            Assert.AreEqual(2, questions.Count);
            CollectionAssert.DoesNotContain(questions.Select(q => q.Answer).ToList(), "四");
        }

        [Test]
        public void 並べた順に作ってから出題順だけシャッフルする()
        {
            var ordered = new List<KanjiUsageEntry>
            {
                Entry("一", new[] { "二", "三" }),
                Entry("六", new[] { "七", "八" }),
                Entry("九", new[] { "十", "百" }),
            };

            var questions = KanjiUsageQuestionFactory.CreateInOrder(ordered, 2);

            // 先頭から2問を選ぶ（3問目は選ばない）
            CollectionAssert.AreEquivalent(new[] { "一", "六" }, questions.Select(q => q.Answer).ToList());
        }

        [Test]
        public void 問題の作り方は引数が正しくないと例外になる()
        {
            Assert.Throws<ArgumentNullException>(() => KanjiUsageQuestionFactory.CreateOrNull(null));
            Assert.Throws<ArgumentNullException>(() => KanjiUsageQuestionFactory.Create(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => KanjiUsageQuestionFactory.Create(new List<KanjiUsageEntry>(), -1));
        }

        [Test]
        public void 問題の正解の番号が範囲の外だと例外になる()
        {
            var choices = new[] { new KanjiUsageChoice("回答", ""), new KanjiUsageChoice("解答", "") };

            Assert.Throws<ArgumentOutOfRangeException>(() => new KanjiUsageQuestion("かいとう", Sentence, choices, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new KanjiUsageQuestion("かいとう", Sentence, choices, -1));
            Assert.Throws<ArgumentException>(() => new KanjiUsageQuestion("かいとう", Sentence, new[] { choices[0] }, 0));
        }
    }
}
