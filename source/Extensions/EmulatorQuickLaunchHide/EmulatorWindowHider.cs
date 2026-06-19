// 文件用途：在模拟器游戏停止后隐藏这次启动的模拟器主窗口，避免关闭模拟器导致主界面卡住。
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmulatorQuickLaunchHide
{
    // 提供隐藏模拟器窗口的最小操作。
    public interface IEmulatorWindowHider
    {
        void HideWindow(int processId);
    }

    // 根据 Playnite 记录的启动进程 ID 隐藏模拟器主窗口。
    public class EmulatorWindowHider : IEmulatorWindowHider
    {
        private const int SW_HIDE = 0;

        // 隐藏启动进程及其子进程中的主窗口，不关闭模拟器进程。
        public void HideWindow(int processId)
        {
            if (processId <= 0)
            {
                return;
            }

            foreach (var id in ProcessWindowHandoff.GetProcessTreeIds(processId))
            {
                try
                {
                    using (var process = Process.GetProcessById(id))
                    {
                        process.Refresh();
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            ShowWindow(process.MainWindowHandle, SW_HIDE);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
