// 文件用途：隐藏模拟器播放动作模型，连接动作构建器和播放控制器。
using Playnite.SDK.Models;

namespace EmulatorQuickLaunchHide
{
    // 描述一个插件生成的隐藏模拟器播放动作。
    public class HiddenEmulatorLaunchAction
    {
        public string Name { get; set; }

        public Game Game { get; set; }

        public GameAction SourceAction { get; set; }

        public Emulator Emulator { get; set; }

        public EmulatorProfile Profile { get; set; }

        public EmulatorDefinitionProfile EmulatorDefinition { get; set; }

        public string RomPath { get; set; }
    }
}
