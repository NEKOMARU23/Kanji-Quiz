using System.Collections.Generic;
using R3;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての画面（熟語と、読みの選択肢）
    /// Presenter はこの View を通して画面を変える。View は判断を持たない
    /// </summary>
    public interface IReadingQuizView
    {
        /// <summary>選択肢を押したときの番号</summary>
        Observable<int> OnOptionClicked { get; }

        /// <summary>見せる熟語</summary>
        void SetWord(string word);

        /// <summary>選択肢の読みを並べる（並びは問題で確定している）</summary>
        void SetOptions(IReadOnlyList<string> readings);

        /// <summary>選択肢を選んでいるかどうかの表示を変える</summary>
        void SetOptionSelected(int index, bool selected);

        /// <summary>選択肢を押せるかどうか</summary>
        void SetOptionsInteractable(bool interactable);

        /// <summary>振り返りで、その選択肢が正しい読みかどうかの表示を変える</summary>
        void SetOptionCorrect(int index, bool correct);

        /// <summary>「読みを1つ選ぶ」「読みをすべて選ぶ」などの案内</summary>
        void SetHint(string hint);
    }
}
