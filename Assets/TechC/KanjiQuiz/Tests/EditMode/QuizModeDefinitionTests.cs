using NUnit.Framework;
using TechC.KanjiQuiz.Common;
using UnityEditor;
using UnityEngine;

namespace TechC.KanjiQuiz.Tests
{
    public class QuizModeDefinitionTests
    {
        private const string StrokeOrderModePath = "Assets/TechC/KanjiQuiz/Data/Modes/Mode_StrokeOrder.asset";
        private const string CatalogPath = "Assets/TechC/KanjiQuiz/Data/Modes/ModeCatalog.asset";

        [Test]
        public void 難易度ごとに名前と保存のキーを作る()
        {
            var mode = QuizModeDefinition.Create("StrokeOrder", "書き順クイズ", "StrokeOrder");

            Assert.AreEqual("書き順クイズ（常識）", mode.Title(Difficulty.Normal));
            Assert.AreEqual("StrokeOrder_Hard", mode.RecordKey(Difficulty.Hard));
            Object.DestroyImmediate(mode);
        }

        [Test]
        public void 難易度のないモードは名前とキーに難易度をつけない()
        {
            var mode = QuizModeDefinition.Create("FourCharIdiom", "四字熟語づくり", "FourCharIdiom", hasDifficulty: false, easyQuestionCount: 6);

            Assert.AreEqual("四字熟語づくり", mode.Title(Difficulty.Hard));
            Assert.AreEqual("FourCharIdiom", mode.RecordKey(Difficulty.Hard));
            Assert.AreEqual(6, mode.QuestionCount(Difficulty.Hard));
            Object.DestroyImmediate(mode);
        }

        [TestCase(Difficulty.Easy, 5, 2)]
        [TestCase(Difficulty.Normal, 7, 3)]
        [TestCase(Difficulty.Hard, 7, 3)]
        public void 書き順クイズの問題数と優先して出す苦手な漢字の数(Difficulty difficulty, int questions, int weak)
        {
            var mode = AssetDatabase.LoadAssetAtPath<QuizModeDefinition>(StrokeOrderModePath);

            Assert.IsNotNull(mode, "書き順クイズのモードの定義がありません");
            Assert.AreEqual(questions, mode.QuestionCount(difficulty));
            Assert.AreEqual(weak, mode.WeakQuestionCount(difficulty));
            Assert.AreEqual(SceneNames.StrokeOrder, mode.SceneName);
        }

        [Test]
        public void モードの一覧から書き順クイズを探せる()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<QuizModeCatalog>(CatalogPath);

            Assert.IsNotNull(catalog, "モードの一覧がありません");
            Assert.IsNotNull(catalog.Find("StrokeOrder"));
            Assert.IsNull(catalog.Find("NoSuchMode"));
        }
    }
}
