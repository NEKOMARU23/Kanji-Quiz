using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 「N 個の選択肢から1つ選ぶ」モード共通の Presenter
    /// 選んだ番号と正解の番号が同じかどうかで判定する
    /// 各モードは選択肢の中身の描き方（ShowChoicesAsync）だけを実装する
    /// </summary>
    /// <typeparam name="TQuestion">選択式の問題の型</typeparam>
    public abstract class ChoiceQuizPresenter<TQuestion> : QuizPresenterBase<TQuestion>
        where TQuestion : IChoiceQuestion
    {
        private int selectedIndex = -1;

        /// <summary>選択肢を並べる View</summary>
        protected IChoiceListView ChoiceListView { get; }

        protected ChoiceQuizPresenter(QuizSession<TQuestion> session, IQuizFrameView frameView, IChoiceListView choiceListView)
            : base(session, frameView)
        {
            ChoiceListView = choiceListView ?? throw new ArgumentNullException(nameof(choiceListView));
        }

        protected override void OnInitialize()
        {
            ChoiceListView.OnChoiceSelected
                .Subscribe(this, static (index, self) => self.HandleChoiceSelected(index))
                .AddTo(Disposables);
        }

        protected sealed override async UniTask ShowQuestionAsync(TQuestion question, AnswerRecord previousAnswer, CancellationToken ct)
        {
            selectedIndex = previousAnswer.IsAnswered ? previousAnswer.SelectedIndex : -1;

            ChoiceListView.SetChoiceCount(question.ChoiceCount);
            ChoiceListView.SetSelected(selectedIndex);

            await ShowChoicesAsync(question, ct);
        }

        protected sealed override bool TryJudge(TQuestion question, out int selected, out bool isCorrect)
        {
            selected = selectedIndex;
            isCorrect = selectedIndex == question.CorrectIndex;
            return selectedIndex >= 0;
        }

        /// <summary>
        /// 各選択肢の中身を描く
        /// </summary>
        protected abstract UniTask ShowChoicesAsync(TQuestion question, CancellationToken ct);

        private void HandleChoiceSelected(int index)
        {
            selectedIndex = index;
            RefreshNextButton();
        }
    }
}
