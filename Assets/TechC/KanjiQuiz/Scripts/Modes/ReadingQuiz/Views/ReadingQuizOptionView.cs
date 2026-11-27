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

        [Tooltip("選ぶときに押す数字キーを出すテキスト。不要なら空でよい")]
        [SerializeField] private TMP_Text keyHintText;

        private readonly Subject<int> onClicked = new Subject<int>();
        private static readonly Color NormalColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color CorrectColor = new Color(0.12f, 0.42f, 0.2f, 1f);
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
            if (keyHintText != null) keyHintText.text = (optionIndex + 1).ToString();
            SetSelected(false);
        }

        /// <summary>選んでいるかどうかの印を変える</summary>
        public void SetSelected(bool selected)
        {
            if (selectedMark != null) selectedMark.SetActive(selected);
        }

        /// <summary>振り返りで、正しい読みなら文字を緑にする</summary>
        public void SetCorrect(bool correct)
        {
            if (label != null) label.color = correct ? CorrectColor : NormalColor;
        }

        /// <summary>押せるかどうか</summary>
        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }
    }
}
