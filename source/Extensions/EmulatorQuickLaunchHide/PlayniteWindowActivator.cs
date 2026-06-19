// 文件用途：在模拟器游戏结束后恢复并激活 Playnite 主窗口。
using Playnite.SDK;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EmulatorQuickLaunchHide
{
    // 提供恢复 Playnite 主窗口的最小操作。
    public interface IPlayniteWindowActivator
    {
        void Activate();
    }

    // 通过 Playnite API 获取当前应用窗口并置前。
    public class PlayniteWindowActivator : IPlayniteWindowActivator
    {
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private readonly IPlayniteAPI api;

        public PlayniteWindowActivator(IPlayniteAPI api)
        {
            this.api = api;
        }

        // 恢复并激活 Playnite 当前窗口。
        public void Activate()
        {
            RunOnUiThread(() =>
            {
                var window = api?.Dialogs?.GetCurrentAppWindow();
                if (window == null)
                {
                    return;
                }

                RestoreAndActivate(window);
            });
        }

        // 使用 WPF 激活和 Win32 置前组合，降低被模拟器抢焦点的概率。
        private static void RestoreAndActivate(Window window)
        {
            if (window.Visibility != Visibility.Visible)
            {
                window.Show();
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            if (!window.Activate())
            {
                window.Topmost = true;
                window.Topmost = false;
            }

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                window.Focus();
                return;
            }

            var foregroundWindow = GetForegroundWindow();
            var foregroundThread = foregroundWindow == IntPtr.Zero ? 0 : GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
            var targetThread = GetWindowThreadProcessId(handle, IntPtr.Zero);
            var attached = foregroundThread != 0 && targetThread != 0 && foregroundThread != targetThread && AttachThreadInput(foregroundThread, targetThread, true);

            try
            {
                SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
                window.Activate();
                window.Focus();
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(foregroundThread, targetThread, false);
                }
            }
        }

        // 确保 WPF 窗口操作在 UI 线程执行。
        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    }
}
