// 文件用途：等待遮罩时间结束，并提供按启动进程遍历子进程的通用能力。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace EmulatorQuickLaunchHide
{
    // 提供延迟关闭遮罩的操作。
    public interface IWindowHandoff
    {
        Task HandoffAsync(int processId, int holdSeconds, CancellationToken cancellationToken);
    }

    // 只等待配置时间，避免干预模拟器窗口大小、焦点或进程。
    public class ProcessWindowHandoff : IWindowHandoff
    {
        // 等待配置的遮罩时间。
        public async Task HandoffAsync(int processId, int holdSeconds, CancellationToken cancellationToken)
        {
            if (holdSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(holdSeconds), cancellationToken);
            }
        }

        // 返回启动进程及其全部子进程 ID，供隐藏窗口和监听标题共用。
        internal static IEnumerable<int> GetProcessTreeIds(int rootProcessId)
        {
            if (rootProcessId <= 0)
            {
                return Enumerable.Empty<int>();
            }

            var result = new List<int>();
            CollectProcessTreeIds(rootProcessId, result);
            return result;
        }

        // 递归收集进程树中的所有进程 ID。
        private static void CollectProcessTreeIds(int processId, ICollection<int> result)
        {
            if (result.Contains(processId))
            {
                return;
            }

            result.Add(processId);
            using (var searcher = new ManagementObjectSearcher(
                $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId={processId}"))
            {
                foreach (var item in searcher.Get().Cast<ManagementObject>())
                {
                    CollectProcessTreeIds(Convert.ToInt32(item["ProcessId"]), result);
                }
            }
        }
    }
}
