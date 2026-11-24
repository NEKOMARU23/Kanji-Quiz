using System;
using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class RubyLabelPresenterTests
    {
        private sealed class FakeRubyLabelView : IRubyLabelView
        {
            public int CallCount;
            public bool? LastValue;

            public void SetHiragana(bool useHiragana)
            {
                CallCount++;
                LastValue = useHiragana;
            }
        }

        [Test]
        public void つないだときに、今の設定を画面の文字に伝える()
        {
            var view = new FakeRubyLabelView();
            var settings = new DisplaySettings(new FakeDisplaySettingRepository { UseHiragana = true });

            using var presenter = new RubyLabelPresenter(view, settings);

            Assert.IsTrue(view.LastValue);
        }

        [Test]
        public void 設定が変わったら、画面の文字に伝える()
        {
            var view = new FakeRubyLabelView();
            var settings = new DisplaySettings(new FakeDisplaySettingRepository());
            using var presenter = new RubyLabelPresenter(view, settings);

            settings.SetUseHiragana(true);

            Assert.IsTrue(view.LastValue);
            Assert.AreEqual(2, view.CallCount);
        }

        [Test]
        public void 片づけたあとは伝えない()
        {
            var view = new FakeRubyLabelView();
            var settings = new DisplaySettings(new FakeDisplaySettingRepository());
            var presenter = new RubyLabelPresenter(view, settings);

            presenter.Dispose();
            settings.SetUseHiragana(true);

            Assert.AreEqual(1, view.CallCount);
        }

        [Test]
        public void 空のものを渡すと例外()
        {
            var settings = new DisplaySettings(new FakeDisplaySettingRepository());

            Assert.Throws<ArgumentNullException>(() => new RubyLabelPresenter(null, settings));
            Assert.Throws<ArgumentNullException>(() => new RubyLabelPresenter(new FakeRubyLabelView(), null));
        }
    }
}
