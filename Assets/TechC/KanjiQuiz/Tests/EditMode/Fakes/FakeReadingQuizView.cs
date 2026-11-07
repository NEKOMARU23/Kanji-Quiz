using System.Collections.Generic;
using R3;
using TechC.KanjiQuiz.Modes.ReadingQuiz;

namespace TechC.KanjiQuiz.Tests
{
    /// <summary>テスト用の読み当ての画面。画面の状態を記録するだけ</summary>
    public class FakeReadingQuizView : IReadingQuizView
    {
        private readonly Subject<int> onOptionClicked = new Subject<int>();

        public Observable<int> OnOptionClicked => onOptionClicked;

        public string Word { get; private set; }
        public IReadOnlyList<string> Options { get; private set; } = new List<string>();
        public bool[] Selected { get; private set; } = new bool[0];
        public bool OptionsInteractable { get; private set; }
        public string Hint { get; private set; }

        public void SetWord(string word) => Word = word;

        public void SetOptions(IReadOnlyList<string> readings)
        {
            Options = readings;
            Selected = new bool[readings.Count];
        }

        public void SetOptionSelected(int index, bool selected)
        {
            if (index >= 0 && index < Selected.Length) Selected[index] = selected;
        }

        public void SetOptionsInteractable(bool interactable) => OptionsInteractable = interactable;

        public void SetHint(string hint) => Hint = hint;

        /// <summary>選択肢を押したことにする</summary>
        public void Click(int index) => onOptionClicked.OnNext(index);
    }
}
