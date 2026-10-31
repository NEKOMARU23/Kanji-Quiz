using UnityEngine.InputSystem;

namespace TechC.KanjiQuiz.Common
{
    /// <summary>
    /// 演出を飛ばす入力（左クリック、画面のタップ、Enter、スペース）
    /// 演出をしている View が Update で確かめる
    /// </summary>
    public static class SkipInput
    {
        /// <summary>このフレームで飛ばす入力があったかどうか。キー操作を止めている間は false</summary>
        public static bool WasPressedThisFrame()
        {
            if (InputGate.IsBlocked) return false;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

            var keyboard = Keyboard.current;
            return keyboard != null
                && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
        }
    }
}
