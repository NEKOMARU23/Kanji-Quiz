using System.Collections.Generic;
using R3;

namespace TechC.KanjiQuiz.Modes.FourCharIdiom
{
    /// <summary>
    /// 四字熟語づくり固有の表示（意味、4つのマス、候補の字、苦手の印、振り返りの読みと例文）
    /// </summary>
    public interface IFourCharIdiomView
    {
        /// <summary>候補の字が押されたとき（候補の番号。数字キーなどを含む）</summary>
        Observable<int> OnTileClicked { get; }

        /// <summary>マスが押されたとき（マスの番号 0〜3）。Backspace は -1（いちばん後ろの埋めたマスを戻す）</summary>
        Observable<int> OnCellClicked { get; }

        /// <summary>熟語の意味（ヒント）を出す。表示する文を渡す</summary>
        void SetMeaning(string meaning);

        /// <summary>4つのマスの字と見せ方を出す</summary>
        void SetCells(IReadOnlyList<FourCharIdiomCell> cells);

        /// <summary>候補の字を並べる。使った候補（マスに入れたもの）は薄くする</summary>
        void SetTiles(IReadOnlyList<string> tiles, IReadOnlyList<bool> used);

        /// <summary>候補とマスを押せるかどうか（振り返りでは押せない）</summary>
        void SetInteractable(bool interactable);

        /// <summary>出題している熟語が苦手な熟語（前に間違えた熟語）であることを示す印を出すかどうか</summary>
        void SetWeakMarkVisible(bool visible);

        /// <summary>
        /// 振り返りで、正解の熟語・読み・例文を出す。プレイ中は空の文字列を渡して隠す
        /// </summary>
        /// <param name="example">例文（表示する文）。熟語を強調して出す</param>
        void ShowAnswerInfo(string word, string reading, string example);
    }
}
