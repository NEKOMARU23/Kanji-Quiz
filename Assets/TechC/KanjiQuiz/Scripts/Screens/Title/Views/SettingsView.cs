using R3;
using TechC.KanjiQuiz.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Screens.Title
{
    /// <summary>
    /// タイトルから開く設定画面の表示
    /// タイトルの黒板に、効果音の音量（チョークのつまみ）、ひらがなで表示（しない／する を丸で囲む）、記録を消す を板書する
    /// [← タイトルへ] か Esc で閉じる（確認を出しているときは、Esc で確認だけを閉じる）
    /// </summary>
    public class SettingsView : MonoBehaviour, ISettingsView
    {
        [Tooltip("設定の中身（黒板の中に置く）")]
        [SerializeField] private GameObject root;
        [SerializeField] private Button backButton;

        [Header("--- 効果音の音量 ---")]
        [SerializeField] private Slider seVolumeSlider;

        [Header("--- ひらがなで表示 ---")]
        [SerializeField] private Button hiraganaOffButton;
        [SerializeField] private Button hiraganaOnButton;
        [Tooltip("選んでいる方を囲むチョークの丸（しない）")]
        [SerializeField] private GameObject hiraganaOffMark;
        [Tooltip("選んでいる方を囲むチョークの丸（する）")]
        [SerializeField] private GameObject hiraganaOnMark;

        [Header("--- 記録を消す ---")]
        [SerializeField] private Button eraseButton;
        [Tooltip("何を消すかの説明。消したあとは、消したことを出す")]
        [SerializeField] private TMP_Text eraseDescriptionText;
        [SerializeField] private string eraseDescription = "{自己|じこ}ベスト・{苦手|にがて}な{漢字|かんじ}・{漢字帳|かんじちょう}を{消|け}します";
        [SerializeField] private string erasedMessage = "{記録|きろく}を{消|け}しました";
        [SerializeField] private Color erasedColor = new Color(0.957f, 0.827f, 0.369f, 1f);
        [Tooltip("消すかどうかの確認")]
        [SerializeField] private GameObject eraseConfirm;
        [SerializeField] private Button eraseConfirmButton;
        [SerializeField] private Button eraseCancelButton;

        private readonly Subject<Unit> escapePressed = new Subject<Unit>();
        private readonly Subject<bool> hiraganaChanged = new Subject<bool>();
        private Color descriptionColor;

        public Observable<Unit> OnBackClicked => backButton.OnClickAsObservable().Merge(escapePressed);
        // OnValueChangedAsObservable は購読したときに今の値も流すので、変わったときだけ流す onValueChanged を使う
        public Observable<float> OnSeVolumeChanged => seVolumeSlider.onValueChanged.AsObservable(destroyCancellationToken);
        public Observable<bool> OnHiraganaChanged => hiraganaChanged;
        public Observable<Unit> OnEraseClicked => eraseButton.OnClickAsObservable();
        public Observable<Unit> OnEraseConfirmed => eraseConfirmButton.OnClickAsObservable();
        public Observable<Unit> OnEraseCancelled => eraseCancelButton.OnClickAsObservable();

        private void Awake()
        {
            escapePressed.AddTo(this);
            hiraganaChanged.AddTo(this);
            descriptionColor = eraseDescriptionText.color;
            hiraganaOffButton.OnClickAsObservable().Subscribe(this, static (_, self) => self.hiraganaChanged.OnNext(false)).AddTo(this);
            hiraganaOnButton.OnClickAsObservable().Subscribe(this, static (_, self) => self.hiraganaChanged.OnNext(true)).AddTo(this);
        }

        private void Update()
        {
            if (!root.activeInHierarchy || InputGate.IsBlocked) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) escapePressed.OnNext(Unit.Default);
        }

        public void SetVisible(bool visible) => root.SetActive(visible);

        public void SetSeVolume(float volume) => seVolumeSlider.SetValueWithoutNotify(volume);

        public void SetHiragana(bool useHiragana)
        {
            hiraganaOffMark.SetActive(!useHiragana);
            hiraganaOnMark.SetActive(useHiragana);
        }

        public void SetEraseConfirmVisible(bool visible) => eraseConfirm.SetActive(visible);

        public void SetErasedMessageVisible(bool visible)
        {
            eraseDescriptionText.text = visible ? erasedMessage : eraseDescription;
            eraseDescriptionText.color = visible ? erasedColor : descriptionColor;
        }
    }
}
