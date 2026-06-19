// 文件用途：注册 Windows 全局截图快捷键，并把热键事件转给插件。
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace GameScreenshots
{
    // 使用 message-only 窗口接收 WM_HOTKEY，不在桌面、任务栏或 Alt-Tab 中显示。
    public class ScreenshotHotkeyService : IScreenshotHotkeyService
    {
        private static readonly IntPtr MessageOnlyWindow = new IntPtr(-3);
        private const int HotkeyId = 0x5139;
        private const int WmHotkey = 0x0312;
        private HwndSource source;
        private Action action;
        private bool registered;

        // 创建真正隐藏的消息窗口参数。
        internal static HwndSourceParameters CreateMessageOnlyWindowParameters()
        {
            var parameters = new HwndSourceParameters("GameScreenshotsHotkeySink");
            parameters.ParentWindow = MessageOnlyWindow;
            parameters.Width = 0;
            parameters.Height = 0;
            return parameters;
        }

        public void Register(ScreenshotHotkey hotkey, Action action)
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

            source = new HwndSource(CreateMessageOnlyWindowParameters());
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

        public void Unregister()
        {
            if (source != null && RequiresDispatcherInvoke(source.Dispatcher))
            {
                source.Dispatcher.Invoke(new Action(UnregisterOnSourceDispatcher));
                return;
            }

            UnregisterOnSourceDispatcher();
        }

        // 判断是否需要切回热键窗口所属线程释放。
        internal static bool RequiresDispatcherInvoke(Dispatcher dispatcher)
        {
            return dispatcher != null && !dispatcher.CheckAccess();
        }

        // 在热键窗口所属线程注销并释放窗口。
        private void UnregisterOnSourceDispatcher()
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

        // 接收系统热键消息。
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
