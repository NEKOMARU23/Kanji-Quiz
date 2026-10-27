using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 中断メニューの表示
    /// 書き順の線（LineRenderer）より手前に出すため、描画順を上げた自分の Canvas に置く
    /// </summary>
    public class PauseMenuView : MonoBehaviour, IPauseMenuView
    {
        [Tooltip("画面の右上の [メニュー] ボタン")]
        [SerializeField] private Button openButton;
        [Tooltip("メニューを開いたときに表示するもの（暗くする幕とメニューの紙）")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Slider seVolumeSlider;

        private readonly Subject<Unit> onMenuKeyPressed = new Subject<Unit>();
        private bool isOpen;

        public Observable<Unit> OnOpenClicked => openButton.OnClickAsObservable();
        public Observable<Unit> OnMenuKeyPressed => onMenuKeyPressed;
        public Observable<Unit> OnResumeClicked => resumeButton.OnClickAsObservable();
        public Observable<Unit> OnRestartClicked => restartButton.OnClickAsObservable();
        // OnValueChangedAsObservable は購読したときに今の値も流すので、変わったときだけ流す onValueChanged を使う
        public Observable<float> OnSeVolumeChanged => seVolumeSlider.onValueChanged.AsObservable(destroyCancellationToken);

        private void Awake()
        {
            onMenuKeyPressed.AddTo(this);
            panel.SetActive(false);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            // 閉じているときは、ほかの理由（シーンの切り替えなど）でキー操作が止まっていれば開かない
            if (!isOpen && InputGate.IsBlocked) return;

            onMenuKeyPressed.OnNext(Unit.Default);
        }

        private void OnDestroy()
        {
            InputGate.Unblock(this);
        }

        public void Show()
        {
            isOpen = true;
            panel.SetActive(true);
            InputGate.Block(this);
        }

        public void Hide()
        {
            isOpen = false;
            panel.SetActive(false);
            InputGate.Unblock(this);
        }

        public void SetSeVolume(float volume)
        {
            seVolumeSlider.SetValueWithoutNotify(volume);
        }
    }
}
