// 文件用途：注册 Windows 全局结束快捷键，并把按键事件交给模拟器遮罩插件处理。
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace EmulatorQuickLaunchHide
{
    // 表示一个系统级结束快捷键。
    public class EmulatorExitHotkey
    {
        public Key Key { get; private set; }
        public ModifierKeys Modifiers { get; private set; }

        // 保存快捷键主键和修饰键。
        public EmulatorExitHotkey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }
    }

    // 负责注册和注销系统级结束快捷键。
    public interface IEmulatorExitHotkeyService
    {
        void Register(EmulatorExitHotkey hotkey, Action action);
        void Unregister();
    }

    // 使用隐藏消息窗口接收 WM_HOTKEY。
    public class EmulatorExitHotkeyService : IEmulatorExitHotkeyService
    {
        private const int HotkeyId = 0x6E02;
        private const int WmHotkey = 0x0312;
        private static readonly IntPtr HwndMessage = new IntPtr(-3);
        private HwndSource source;
        private Action action;
        private bool registered;

        // 注册新的全局结束快捷键。
        public void Register(EmulatorExitHotkey hotkey, Action action)
        {
            if (hotkey == null)
            {
                throw new ArgumentNullException("hotkey");
            }

            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            Unregister();
            this.action = action;

            var parameters = CreateSinkParameters();
            source = new HwndSource(parameters);
            source.AddHook(WndProc);

            var virtualKey = KeyInterop.VirtualKeyFromKey(hotkey.Key);
            if (!RegisterHotKey(source.Handle, HotkeyId, (uint)hotkey.Modifiers, (uint)virtualKey))
            {
                var error = Marshal.GetLastWin32Error();
                source.RemoveHook(WndProc);
                source.Dispose();
                source = null;
                throw new Win32Exception(error);
            }

            registered = true;
        }

        // 注销当前全局快捷键。
        public void Unregister()
        {
            if (source != null)
            {
                if (registered)
                {
                    UnregisterHotKey(source.Handle, HotkeyId);
                }

                source.RemoveHook(WndProc);
                source.Dispose();
                source = null;
            }

            registered = false;
            action = null;
        }

        // 创建不显示在桌面上的 message-only 消息窗口。
        internal static HwndSourceParameters CreateSinkParameters()
        {
            var parameters = new HwndSourceParameters("EmulatorQuickLaunchHideExitHotkeySink");
            parameters.Width = 0;
            parameters.Height = 0;
            parameters.WindowStyle = 0;
            parameters.ExtendedWindowStyle = 0;
            parameters.ParentWindow = HwndMessage;
            return parameters;
        }

        // 接收系统快捷键消息。
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
            {
                if (action != null)
                {
                    action();
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
