using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.KanjiUsage
{
    /// <summary>
    /// 使い分けの問題の振り返りの Presenter
    /// 進行と印は ChoiceQuizReviewPresenter に任せ、空いたところに正解の言葉を入れた文と、候補それぞれの意味を出させる
    /// 表示の設定でひらがなにすると、文と意味の漢字をひらがなで出し（選択肢と正解の言葉は漢字のまま）、切り替えたらすぐ出し直す
    /// </summary>
    public sealed class KanjiUsageReviewPresenter : ChoiceQuizReviewPresenter<KanjiUsageQuestion>
    {
        private readonly IReadOnlyList<KanjiUsageQuestion> questions;
        private readonly IKanjiUsageView view;
        private readonly DisplaySettings displaySettings;
        private readonly IDisposable settingSubscription;

        public KanjiUsageReviewPresenter(
            IReadOnlyList<KanjiUsageQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IQuizReviewView reviewView,
            IKanjiUsageView view,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer = null,
            DisplaySettings displaySettings = null)
            : base(questions, answers, startIndex, frameView, choiceListView, reviewView, sceneLoader, SceneNames.Result, audioPlayer)
        {
            this.questions = questions;
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.displaySettings = displaySettings;
            // 表示の設定が変わったら、今の問題を出し直す（購読した時点の値では呼ばない）
            settingSubscription = displaySettings?.UseHiragana
                .Skip(1)
                .Subscribe(this, static (_, self) => self.Show(self.questions[self.CurrentIndex]));
        }

        protected override UniTask ShowChoicesAsync(KanjiUsageQuestion question, CancellationToken ct)
        {
            Show(question);
            return UniTask.CompletedTask;
        }

        protected override void OnDispose()
        {
            settingSubscription?.Dispose();
        }

        private void Show(KanjiUsageQuestion question)
        {
            bool useHiragana = displaySettings != null && displaySettings.UseHiragana.CurrentValue;

            view.SetWeakMarkVisible(false);
            view.SetHint($"正解：{question.Answer}");
            view.ShowSentence(RubyText.Render(question.Sentence, useHiragana), question.Answer, true);
            for (int i = 0; i < question.ChoiceCount; i++)
            {
                var choice = question.Choices[i];
                view.ShowChoice(i, choice.Text, RubyText.Render(choice.Meaning, useHiragana));
            }
        }
    }
}
