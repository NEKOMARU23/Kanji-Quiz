using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TechC.KanjiQuiz.Common;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 四字熟語づくりの Presenter
    /// 候補を押すと最初の空いたマスに入れ、埋めたマスを押すと候補に戻す。空いたマスがなくなったら [次へ] を押せる
    /// 入れた候補の番号は FourCharIdiomAnswer で回答の記録にまとめるので、前の問題に戻ると元に戻せる
    /// </summary>
    public sealed class FourCharIdiomPresenter : QuizPresenterBase<FourCharIdiomQuestion>
    {
        private readonly IFourCharIdiomView view;
        private readonly WeakQuestionList weakList;
        private readonly DisplaySettings displaySettings;
        private int[] filledTiles = Array.Empty<int>();

        /// <param name="weakList">苦手な熟語。出題した熟語が入っていれば「苦手」の印を出す。null なら出さない</param>
        /// <param name="displaySettings">意味をひらがなで出すかどうか。null なら漢字のまま</param>
        public FourCharIdiomPresenter(
            QuizSession<FourCharIdiomQuestion> session,
            IQuizFrameView frameView,
            IFourCharIdiomView view,
            IAudioPlayer audioPlayer = null,
            WeakQuestionList weakList = null,
            DisplaySettings displaySettings = null)
            : base(session, frameView, audioPlayer)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.weakList = weakList;
            this.displaySettings = displaySettings;
        }

        protected override void OnInitialize()
        {
            view.OnTileClicked.Subscribe(this, static (tile, self) => self.PutTile(tile)).AddTo(Disposables);
            view.OnCellClicked.Subscribe(this, static (cell, self) => self.TakeBack(cell)).AddTo(Disposables);
            // 表示の設定が変わったら、意味を出し直す（購読した時点の値では呼ばない）
            displaySettings?.UseHiragana
                .Skip(1)
                .Subscribe(this, static (_, self) => self.ShowMeaning(self.Session.Current))
                .AddTo(Disposables);
        }

        protected override UniTask ShowQuestionAsync(FourCharIdiomQuestion question, AnswerRecord previousAnswer, CancellationToken ct)
        {
            filledTiles = FourCharIdiomAnswer.Decode(previousAnswer.IsAnswered ? previousAnswer.SelectedIndex : -1, question.BlankPositions.Count);
            view.SetInteractable(true);
            view.SetWeakMarkVisible(weakList != null && weakList.Contains(question.Word));
            view.ShowAnswerInfo(string.Empty, string.Empty, string.Empty);
            ShowMeaning(question);
            ShowBoard(question);
            return UniTask.CompletedTask;
        }

        protected override bool TryJudge(FourCharIdiomQuestion question, out int selectedIndex, out bool isCorrect)
        {
            selectedIndex = FourCharIdiomAnswer.Encode(filledTiles);
            isCorrect = question.IsCorrect(filledTiles);
            return question.IsComplete(filledTiles);
        }

        /// <summary>候補をいちばん前の空いたマスに入れる</summary>
        private void PutTile(int tile)
        {
            var question = Session.Current;
            if (tile < 0 || tile >= question.Tiles.Count || Array.IndexOf(filledTiles, tile) >= 0) return;

            int blank = Array.IndexOf(filledTiles, -1);
            if (blank < 0) return;

            filledTiles[blank] = tile;
            AudioPlayer.PlaySe(SeType.Select);
            ShowBoard(question);
            RefreshNextButton();
        }

        /// <summary>埋めたマスの字を候補に戻す。cell が -1 なら、いちばん後ろの埋めたマスを戻す</summary>
        private void TakeBack(int cell)
        {
            var question = Session.Current;
            int index = -1;
            for (int k = 0; k < filledTiles.Length; k++)
            {
                if (filledTiles[k] < 0) continue;
                if (cell < 0 || question.BlankPositions[k] == cell) index = k;
            }
            if (index < 0) return;

            filledTiles[index] = -1;
            AudioPlayer.PlaySe(SeType.Select);
            ShowBoard(question);
            RefreshNextButton();
        }

        private void ShowMeaning(FourCharIdiomQuestion question)
        {
            // 意味の中の、空いたマスの字を含む言葉は読みで出す（答えが見えないように）
            bool useHiragana = displaySettings != null && displaySettings.UseHiragana.CurrentValue;
            view.SetMeaning(FourCharIdiomHint.Render(question, useHiragana));
        }

        private void ShowBoard(FourCharIdiomQuestion question)
        {
            view.SetCells(FourCharIdiomBoard.Cells(question, filledTiles, review: false));
            view.SetTiles(question.Tiles, FourCharIdiomBoard.UsedTiles(question, filledTiles));
        }
    }
}
