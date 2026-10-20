using R3;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 選択肢1つ分の View
    /// トグルと、モードごとの中身を置く場所（ContentRoot）を持つ
    /// </summary>
    public class ChoiceItemView : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [Tooltip("モードごとの中身（文字、画像、書き順の図など）を置く親")]
        [SerializeField] private RectTransform contentRoot;

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
        /// 選択状態を設定する。イベントは発行しない
        /// </summary>
        public void SetIsOnWithoutNotify(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }
    }
}
