// 文件用途：隐藏模拟器启动请求模型，供解析器和播放控制器共用。
using Playnite.SDK.Models;

namespace EmulatorQuickLaunchHide
{
    // 隐藏启动前解析出的最小进程启动信息。
    public class HiddenEmulatorLaunchRequest
    {
        public string ExecutablePath { get; set; }

        public string Arguments { get; set; }

        public string WorkingDirectory { get; set; }

        public TrackingMode TrackingMode { get; set; }

        public string TrackingPath { get; set; }
    }
}
