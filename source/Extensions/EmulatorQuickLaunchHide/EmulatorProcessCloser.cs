// 文件用途：关闭上一次由插件记录的模拟器进程，避免残留进程影响下一次启动。
using Playnite.SDK;
using System;
using System.Diagnostics;

namespace EmulatorQuickLaunchHide
{
    // 提供关闭模拟器进程的操作。
    public interface IEmulatorProcessCloser
    {
        void CloseProcess(int processId);
    }

    // 根据 Playnite 返回的启动进程 ID 结束模拟器进程。
    public class EmulatorProcessCloser : IEmulatorProcessCloser
    {
        private readonly ILogger logger = LogManager.GetLogger();

        // 结束指定进程；进程不存在时直接忽略。
        public void CloseProcess(int processId)
        {
            if (processId <= 0)
            {
                return;
            }

            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to close emulator process.");
            }
        }
    }
}
