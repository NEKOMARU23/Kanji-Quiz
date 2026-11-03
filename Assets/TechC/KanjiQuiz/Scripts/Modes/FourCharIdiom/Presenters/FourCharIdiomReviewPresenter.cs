using System;
using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 四字熟語づくりの問題の振り返りの Presenter
    /// 結果画面から開き、自分が入れた字を正しいマスと間違ったマスに分けて見せ、正解の熟語・読み・意味・例文を出す。字は押せない
    /// 表示の設定でひらがなにすると、意味と例文の漢字を（熟語のほかは）ひらがなで出し、切り替えたらすぐ出し直す
    /// </summary>
    public sealed class FourCharIdiomReviewPresenter : IDisposable
    {
        private readonly IReadOnlyList<FourCharIdiomQuestion> questions;
        private readonly IReadOnlyList<AnswerRecord> answers;
        private readonly IQuizFrameView frameView;
        private readonly IQuizReviewView reviewView;
        private readonly IFourCharIdiomView view;
        private readonly ISceneLoader sceneLoader;
        private readonly IAudioPlayer audioPlayer;
        private readonly DisplaySettings displaySettings;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private bool isDisposed;

        /// <summary>表示中の問題の番号（0 始まり）</summary>
        public int CurrentIndex { get; private set; }

        /// <param name="audioPlayer">効果音を鳴らす。null なら鳴らさない</param>
        /// <param name="displaySettings">意味と例文をひらがなで出すかどうか。null なら漢字のまま</param>
        public FourCharIdiomReviewPresenter(
            IReadOnlyList<FourCharIdiomQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IQuizReviewView reviewView,
            IFourCharIdiomView view,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer = null,
            DisplaySettings displaySettings = null)
        {
            this.questions = questions ?? throw new ArgumentNullException(nameof(questions));
            this.answers = answers ?? throw new ArgumentNullException(nameof(answers));
            if (questions.Count == 0) throw new ArgumentException("問題が1問もありません。", nameof(questions));
            if (answers.Count != questions.Count) throw new ArgumentException("問題と回答の数が一致しません。", nameof(answers));
            if (startIndex < 0 || startIndex >= questions.Count) throw new ArgumentOutOfRangeException(nameof(startIndex));
            this.frameView = frameView ?? throw new ArgumentNullException(nameof(frameView));
            this.reviewView = reviewView ?? throw new ArgumentNullException(nameof(reviewView));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            this.audioPlayer = audioPlayer ?? NullAudioPlayer.Instance;
            this.displaySettings = displaySettings;
            CurrentIndex = startIndex;
        }

        /// <summary>
        /// ボタンの購読を始め、最初に頼まれた問題を表示する
        /// </summary>
        public void Initialize()
        {
            reviewView.SetVisible(true);
            view.SetInteractable(false);
            view.SetWeakMarkVisible(false);

            frameView.OnNextClicked.Subscribe(this, static (_, self) => self.Move(1)).AddTo(disposables);
            frameView.OnPrevClicked.Subscribe(this, static (_, self) => self.Move(-1)).AddTo(disposables);
            reviewView.OnBackClicked.Subscribe(this, static (_, self) => self.sceneLoader.Load(SceneNames.Result)).AddTo(disposables);
            displaySettings?.UseHiragana
                .Skip(1)
                .Subscribe(this, static (_, self) => self.ShowTexts(self.questions[self.CurrentIndex]))
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

            frameView.SetProgress(CurrentIndex + 1, questions.Count);
            frameView.SetPrevInteractable(CurrentIndex > 0);
            frameView.SetNextInteractable(CurrentIndex < questions.Count - 1);
            reviewView.SetAnswerState(answer.State);
            frameView.PlayQuestionTransition(direction);

            var filled = FourCharIdiomAnswer.Decode(answer.IsAnswered ? answer.SelectedIndex : -1, question.BlankPositions.Count);
            view.SetCells(FourCharIdiomBoard.Cells(question, filled, review: true));
            view.SetTiles(question.Tiles, FourCharIdiomBoard.UsedTiles(question, filled));
            ShowTexts(question);
        }

        private void ShowTexts(FourCharIdiomQuestion question)
        {
            bool useHiragana = displaySettings != null && displaySettings.UseHiragana.CurrentValue;
            view.SetMeaning(RubyText.Render(question.Meaning, useHiragana));
            view.ShowAnswerInfo(question.Word, question.Reading, RubyText.Render(question.Example, useHiragana));
        }
    }
}
