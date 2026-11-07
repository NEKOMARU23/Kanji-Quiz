using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Modes.ReadingQuiz
{
    /// <summary>
    /// 読み当ての選択肢1つ（プレハブ ReadingQuizOption にする）
    /// 読みを出し、選んでいるときは印を出す。押した番号を通知するだけで、判断はしない
    /// </summary>
    public class ReadingQuizOptionView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [Tooltip("選んでいるときに出す印（チェックなど）")]
        [SerializeField] private GameObject selectedMark;

        private readonly Subject<int> onClicked = new Subject<int>();
        private int index;

        /// <summary>押したときの番号</summary>
        public Observable<int> OnClicked => onClicked;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(() => onClicked.OnNext(index));
        }

        private void OnDestroy()
        {
            onClicked.Dispose();
        }

        /// <summary>番号と読みを設定する</summary>
        public void Setup(int optionIndex, string reading)
        {
            index = optionIndex;
            if (label != null) label.text = reading;
            SetSelected(false);
        }

        /// <summary>選んでいるかどうかの印を変える</summary>
        public void SetSelected(bool selected)
        {
            if (selectedMark != null) selectedMark.SetActive(selected);
        }

        /// <summary>押せるかどうか</summary>
        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }
    }
}
