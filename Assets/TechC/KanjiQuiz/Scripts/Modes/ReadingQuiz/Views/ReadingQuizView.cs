using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての画面。熟語と、読みの選択肢（ReadingQuizOptionView）を並べる
    /// 判断は持たず、押された番号を Observable で通知するだけ
    /// </summary>
    public class ReadingQuizView : MonoBehaviour, IReadingQuizView
    {
        [Tooltip("見せる熟語の文字")]
        [SerializeField] private TMP_Text wordText;
        [Tooltip("「読みを1つ選ぶ」などの案内")]
        [SerializeField] private TMP_Text hintText;
        [Tooltip("選択肢を並べる場所（グリッドなど）")]
        [SerializeField] private Transform optionRoot;
        [Tooltip("選択肢のプレハブ（ReadingQuizOptionView を付けたもの）")]
        [SerializeField] private ReadingQuizOptionView optionPrefab;

        private readonly Subject<int> onOptionClicked = new Subject<int>();
        private readonly List<ReadingQuizOptionView> options = new List<ReadingQuizOptionView>();

        /// <inheritdoc/>
        public Observable<int> OnOptionClicked => onOptionClicked;

        private void OnDestroy()
        {
            onOptionClicked.Dispose();
        }

        /// <inheritdoc/>
        public void SetWord(string word)
        {
            if (wordText != null) wordText.text = word;
        }

        /// <inheritdoc/>
        public void SetOptions(IReadOnlyList<string> readings)
        {
            // 前の問題の選択肢を消して、今の問題の数だけ作る
            foreach (var option in options)
            {
                if (option != null) Destroy(option.gameObject);
            }
            options.Clear();

            if (optionPrefab == null || optionRoot == null)
            {
                Debug.LogError("[ReadingQuizView] 選択肢のプレハブか置き場が設定されていません。");
                return;
            }

            for (int i = 0; i < readings.Count; i++)
            {
                var option = Instantiate(optionPrefab, optionRoot);
                option.Setup(i, readings[i]);
                option.OnClicked
                    .Subscribe(this, static (index, self) => self.onOptionClicked.OnNext(index))
                    .AddTo(option);
                options.Add(option);
            }
        }

        /// <inheritdoc/>
        public void SetOptionSelected(int index, bool selected)
        {
            if (index >= 0 && index < options.Count) options[index].SetSelected(selected);
        }

        /// <inheritdoc/>
        public void SetOptionsInteractable(bool interactable)
        {
            foreach (var option in options) option.SetInteractable(interactable);
        }

        /// <inheritdoc/>
        public void SetHint(string hint)
        {
            if (hintText != null) hintText.text = hint;
        }
    }
}
