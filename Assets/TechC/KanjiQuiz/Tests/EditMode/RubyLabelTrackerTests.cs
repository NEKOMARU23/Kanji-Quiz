using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class RubyLabelTrackerTests
    {
        private sealed class Label { }

        private const string Marked = "{次|つぎ}へ →";

        [Test]
        public void 印のついた文字は漢字のままなら読みを外して書き直す()
        {
            var tracker = new RubyLabelTracker<Label>();

            Assert.AreEqual("次へ →", tracker.Sync(new Label(), Marked));
        }

        [Test]
        public void ひらがなにすると読みで書き直す()
        {
            var tracker = new RubyLabelTracker<Label>();
            tracker.SetUseHiragana(true);

            Assert.AreEqual("つぎへ →", tracker.Sync(new Label(), Marked));
        }

        [Test]
        public void 印のない文字は直さない()
        {
            var tracker = new RubyLabelTracker<Label>();

            Assert.IsNull(tracker.Sync(new Label(), "次へ →"));
            Assert.AreEqual(0, tracker.Count);
        }

        [Test]
        public void 書き直したあとは_変わらなければ何もしない()
        {
            var tracker = new RubyLabelTracker<Label>();
            var label = new Label();
            string rendered = tracker.Sync(label, Marked);

            Assert.IsNull(tracker.Sync(label, rendered));
        }

        [Test]
        public void 設定が変わったら_元の印のついた文字から書き直す()
        {
            var tracker = new RubyLabelTracker<Label>();
            var label = new Label();
            string kanji = tracker.Sync(label, Marked);

            tracker.SetUseHiragana(true);
            string hiragana = tracker.Sync(label, kanji);
            tracker.SetUseHiragana(false);
            string back = tracker.Sync(label, hiragana);

            Assert.AreEqual("つぎへ →", hiragana);
            Assert.AreEqual("次へ →", back);
        }

        [Test]
        public void ほかのコードが入れ替えた文字は_設定が変わっても上書きしない()
        {
            var tracker = new RubyLabelTracker<Label>();
            var label = new Label();
            tracker.Sync(label, Marked);

            tracker.SetUseHiragana(true);

            Assert.IsNull(tracker.Sync(label, "3 / 5 問"));
            Assert.AreEqual(0, tracker.Count);
        }

        [Test]
        public void コードが印のついた別の文字を入れたら_それを新しく覚える()
        {
            var tracker = new RubyLabelTracker<Label>();
            var label = new Label();
            tracker.Sync(label, Marked);

            Assert.AreEqual("2 / 5 問", tracker.Sync(label, "2 / 5 {問|もん}"));
            tracker.SetUseHiragana(true);
            Assert.AreEqual("2 / 5 もん", tracker.Sync(label, "2 / 5 問"));
        }

        [Test]
        public void 忘れたラベルは書き直さない()
        {
            var tracker = new RubyLabelTracker<Label>();
            var label = new Label();
            string rendered = tracker.Sync(label, Marked);

            tracker.Forget(label);
            tracker.SetUseHiragana(true);

            Assert.IsNull(tracker.Sync(label, rendered));
        }

        [Test]
        public void 覚えているラベルの一覧を返す()
        {
            var tracker = new RubyLabelTracker<Label>();
            var a = new Label();
            var b = new Label();
            tracker.Sync(a, Marked);
            tracker.Sync(b, "印なし");

            CollectionAssert.AreEqual(new[] { a }, tracker.Keys());
        }

        [Test]
        public void 印の有無を調べる()
        {
            Assert.IsTrue(RubyText.HasMark("{次|つぎ}へ"));
            Assert.IsTrue(RubyText.HasMark("{ 1 {次|つぎ}"));
            Assert.IsFalse(RubyText.HasMark("次へ"));
            Assert.IsFalse(RubyText.HasMark("{次|つぎ"));
            Assert.IsFalse(RubyText.HasMark(null));
        }
    }
}
