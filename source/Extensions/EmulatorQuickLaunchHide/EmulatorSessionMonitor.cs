// 文件用途：监听模拟器窗口标题，发现游戏退出回到模拟器主界面时通知插件。
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EmulatorQuickLaunchHide
{
    // 提供模拟器会话监听能力。
    public interface IEmulatorSessionMonitor
    {
        void Start(int processId, string gameName, string romPath, Action returnedToLauncher, CancellationToken cancellationToken);
    }

    // 通过窗口标题判断模拟器是否已经从游戏返回主界面。
    public class EmulatorSessionMonitor : IEmulatorSessionMonitor
    {
        public void Start(int processId, string gameName, string romPath, Action returnedToLauncher, CancellationToken cancellationToken)
        {
            if (processId <= 0 || string.IsNullOrWhiteSpace(gameName) || returnedToLauncher == null)
            {
                return;
            }

            Task.Run(async () =>
            {
                var sawGameWindow = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var title = GetMainWindowTitle(processId);
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        if (ContainsGameName(title, gameName))
                        {
                            sawGameWindow = true;
                        }
                        else if (sawGameWindow)
                        {
                            returnedToLauncher();
                            return;
                        }
                    }

                    await Task.Delay(1000, cancellationToken).ContinueWith(_ => { });
                }
            }, cancellationToken);
        }

        // 读取模拟器进程树里第一个可见窗口标题。
        internal static string GetMainWindowTitle(int processId)
        {
            foreach (var id in ProcessWindowHandoff.GetProcessTreeIds(processId))
            {
                try
                {
                    using (var process = Process.GetProcessById(id))
                    {
                        process.Refresh();
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            return process.MainWindowTitle;
                        }
                    }
                }
                catch
                {
                }
            }

            return string.Empty;
        }

        // 判断标题是否仍然处于具体游戏窗口。
        internal static bool ContainsGameName(string title, string gameName)
        {
            return !string.IsNullOrWhiteSpace(title) &&
                   !string.IsNullOrWhiteSpace(gameName) &&
                   title.IndexOf(gameName, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
