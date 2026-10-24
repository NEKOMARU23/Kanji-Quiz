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
                var question = await TryCreateFromSourceAsync(kanji, difficulty, choiceCount, true, ct);
                if (question != null) questions.Add(question);
            }
            return questions;
        }

        /// <summary>
        /// 出題範囲の漢字からランダムに選んで、指定した数の問題を作る
        /// 選んだ漢字で問題を作れないときは、別の漢字を選び直す
        /// </summary>
        /// <param name="candidates">出題範囲の漢字</param>
        /// <param name="questionCount">作る問題の数</param>
        /// <returns>作れた問題のリスト。出題範囲の漢字が足りないときは指定した数より少なくなる</returns>
        public async UniTask<List<StrokeOrderQuestion>> CreateRandomAsync(
            IReadOnlyList<string> candidates,
            Difficulty difficulty,
            int choiceCount,
            int questionCount,
            CancellationToken ct)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (questionCount < 1) throw new ArgumentOutOfRangeException(nameof(questionCount));

            var shuffled = ShuffleUtility.ShuffledCopy(candidates);
            var questions = new List<StrokeOrderQuestion>(questionCount);
            foreach (var kanji in shuffled)
            {
                if (questions.Count >= questionCount) break;

                // 画数が少なくて作れない漢字は出題範囲によくあるので、ログは出さない
                var question = await TryCreateFromSourceAsync(kanji, difficulty, choiceCount, false, ct);
                if (question != null) questions.Add(question);
            }

            if (questions.Count < questionCount)
            {
                Debug.LogWarning($"[StrokeOrderQuestionFactory] 出題範囲から {questionCount} 問を作れませんでした（{questions.Count} 問）。");
            }
            return questions;
        }

        private async UniTask<StrokeOrderQuestion> TryCreateFromSourceAsync(
            string kanji,
            Difficulty difficulty,
            int choiceCount,
            bool logSkipped,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(kanji)) return null;

            var strokePaths = await strokeSource.LoadStrokePathsAsync(kanji, ct);
            if (strokePaths.Count == 0)
            {
                Debug.LogWarning($"[StrokeOrderQuestionFactory] 漢字 '{kanji}' は書き順データがないため除外しました。");
                return null;
            }

            if (!TryCreate(kanji, strokePaths, difficulty, choiceCount, out var question))
            {
                if (logSkipped)
                {
                    Debug.Log($"[StrokeOrderQuestionFactory] 漢字 '{kanji}'（{strokePaths.Count}画）は難易度 {difficulty} の条件で問題を作れないため除外しました。");
                }
                return null;
            }

            return question;
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
