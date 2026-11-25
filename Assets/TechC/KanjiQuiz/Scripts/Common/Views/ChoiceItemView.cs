using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 選択肢1つ分の View
    /// トグルと、モードごとの中身を置く場所（ContentRoot）を持つ
    /// 行のどこをクリックしても、その行のトグルを選ぶ（行の背景の Image は Raycast Target を有効にしておく）
    /// </summary>
    public class ChoiceItemView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Toggle toggle;
        [Tooltip("モードごとの中身（文字、画像、書き順の図など）を置く親")]
        [SerializeField] private RectTransform contentRoot;
        [Tooltip("選ぶときに押す数字キーを表示するテキスト。不要なら空でよい")]
        [SerializeField] private TMP_Text keyHintText;

        [Header("--- 振り返り用の印 ---")]
        [Tooltip("行の背景。印に合わせて色を変える。不要なら空でよい")]
        [SerializeField] private Image background;
        [Tooltip("「正解」「あなたの回答」を表示するテキスト。不要なら空でよい")]
        [SerializeField] private TMP_Text markText;
        [SerializeField] private Color correctBackgroundColor = new Color(0.86f, 0.95f, 0.86f, 1f);
        [SerializeField] private Color wrongBackgroundColor = new Color(0.98f, 0.87f, 0.85f, 1f);
        [SerializeField] private Color correctTextColor = new Color(0.2f, 0.55f, 0.25f, 1f);
        [SerializeField] private Color wrongTextColor = new Color(0.78f, 0.29f, 0.19f, 1f);

        private Color normalBackgroundColor;
        private bool hasNormalBackgroundColor;

        /// <summary>モードごとの中身を置く親</summary>
        public RectTransform ContentRoot => contentRoot;

        /// <summary>この選択肢が選ばれたとき</summary>
        public Observable<Unit> OnSelected =>
            toggle.onValueChanged.AsObservable(destroyCancellationToken)
                .Where(isOn => isOn)
                .AsUnitObservable();

        /// <summary>
        /// トグルグループを設定する
        /// </summary>
        public void SetGroup(ToggleGroup group)
        {
            toggle.group = group;
        }

        /// <summary>
        /// 行（トグル以外の部分）がクリックされたら、トグルを選ぶ
        /// トグル自体のクリックはトグルが処理するので、ここには届かない
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Select();
        }

        /// <summary>
        /// この選択肢を選ぶ（クリックやキー操作と同じ扱い）。押せないときは何もしない
        /// </summary>
        public void Select()
        {
            if (!toggle.IsInteractable()) return;

            toggle.isOn = true;
        }

        /// <summary>
        /// 選ぶときに押すキーを表示する
        /// </summary>
        public void SetKeyHint(string key)
        {
            if (keyHintText != null) keyHintText.text = key;
        }

        /// <summary>
        /// 押せるかどうかを切り替える
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            toggle.interactable = interactable;
        }

        /// <summary>
        /// 振り返り用の印をつける
        /// </summary>
        public void SetMark(ChoiceMark mark)
        {
            if (background != null)
            {
                if (!hasNormalBackgroundColor)
                {
                    normalBackgroundColor = background.color;
                    hasNormalBackgroundColor = true;
                }

                switch (mark)
                {
                    case ChoiceMark.Correct: background.color = correctBackgroundColor; break;
                    case ChoiceMark.SelectedWrong: background.color = wrongBackgroundColor; break;
                    default: background.color = normalBackgroundColor; break;
                }
            }

            if (markText != null)
            {
                switch (mark)
                {
                    case ChoiceMark.Correct:
                        markText.text = "{正解|せいかい}";
                        markText.color = correctTextColor;
                        break;
                    case ChoiceMark.SelectedWrong:
                        markText.text = "あなたの{回答|かいとう}";
                        markText.color = wrongTextColor;
                        break;
                    default:
                        markText.text = string.Empty;
                        break;
                }
            }
        }

        /// <summary>
        /// 選択状態を設定する。イベントは発行しない
        /// </summary>
        public void SetIsOnWithoutNotify(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }
    }
}
