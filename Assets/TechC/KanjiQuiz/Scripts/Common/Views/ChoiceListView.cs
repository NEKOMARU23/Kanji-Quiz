using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 選択肢を並べて、1つだけ選べるようにする View
    /// 選択肢は必要な数だけ生成し、余った分は非表示にして使い回す
    /// </summary>
    public class ChoiceListView : MonoBehaviour, IChoiceListView
    {
        [SerializeField] private ChoiceItemView itemPrefab;
        [SerializeField] private RectTransform container;
        [SerializeField] private ToggleGroup toggleGroup;

        private readonly List<ChoiceItemView> items = new List<ChoiceItemView>();
        private readonly Subject<int> onChoiceSelected = new Subject<int>();
        private int activeCount;
        private bool isInteractable = true;

        /// <summary>表示中の選択肢。モードごとの View が中身を描くために使う</summary>
        public IReadOnlyList<ChoiceItemView> Items => items.GetRange(0, activeCount);

        public Observable<int> OnChoiceSelected => onChoiceSelected;

        private void Awake()
        {
            onChoiceSelected.AddTo(this);
        }

        public void SetChoiceCount(int count)
        {
            while (items.Count < count)
            {
                CreateItem(items.Count);
            }

            for (int i = 0; i < items.Count; i++)
            {
                items[i].gameObject.SetActive(i < count);
            }

            activeCount = count;
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        public void SetSelected(int index)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].SetIsOnWithoutNotify(i == index);
            }
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
            foreach (var item in items)
            {
                item.SetInteractable(interactable);
            }
        }

        public void SetMarks(int correctIndex, int selectedIndex)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var mark = ChoiceMark.None;
                if (correctIndex >= 0)
                {
                    if (i == correctIndex) mark = ChoiceMark.Correct;
                    else if (i == selectedIndex) mark = ChoiceMark.SelectedWrong;
                }
                items[i].SetMark(mark);
            }
        }

        private void CreateItem(int index)
        {
            var item = Instantiate(itemPrefab, container);
            item.SetGroup(toggleGroup);
            item.SetInteractable(isInteractable);
            item.OnSelected
                .Subscribe((index, onChoiceSelected), static (_, state) => state.onChoiceSelected.OnNext(state.index))
                .AddTo(this);
            items.Add(item);
        }
    }
}
