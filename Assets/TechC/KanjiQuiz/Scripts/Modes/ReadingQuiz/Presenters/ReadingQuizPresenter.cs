using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての Presenter
    /// 正しい読みが1つの問題は1つ選ぶ。複数の問題は、正しい読みをすべて選ぶ
    /// 選択肢を押すと、選ぶ・外すを切り替える。1つ以上選んでいれば [次へ] を押せる
    /// 正誤は結果画面でまとめて出す（ほかのモードと同じ）
    /// </summary>
    public sealed class ReadingQuizPresenter : QuizPresenterBase<ReadingQuizQuestion>
    {
        private readonly IReadingQuizView view;
        private int selectedMask;

        /// <param name="view">熟語と選択肢を描く View</param>
        public ReadingQuizPresenter(
            QuizSession<ReadingQuizQuestion> session,
            IQuizFrameView frameView,
            IReadingQuizView view,
            IAudioPlayer audioPlayer = null)
            : base(session, frameView, audioPlayer)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        protected override void OnInitialize()
        {
            view.OnOptionClicked
                .Subscribe(this, static (index, self) => self.ToggleOption(index))
                .AddTo(Disposables);
        }

        protected override UniTask ShowQuestionAsync(ReadingQuizQuestion question, AnswerRecord previousAnswer, CancellationToken ct)
        {
            selectedMask = previousAnswer.IsAnswered ? previousAnswer.SelectedIndex : 0;

            view.SetWord(question.Word);
            view.SetOptions(question.Choices);
            view.SetOptionsInteractable(true);
            view.SetHint(question.IsMultiple ? "{正|ただ}しい{読|よ}みを、すべて{選|えら}んでください" : "{正|ただ}しい{読|よ}みを1つ{選|えら}んでください");
            RefreshOptions(question);
            return UniTask.CompletedTask;
        }

        protected override bool TryJudge(ReadingQuizQuestion question, out int selected, out bool isCorrect)
        {
            selected = selectedMask;
            isCorrect = question.IsCorrect(selectedMask);
            return selectedMask > 0;
        }

        /// <summary>
        /// 1つだけの問題では、押した選択肢に選び替える。複数の問題では、押した選択肢を選ぶ・外すを切り替える
        /// </summary>
        private void ToggleOption(int index)
        {
            var question = Session.Current;
            if (index < 0 || index >= question.ChoiceCount) return;

            int bit = 1 << index;
            if (question.IsMultiple)
            {
                selectedMask ^= bit;
            }
            else
            {
                selectedMask = bit;
            }

            AudioPlayer.PlaySe(SeType.Select);
            RefreshOptions(question);
            RefreshNextButton();
        }

        private void RefreshOptions(ReadingQuizQuestion question)
        {
            for (int i = 0; i < question.ChoiceCount; i++)
            {
                view.SetOptionSelected(i, ReadingQuizAnswer.IsSelected(selectedMask, i));
            }
        }
    }
}
