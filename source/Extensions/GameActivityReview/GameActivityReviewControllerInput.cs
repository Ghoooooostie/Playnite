// 文件用途：识别 Playnite 全屏模式传入插件控件的手柄确认和返回按键。
using System;
using System.Reflection;
using System.Windows.Input;

namespace GameActivityReview
{
    // 通过反射读取 Playnite 内部手柄事件，避免插件新增非 SDK 编译依赖。
    internal static class GameActivityReviewControllerInput
    {
        // 判断是否为手柄确认键。
        public static bool IsConfirmation(KeyEventArgs args)
        {
            return IsControllerButton(args, "ConfirmationBinding");
        }

        // 判断是否为手柄返回键。
        public static bool IsCancellation(KeyEventArgs args)
        {
            return IsControllerButton(args, "CancellationBinding");
        }

        // 判断事件是否匹配指定手柄绑定。
        private static bool IsControllerButton(KeyEventArgs args, string bindingName)
        {
            if (args == null || args.GetType().Name != "GameControllerInputEventArgs")
            {
                return false;
            }

            var state = args.GetType().GetProperty("ButtonState")?.GetValue(args, null);
            if (state == null || !string.Equals(state.ToString(), "Pressed", StringComparison.Ordinal))
            {
                return false;
            }

            var button = args.GetType().GetProperty("Button")?.GetValue(args, null);
            var gestureType = Type.GetType("Playnite.Input.GameControllerGesture, Playnite");
            var binding = gestureType?.GetProperty(bindingName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
            return button != null && binding != null && string.Equals(button.ToString(), binding.ToString(), StringComparison.Ordinal);
        }
    }
}
