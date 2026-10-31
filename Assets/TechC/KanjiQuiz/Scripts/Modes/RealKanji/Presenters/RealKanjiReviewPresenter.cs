using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.RealKanji
{
    /// <summary>
    /// 本物の漢字さがしの問題の振り返りの Presenter
    /// 進行と印は ChoiceQuizReviewPresenter に任せ、選択肢の字と説明、本物の読み・意味・例文・使用例を出させる
    /// 表示の設定でひらがなにすると、意味と例文の漢字を（本物の字のほかは）ひらがなで出し、切り替えたらすぐ出し直す
    /// </summary>
    public sealed class RealKanjiReviewPresenter : ChoiceQuizReviewPresenter<RealKanjiQuestion>
    {
        private readonly IReadOnlyList<RealKanjiQuestion> questions;
        private readonly IRealKanjiView realKanjiView;
        private readonly DisplaySettings displaySettings;
        private readonly IDisposable settingSubscription;

        public RealKanjiReviewPresenter(
            IReadOnlyList<RealKanjiQuestion> questions,
            IReadOnlyList<AnswerRecord> answers,
            int startIndex,
            IQuizFrameView frameView,
            IChoiceListView choiceListView,
            IQuizReviewView reviewView,
            IRealKanjiView realKanjiView,
            ISceneLoader sceneLoader,
            IAudioPlayer audioPlayer = null,
            DisplaySettings displaySettings = null)
            : base(questions, answers, startIndex, frameView, choiceListView, reviewView, sceneLoader, SceneNames.Result, audioPlayer)
        {
            this.questions = questions;
            this.realKanjiView = realKanjiView ?? throw new ArgumentNullException(nameof(realKanjiView));
            this.displaySettings = displaySettings;
            // 表示の設定が変わったら、今の問題の意味と例文を出し直す（購読した時点の値では呼ばない）
            settingSubscription = displaySettings?.UseHiragana
                .Skip(1)
                .Subscribe(this, static (_, self) => self.ShowAnswerInfo(self.questions[self.CurrentIndex]));
        }

        protected override UniTask ShowChoicesAsync(RealKanjiQuestion question, CancellationToken ct)
        {
            realKanjiView.SetWeakMarkVisible(false);
            ShowAnswerInfo(question);
            RealKanjiChoiceDrawer.Draw(realKanjiView, question, true);
            return UniTask.CompletedTask;
        }

        private void ShowAnswerInfo(RealKanjiQuestion question)
        {
            bool useHiragana = displaySettings != null && displaySettings.UseHiragana.CurrentValue;
            realKanjiView.ShowAnswerInfo(
                question.Kanji, question.Reading,
                RubyText.Render(question.Meaning.Meaning, useHiragana),
                RubyText.Render(question.Meaning.Example, useHiragana),
                question.Words);
        }

        protected override void OnDispose()
        {
            settingSubscription?.Dispose();
        }
    }
}
