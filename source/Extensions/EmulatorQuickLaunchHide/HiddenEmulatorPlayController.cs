// 文件用途：执行隐藏模拟器启动，并把开始/结束事件回传给 Playnite。
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace EmulatorQuickLaunchHide
{
    // 执行隐藏模拟器启动，并向 Playnite 回报开始和结束事件。
    public class HiddenEmulatorPlayController : PlayController
    {
        private readonly HiddenEmulatorLaunchAction action;
        private readonly Func<HiddenEmulatorLaunchAction, HiddenEmulatorLaunchRequest> resolver;
        private CancellationTokenSource watchToken;
        private Process startedProcess;

        // 创建隐藏模拟器播放控制器。
        public HiddenEmulatorPlayController(
            HiddenEmulatorLaunchAction action,
            Func<HiddenEmulatorLaunchAction, HiddenEmulatorLaunchRequest> resolver) : base(action.Game)
        {
            this.action = action;
            this.resolver = resolver;
            Name = action.Name;
        }

        // 启动模拟器并开始跟踪运行状态。
        public override void Play(PlayActionArgs args)
        {
            var request = resolver(action);
            var startInfo = HiddenEmulatorProcessStartInfoFactory.Create(request);
            startedProcess = Process.Start(startInfo);
            if (startedProcess == null)
            {
                throw new InvalidOperationException("模拟器进程启动失败。");
            }

            InvokeOnStarted(new GameStartedEventArgs { StartedProcessId = startedProcess.Id });
            watchToken = new CancellationTokenSource();
            Task.Run(() => WatchProcessAsync(request, watchToken.Token));
        }

        // 释放跟踪任务和进程句柄。
        public override void Dispose()
        {
            watchToken?.Cancel();
            watchToken?.Dispose();
            startedProcess?.Dispose();
            base.Dispose();
        }

        // 轮询运行状态，退出后通知 Playnite。
        private async Task WatchProcessAsync(HiddenEmulatorLaunchRequest request, CancellationToken cancelToken)
        {
            var watch = Stopwatch.StartNew();
            while (!cancelToken.IsCancellationRequested && IsRunning(request))
            {
                await Task.Delay(2000, cancelToken).ContinueWith(_ => { });
            }

            if (!cancelToken.IsCancellationRequested)
            {
                InvokeOnStopped(new GameStoppedEventArgs(Convert.ToUInt64(watch.Elapsed.TotalSeconds)));
            }
        }

        // 按配置判断当前游戏是否仍在运行。
        private bool IsRunning(HiddenEmulatorLaunchRequest request)
        {
            if (request.TrackingMode == TrackingMode.Default || request.TrackingMode == TrackingMode.Process)
            {
                return IsStartedProcessTreeRunning();
            }

            if (request.TrackingMode == TrackingMode.OriginalProcess)
            {
                return IsStartedProcessRunning();
            }

            if (request.TrackingMode == TrackingMode.ProcessName)
            {
                return IsProcessNameRunning(request.TrackingPath);
            }

            if (request.TrackingMode == TrackingMode.Directory)
            {
                return IsAnyProcessFromDirectoryRunning(request.TrackingPath ?? request.WorkingDirectory);
            }

            throw new NotSupportedException("不支持的进程跟踪模式。");
        }

        // 判断本次启动的根进程是否仍在运行。
        private bool IsStartedProcessRunning()
        {
            try
            {
                startedProcess.Refresh();
                return !startedProcess.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        // 判断本次启动的根进程或子进程是否仍在运行。
        private bool IsStartedProcessTreeRunning()
        {
            if (IsStartedProcessRunning())
            {
                return true;
            }

            return GetChildProcessIds(startedProcess.Id).Any(IsProcessRunning);
        }

        // 递归获取子进程 ID。
        private static IEnumerable<int> GetChildProcessIds(int parentProcessId)
        {
            using (var searcher = new ManagementObjectSearcher(
                $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId={parentProcessId}"))
            {
                foreach (var item in searcher.Get().Cast<ManagementObject>())
                {
                    var childId = Convert.ToInt32(item["ProcessId"]);
                    yield return childId;
                    foreach (var descendantId in GetChildProcessIds(childId))
                    {
                        yield return descendantId;
                    }
                }
            }
        }

        // 判断指定进程 ID 是否仍存在。
        private static bool IsProcessRunning(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    return !process.HasExited;
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        // 判断指定进程名是否存在。
        private static bool IsProcessNameRunning(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            var normalized = Path.GetFileNameWithoutExtension(processName);
            return Process.GetProcessesByName(normalized).Any();
        }

        // 判断指定目录下是否存在运行中的进程。
        private static bool IsAnyProcessFromDirectoryRunning(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            var fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var fileName = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(fileName) && fileName.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    // 无权限读取系统进程路径时跳过该进程。
                }
                finally
                {
                    process.Dispose();
                }
            }

            return false;
        }
    }
}
