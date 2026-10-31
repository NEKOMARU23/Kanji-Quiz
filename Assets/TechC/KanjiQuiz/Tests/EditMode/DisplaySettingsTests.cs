using NUnit.Framework;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Tests
{
    public class DisplaySettingsTests
    {
        [Test]
        public void 作るときに保存した値を読む()
        {
            var settings = new DisplaySettings(new FakeDisplaySettingRepository { UseHiragana = true });

            Assert.IsTrue(settings.UseHiragana.CurrentValue);
        }

        [Test]
        public void 変えると保存して通知する()
        {
            var repository = new FakeDisplaySettingRepository();
            var settings = new DisplaySettings(repository);
            bool? notified = null;
            R3.ObservableSubscribeExtensions.Subscribe(settings.UseHiragana, value => notified = value);

            settings.SetUseHiragana(true);

            Assert.IsTrue(repository.UseHiragana);
            Assert.IsTrue(notified);
        }

        [Test]
        public void 同じ値なら保存しない()
        {
            var repository = new FakeDisplaySettingRepository();
            var settings = new DisplaySettings(repository);

            settings.SetUseHiragana(false);

            Assert.AreEqual(0, repository.SaveCount);
        }

        [Test]
        public void 意味と例文の辞書をJSONから読む()
        {
            const string json = "{\"id\":\"Elementary\",\"entries\":[{\"kanji\":\"海\",\"meaning\":\"しおからい{水|みず}。\",\"example\":\"海へ{行|い}く。\"}]}";

            var dictionary = KanjiMeaningDictionary.FromJson(json);

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("海へ{行|い}く。", dictionary.Get("海").Example);
            Assert.IsTrue(dictionary.Get("山").IsEmpty);
        }
    }
}
