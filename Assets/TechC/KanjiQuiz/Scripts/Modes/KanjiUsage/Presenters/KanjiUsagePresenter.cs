using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分けの Presenter
    /// 進行と判定は ChoiceQuizPresenter に任せ、文と選択肢の言葉を描かせ、苦手な言葉に印をつける
    /// 文の漢字は、表示の設定でひらがなにできる。設定を切り替えたら、今の問題の文をすぐ出し直す
    /// </summary>
    public sealed class KanjiUsagePresenter : ChoiceQuizPresenter<KanjiUsageQuestion>
    {
        private readonly IKanjiUsageView view;
        private readonly WeakQuestionList weakList;
        private readonly DisplaySettings displaySettings;

        public KanjiUsagePresenter(
            QuizSession<KanjiUsageQuestion> session,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IKanjiUsageView view,
            IAudioPlayer audioPlayer = null,
            WeakQuestionList weakList = null,
            DisplaySettings displaySettings = null)
            : base(session, frameView, choiceListView, audioPlayer)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.weakList = weakList;
            this.displaySettings = displaySettings;
        }

        protected override void OnInitialize()
        {
            // 選択肢を押す操作の購読は、基底クラスが行う
            base.OnInitialize();

            // 表示の設定が変わったら、今の問題の文を出し直す（購読した時点の値では呼ばない）
            displaySettings?.UseHiragana
                .Skip(1)
                .Subscribe(this, static (_, self) => self.ShowSentence(self.Session.Current))
                .AddTo(Disposables);
        }

        protected override UniTask ShowChoicesAsync(KanjiUsageQuestion question, CancellationToken ct)
        {
            view.SetWeakMarkVisible(weakList != null && weakList.Contains(question.Answer));
            view.SetHint("文に合う言葉を1つ選んでください");
            ShowSentence(question);
            for (int i = 0; i < question.ChoiceCount; i++)
            {
                view.ShowChoice(i, question.Choices[i].Text, null);
            }
            return UniTask.CompletedTask;
        }

        private void ShowSentence(KanjiUsageQuestion question)
        {
            bool useHiragana = displaySettings != null && displaySettings.UseHiragana.CurrentValue;
            view.ShowSentence(RubyText.Render(question.Sentence, useHiragana), $"（{question.Reading}）", false);
        }
    }
}
