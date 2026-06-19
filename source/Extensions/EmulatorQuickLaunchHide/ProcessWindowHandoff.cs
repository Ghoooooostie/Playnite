// 文件用途：等待遮罩时间结束，不对模拟器窗口做任何操作。
using System;
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
    }
}
