using System;
using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての問題の振り返りの Presenter
    /// 結果画面から開き、熟語と選択肢を見せる。正しい読みは緑にし、プレイヤーが選んだものは印をつける
    /// 選択肢は押せない。案内には正しい読みを出す
    /// </summary>
    public sealed class ReadingQuizReviewPresenter : IDisposable
    {
        private readonly IReadOnlyList<ReadingQuizQuestion> questions;
        private readonly IReadOnlyList<AnswerRecord> answers;
        private readonly IQuizFrameView frameView;
        private readonly IReadingQuizView view;
        private readonly IQuizReviewView reviewView;
        private readonly ISceneLoader sceneLoader;
        private readonly string resultSceneName;
        private readonly IAudioPlayer audioPlayer;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private bool isDisposed;

        /// <summary>表示中の問題の番号（0 始まり）</summary>
        public int CurrentIndex { get; private set; }

        /// <param name="startIndex">最初に見せる問題の番号</param>
        public ReadingQuizReviewPresenter(
            IReadOnlyList<ReadingQuizQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IReadingQuizView view,
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
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.reviewView = reviewView ?? throw new ArgumentNullException(nameof(reviewView));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.resultSceneName = resultSceneName ?? throw new ArgumentNullException(nameof(resultSceneName));
            this.audioPlayer = audioPlayer ?? NullAudioPlayer.Instance;
            CurrentIndex = startIndex;
        }

        /// <summary>ボタンの購読を始め、最初に頼まれた問題を表示する</summary>
        public void Initialize()
        {
            reviewView.SetVisible(true);
            view.SetOptionsInteractable(false);

            frameView.OnNextClicked
                .Subscribe(this, static (_, self) => self.Move(1))
                .AddTo(disposables);
            frameView.OnPrevClicked
                .Subscribe(this, static (_, self) => self.Move(-1))
                .AddTo(disposables);
            reviewView.OnBackClicked
                .Subscribe(this, static (_, self) => self.sceneLoader.Load(self.resultSceneName))
                .AddTo(disposables);

            ShowCurrent(0);
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            disposables.Dispose();
        }

        private void Move(int delta)
        {
            int next = CurrentIndex + delta;
            if (next < 0 || next >= questions.Count) return;

            CurrentIndex = next;
            audioPlayer.PlaySe(SeType.PageTurn);
            ShowCurrent(delta);
        }

        /// <param name="direction">切り替えの向き（IQuizFrameView.PlayQuestionTransition を参照）</param>
        private void ShowCurrent(int direction)
        {
            var question = questions[CurrentIndex];
            var answer = answers[CurrentIndex];
            int selectedMask = answer.IsAnswered ? answer.SelectedIndex : 0;

            frameView.SetProgress(CurrentIndex + 1, questions.Count);
            frameView.SetPrevInteractable(CurrentIndex > 0);
            frameView.SetNextInteractable(CurrentIndex < questions.Count - 1);
            reviewView.SetAnswerState(answer.State);
            frameView.PlayQuestionTransition(direction);

            view.SetWord(question.Word);
            view.SetOptions(question.Choices);
            view.SetOptionsInteractable(false);
            for (int i = 0; i < question.ChoiceCount; i++)
            {
                view.SetOptionSelected(i, ReadingQuizAnswer.IsSelected(selectedMask, i));
                view.SetOptionCorrect(i, ReadingQuizAnswer.IsSelected(question.CorrectMask, i));
            }
            view.SetHint("{正|ただ}しい{読|よ}み：" + string.Join("、", question.CorrectReadings));
        }
    }
}
