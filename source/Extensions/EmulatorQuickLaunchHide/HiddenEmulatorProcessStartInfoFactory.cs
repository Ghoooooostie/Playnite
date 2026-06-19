// 文件用途：创建隐藏窗口启动所需的 ProcessStartInfo。
using System.Diagnostics;

namespace EmulatorQuickLaunchHide
{
    // 创建隐藏窗口启动所需的 ProcessStartInfo。
    public static class HiddenEmulatorProcessStartInfoFactory
    {
        // 创建隐藏窗口启动配置。
        public static ProcessStartInfo Create(HiddenEmulatorLaunchRequest request)
        {
            return new ProcessStartInfo(request.ExecutablePath)
            {
                Arguments = request.Arguments ?? string.Empty,
                WorkingDirectory = request.WorkingDirectory ?? string.Empty,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false
            };
        }
    }
}
