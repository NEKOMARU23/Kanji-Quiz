using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TechC.KanjiQuiz.Common;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.StrokeOrder
{
    /// <summary>
    /// 漢字のリストから書き順クイズの問題を作る
    /// 書き順データがない漢字や、難易度の条件で問題を作れない漢字は除外する
    /// </summary>
    public sealed class StrokeOrderQuestionFactory
    {
        private readonly IKanjiStrokeSource strokeSource;

        public StrokeOrderQuestionFactory(IKanjiStrokeSource strokeSource)
        {
            this.strokeSource = strokeSource ?? throw new ArgumentNullException(nameof(strokeSource));
        }

        /// <summary>
        /// 漢字のリストから問題を作る
        /// </summary>
        /// <returns>作れた問題のリスト。リストの順番は漢字のリストの順番のまま</returns>
        public async UniTask<List<StrokeOrderQuestion>> CreateAsync(IEnumerable<string> kanjiList, Difficulty difficulty, int choiceCount, CancellationToken ct)
        {
            if (kanjiList == null) throw new ArgumentNullException(nameof(kanjiList));

            var questions = new List<StrokeOrderQuestion>();
            foreach (var kanji in kanjiList)
            {
                if (string.IsNullOrEmpty(kanji)) continue;

                var strokePaths = await strokeSource.LoadStrokePathsAsync(kanji, ct);
                if (strokePaths.Count == 0)
                {
                    Debug.LogWarning($"[StrokeOrderQuestionFactory] 漢字 '{kanji}' は書き順データがないため除外しました。");
                    continue;
                }

                if (!TryCreate(kanji, strokePaths, difficulty, choiceCount, out var question))
                {
                    Debug.Log($"[StrokeOrderQuestionFactory] 漢字 '{kanji}'（{strokePaths.Count}画）は難易度 {difficulty} の条件で問題を作れないため除外しました。");
                    continue;
                }

                questions.Add(question);
            }
            return questions;
        }

        /// <summary>
        /// 1つの漢字から問題を作る
        /// </summary>
        /// <returns>作れた場合は true。画数が少なくて重複しない不正解を作れない場合は false</returns>
        public static bool TryCreate(string kanji, IReadOnlyList<string> strokePaths, Difficulty difficulty, int choiceCount, out StrokeOrderQuestion question)
        {
            if (choiceCount < 2) throw new ArgumentOutOfRangeException(nameof(choiceCount), "選択肢は2つ以上必要です。");

            question = null;
            if (!WrongOrderGenerator.TryGenerate(strokePaths.Count, difficulty, choiceCount - 1, out var wrongOrders))
            {
                return false;
            }

            var strokes = new StrokeData[strokePaths.Count];
            for (int i = 0; i < strokePaths.Count; i++)
            {
                strokes[i] = StrokePathParser.Parse(strokePaths[i]);
            }

            int correctIndex = UnityEngine.Random.Range(0, choiceCount);
            var choiceOrders = new List<int[]>(wrongOrders);
            choiceOrders.Insert(correctIndex, WrongOrderGenerator.CreateIdentity(strokePaths.Count));

            question = new StrokeOrderQuestion(kanji, strokes, choiceOrders, correctIndex);
            return true;
        }
    }
}
