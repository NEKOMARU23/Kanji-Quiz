using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;
using TechC.KanjiQuiz.Screens.Result;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>
    /// テスト用の結果画面。表示された内容を記録し、ボタン操作をメソッドで再現する
    /// 演出は待たずにすぐ終わる。HoldReveal を true にすると、演出の途中で止まる
    /// </summary>
    public class FakeResultView : IResultView
    {
        private readonly Subject<Unit> onRetryClicked = new Subject<Unit>();
        private readonly Subject<int> onItemClicked = new Subject<int>();
        private readonly Subject<Unit> onReviewClicked = new Subject<Unit>();
        private readonly Subject<Unit> onSkipRequested = new Subject<Unit>();

        public Observable<Unit> OnRetryClicked => onRetryClicked;
        public Observable<int> OnItemClicked => onItemClicked;
        public Observable<Unit> OnReviewClicked => onReviewClicked;
        public Observable<Unit> OnSkipRequested => onSkipRequested;
        public bool IsReviewAvailable { get; private set; }

        /// <summary>true にすると、最初の ○× を出したところで演出が止まる（飛ばす操作のテスト用）</summary>
        public bool HoldReveal { get; set; }

        public string ModeName { get; private set; }
        public int CorrectCount { get; private set; } = -1;
        public int Total { get; private set; } = -1;
        public bool IsScoreAnimated { get; private set; }
        public IReadOnlyList<QuizResultItem> Items { get; private set; }
        public bool IsNoResultShown { get; private set; }
        public bool IsMenuVisible { get; private set; } = true;
        public bool IsAllRevealed { get; private set; }
        public int RouletteCount { get; private set; }
        public int BeatCount { get; private set; }

        /// <summary>表示した級。まだなら null</summary>
        public string Rank { get; private set; }
        public bool IsRankAnimated { get; private set; }
        public bool IsHanamaruShown { get; private set; }
        public bool IsHanamaruAnimated { get; private set; }

        /// <summary>表示した自己ベスト。まだなら -1</summary>
        public int BestCorrectCount { get; private set; } = -1;
        public bool IsNewBestShown { get; private set; }

        /// <summary>○× を出した問題の番号（出した順）</summary>
        public List<int> RevealedIndices { get; } = new List<int>();

        public void SetModeName(string modeName) => ModeName = modeName;

        public void SetItems(IReadOnlyList<QuizResultItem> items) => Items = items;

        public UniTask RevealItemAsync(int index, CancellationToken ct)
        {
            RevealedIndices.Add(index);
            return HoldReveal ? UniTask.Never(ct) : UniTask.CompletedTask;
        }

        public void RevealAllItems() => IsAllRevealed = true;

        public UniTask PlayScoreRouletteAsync(int total, CancellationToken ct)
        {
            RouletteCount++;
            return UniTask.CompletedTask;
        }

        public void ShowScore(int correctCount, int total, bool animate)
        {
            CorrectCount = correctCount;
            Total = total;
            IsScoreAnimated = animate;
        }

        public UniTask WaitBeatAsync(CancellationToken ct)
        {
            BeatCount++;
            return UniTask.CompletedTask;
        }

        public UniTask PlayRankStampAsync(string rank, CancellationToken ct)
        {
            Rank = rank;
            IsRankAnimated = true;
            return UniTask.CompletedTask;
        }

        public void ShowRank(string rank) => Rank = rank;

        public UniTask PlayHanamaruAsync(CancellationToken ct)
        {
            IsHanamaruShown = true;
            IsHanamaruAnimated = true;
            return UniTask.CompletedTask;
        }

        public void ShowHanamaru() => IsHanamaruShown = true;

        public void ShowBestRecord(int bestCorrectCount, int total, bool isNewBest, bool animate)
        {
            BestCorrectCount = bestCorrectCount;
            IsNewBestShown = isNewBest;
        }

        public void SetMenuVisible(bool visible) => IsMenuVisible = visible;

        public void ShowNoResult() => IsNoResultShown = true;

        public void SetReviewAvailable(bool available) => IsReviewAvailable = available;

        public void ClickRetry() => onRetryClicked.OnNext(Unit.Default);
        public void ClickItem(int index) => onItemClicked.OnNext(index);
        public void ClickReview() => onReviewClicked.OnNext(Unit.Default);
        public void ClickSkip() => onSkipRequested.OnNext(Unit.Default);
    }
}
