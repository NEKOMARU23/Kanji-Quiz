using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 全モード共通のクイズ進行（出題 → 回答 → 判定 → 次の問題 → 終了）
    /// 各モードは「問題の見せ方」と「回答の判定」だけを実装する
    /// </summary>
    /// <typeparam name="TQuestion">問題の型</typeparam>
    public abstract class QuizPresenterBase<TQuestion> : IDisposable
    {
        private readonly IQuizFrameView frameView;
        private readonly Subject<Unit> onFinished = new Subject<Unit>();
        private CancellationTokenSource questionCts;
        private bool isInitialized;
        private bool isDisposed;

        /// <summary>進行状況</summary>
        protected QuizSession<TQuestion> Session { get; }

        /// <summary>Dispose でまとめて解除する購読</summary>
        protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

        /// <summary>最後の問題に回答して [次へ] が押されたとき</summary>
        public Observable<Unit> OnFinished => onFinished;

        protected QuizPresenterBase(QuizSession<TQuestion> session, IQuizFrameView frameView)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            this.frameView = frameView ?? throw new ArgumentNullException(nameof(frameView));
        }

        /// <summary>
        /// 入力の購読を始め、最初の問題を表示する
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;

            frameView.OnNextClicked
                .Subscribe(this, static (_, self) => self.HandleNext())
                .AddTo(Disposables);
            frameView.OnPrevClicked
                .Subscribe(this, static (_, self) => self.HandlePrev())
                .AddTo(Disposables);

            OnInitialize();
            ShowCurrentQuestionAsync().Forget();
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            CancelQuestion();
            Disposables.Dispose();
            onFinished.Dispose();
        }

        /// <summary>
        /// 入力の購読を追加したいときに上書きする。最初の問題を表示する前に呼ばれる
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// 問題を表示する
        /// </summary>
        /// <param name="question">表示する問題</param>
        /// <param name="previousAnswer">この問題に以前回答していれば、その記録（前の問題に戻ったとき）</param>
        /// <param name="ct">別の問題へ移ったときにキャンセルされる</param>
        protected abstract UniTask ShowQuestionAsync(TQuestion question, AnswerRecord previousAnswer, CancellationToken ct);

        /// <summary>
        /// 現在の回答を判定する
        /// </summary>
        /// <returns>回答が選ばれていれば true。未選択なら false</returns>
        protected abstract bool TryJudge(TQuestion question, out int selectedIndex, out bool isCorrect);

        /// <summary>
        /// 回答の選択状態が変わったときに呼ぶ。[次へ] を押せるかどうかを更新する
        /// </summary>
        protected void RefreshNextButton()
        {
            frameView.SetNextInteractable(TryJudge(Session.Current, out _, out _));
        }

        private void HandleNext()
        {
            if (!TryJudge(Session.Current, out var selectedIndex, out var isCorrect)) return;

            Session.RecordAnswer(selectedIndex, isCorrect);

            if (Session.MoveNext())
            {
                ShowCurrentQuestionAsync().Forget();
                return;
            }

            CancelQuestion();
            frameView.SetNextInteractable(false);
            frameView.SetPrevInteractable(false);
            onFinished.OnNext(Unit.Default);
        }

        private void HandlePrev()
        {
            if (Session.MovePrev())
            {
                ShowCurrentQuestionAsync().Forget();
            }
        }

        private async UniTaskVoid ShowCurrentQuestionAsync()
        {
            CancelQuestion();
            questionCts = new CancellationTokenSource();
            var ct = questionCts.Token;

            frameView.SetProgress(Session.CurrentIndex.CurrentValue + 1, Session.Count);
            frameView.SetPrevInteractable(!Session.IsFirst);
            frameView.SetNextInteractable(false);

            try
            {
                await ShowQuestionAsync(Session.Current, Session.CurrentAnswer, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested) return;
            RefreshNextButton();
        }

        private void CancelQuestion()
        {
            if (questionCts == null) return;

            questionCts.Cancel();
            questionCts.Dispose();
            questionCts = null;
        }
    }
}
