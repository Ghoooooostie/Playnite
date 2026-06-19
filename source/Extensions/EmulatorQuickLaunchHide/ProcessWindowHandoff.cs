// 文件用途：等待遮罩时间结束后，把前台窗口交给模拟器进程。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace EmulatorQuickLaunchHide
{
    // 提供延迟切换到模拟器窗口的操作。
    public interface IWindowHandoff
    {
        Task HandoffAsync(int processId, int holdSeconds, CancellationToken cancellationToken);
    }

    // 根据启动进程和子进程寻找窗口并切到前台。
    public class ProcessWindowHandoff : IWindowHandoff
    {
        private const int SW_RESTORE = 9;

        public async Task HandoffAsync(int processId, int holdSeconds, CancellationToken cancellationToken)
        {
            if (holdSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken);
            }

            var window = await WaitForMainWindowAsync(processId, cancellationToken);
            if (window != IntPtr.Zero)
            {
                ShowWindow(window, SW_RESTORE);
                SetForegroundWindow(window);
            }
        }

        // 等待模拟器窗口出现，最多等待 10 秒。
        private static async Task<IntPtr> WaitForMainWindowAsync(int processId, CancellationToken cancellationToken)
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < timeoutAt)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var window = FindMainWindow(processId);
                if (window != IntPtr.Zero)
                {
                    return window;
                }

                await Task.Delay(250, cancellationToken);
            }

            return IntPtr.Zero;
        }

        // 查找启动进程及其子进程的可见窗口。
        internal static IntPtr FindMainWindow(int processId)
        {
            if (processId <= 0)
            {
                return IntPtr.Zero;
            }

            var processIds = GetProcessTreeIds(processId);
            foreach (var id in processIds)
            {
                try
                {
                    var process = Process.GetProcessById(id);
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        return process.MainWindowHandle;
                    }
                }
                catch
                {
                }
            }

            return IntPtr.Zero;
        }

        // 构建进程树 ID 列表，先父进程再子进程。
        internal static List<int> GetProcessTreeIds(int rootProcessId)
        {
            var ids = new List<int> { rootProcessId };
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var parentId = GetParentProcessId(process);
                    if (parentId.HasValue && ids.Contains(parentId.Value) && !ids.Contains(process.Id))
                    {
                        ids.Add(process.Id);
                    }
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            return ids;
        }

        // 读取父进程 ID，用于处理模拟器启动器再拉起真实窗口的情况。
        private static int? GetParentProcessId(Process process)
        {
            var info = new PROCESS_BASIC_INFORMATION();
            var status = NtQueryInformationProcess(
                process.Handle,
                0,
                ref info,
                Marshal.SizeOf(info),
                out _);

            return status == 0 ? (int?)info.InheritedFromUniqueProcessId.ToInt32() : null;
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength,
            out int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_BASIC_INFORMATION
        {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }
    }
}
