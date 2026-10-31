using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 選択式モード共通の、問題の振り返りの Presenter
    /// 結果画面から開き、正解とプレイヤーの回答に印をつけて問題を見せる。選択肢は押せない
    /// 各モードは選択肢の中身の描き方（ShowChoicesAsync）だけを実装する
    /// </summary>
    /// <typeparam name="TQuestion">選択式の問題の型</typeparam>
    public abstract class ChoiceQuizReviewPresenter<TQuestion> : IDisposable
        where TQuestion : IChoiceQuestion
    {
        private readonly IReadOnlyList<TQuestion> questions;
        private readonly IReadOnlyList<AnswerRecord> answers;
        private readonly IQuizFrameView frameView;
        private readonly IChoiceListView choiceListView;
        private readonly IQuizReviewView reviewView;
        private readonly ISceneLoader sceneLoader;
        private readonly string resultSceneName;
        private readonly IAudioPlayer audioPlayer;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private CancellationTokenSource questionCts;
        private bool isDisposed;

        /// <summary>表示中の問題の番号（0 始まり）</summary>
        public int CurrentIndex { get; private set; }

        protected ChoiceQuizReviewPresenter(
            IReadOnlyList<TQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IQuizReviewView reviewView,
            ISceneLoader sceneLoader,
            string resultSceneName,
            IAudioPlayer audioPlayer = null)
        {
            this.questions = questions ?? throw new ArgumentNullException(nameof(questions));
            this.answers = answers ?? throw new ArgumentNullException(nameof(answers));
            if (questions.Count == 0) throw new ArgumentException("問題が1問もありません。", nameof(questions));
            if (answers.Count != questions.Count) throw new ArgumentException("問題と回答の数が一致しません。", nameof(answers));
            if (startIndex < 0 || startIndex >= questions.Count) throw new ArgumentOutOfRangeException(nameof(startIndex));

            this.frameView = frameView ?? throw new ArgumentNullException(nameof(frameView));
            this.choiceListView = choiceListView ?? throw new ArgumentNullException(nameof(choiceListView));
            this.reviewView = reviewView ?? throw new ArgumentNullException(nameof(reviewView));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.resultSceneName = resultSceneName ?? throw new ArgumentNullException(nameof(resultSceneName));
            this.audioPlayer = audioPlayer ?? NullAudioPlayer.Instance;
            CurrentIndex = startIndex;
        }

        /// <summary>
        /// ボタンの購読を始め、最初に頼まれた問題を表示する
        /// </summary>
        public void Initialize()
        {
            reviewView.SetVisible(true);
            choiceListView.SetInteractable(false);

            frameView.OnNextClicked
                .Subscribe(this, static (_, self) => self.Move(1))
                .AddTo(disposables);
            frameView.OnPrevClicked
                .Subscribe(this, static (_, self) => self.Move(-1))
                .AddTo(disposables);
            reviewView.OnBackClicked
                .Subscribe(this, static (_, self) => self.sceneLoader.Load(self.resultSceneName))
                .AddTo(disposables);

            ShowCurrentAsync(0).Forget();
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            CancelQuestion();
            disposables.Dispose();
            OnDispose();
        }

        /// <summary>Dispose のときに、モードごとの後片付けをする（購読の解除など）</summary>
        protected virtual void OnDispose()
        {
        }

        /// <summary>
        /// 各選択肢の中身を描く
        /// </summary>
        protected abstract UniTask ShowChoicesAsync(TQuestion question, CancellationToken ct);

        private void Move(int delta)
        {
            int next = CurrentIndex + delta;
            if (next < 0 || next >= questions.Count) return;

            CurrentIndex = next;
            audioPlayer.PlaySe(SeType.PageTurn);
            ShowCurrentAsync(delta).Forget();
        }

        /// <param name="direction">切り替えの向き（IQuizFrameView.PlayQuestionTransition を参照）</param>
        private async UniTaskVoid ShowCurrentAsync(int direction)
        {
            CancelQuestion();
            questionCts = new CancellationTokenSource();
            var ct = questionCts.Token;

            var question = questions[CurrentIndex];
            var answer = answers[CurrentIndex];

            frameView.SetProgress(CurrentIndex + 1, questions.Count);
            frameView.SetPrevInteractable(CurrentIndex > 0);
            frameView.SetNextInteractable(CurrentIndex < questions.Count - 1);
            reviewView.SetAnswerState(answer.State);
            frameView.PlayQuestionTransition(direction);

            choiceListView.SetChoiceCount(question.ChoiceCount);
            choiceListView.SetSelected(answer.IsAnswered ? answer.SelectedIndex : -1);
            choiceListView.SetMarks(question.CorrectIndex, answer.IsAnswered ? answer.SelectedIndex : -1);

            try
            {
                await ShowChoicesAsync(question, ct);
            }
            catch (OperationCanceledException)
            {
                // 別の問題へ移った
            }
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
