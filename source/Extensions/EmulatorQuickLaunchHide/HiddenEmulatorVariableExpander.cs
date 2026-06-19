// 文件用途：展开隐藏模拟器启动需要的 Playnite 变量。
using Playnite.SDK;
using Playnite.SDK.Models;
using System.IO;
using System.Linq;

namespace EmulatorQuickLaunchHide
{
    // 展开隐藏启动需要用到的 Playnite 变量。
    public static class HiddenEmulatorVariableExpander
    {
        // 展开游戏、ROM、模拟器目录和 Playnite 目录变量。
        public static string Expand(Game game, string input, string emulatorDir, string romPath, string playniteDir)
        {
            if (string.IsNullOrWhiteSpace(input) || !input.Contains("{"))
            {
                return input;
            }

            var result = input;
            result = result.Replace(ExpandableVariables.InstallationDirectory, game.InstallDirectory ?? string.Empty);
            result = result.Replace(ExpandableVariables.InstallationDirName, GetLastPathName(game.InstallDirectory));
            result = result.Replace(ExpandableVariables.ImagePath, romPath ?? string.Empty);
            result = result.Replace(ExpandableVariables.ImageNameNoExtension, Path.GetFileNameWithoutExtension(romPath ?? string.Empty));
            result = result.Replace(ExpandableVariables.ImageName, Path.GetFileName(romPath ?? string.Empty));
            result = result.Replace(ExpandableVariables.ImageDir, Path.GetDirectoryName(romPath ?? string.Empty) ?? string.Empty);
            result = result.Replace(ExpandableVariables.PlayniteDirectory, playniteDir ?? string.Empty);
            result = result.Replace(ExpandableVariables.Name, game.Name ?? string.Empty);
            result = result.Replace(ExpandableVariables.GameId, game.GameId ?? string.Empty);
            result = result.Replace(ExpandableVariables.DatabaseId, game.Id.ToString());
            result = result.Replace(ExpandableVariables.PluginId, game.PluginId.ToString());
            result = result.Replace(ExpandableVariables.Version, game.Version ?? string.Empty);
            result = result.Replace(ExpandableVariables.EmulatorDirectory, emulatorDir ?? string.Empty);

            var platform = game.Platforms?.FirstOrDefault()?.Name ?? string.Empty;
            result = result.Replace(ExpandableVariables.Platform, platform);
            return result;
        }

        // 返回路径最后一级名称。
        private static string GetLastPathName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .LastOrDefault() ?? string.Empty;
        }
    }
}
