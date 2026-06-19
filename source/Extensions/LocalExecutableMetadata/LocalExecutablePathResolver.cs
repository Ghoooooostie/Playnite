using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LocalExecutableMetadata
{
    // 从 Playnite 游戏对象中找出本地 Windows 可执行文件路径。
    public static class LocalExecutablePathResolver
    {
        public static IEnumerable<string> Resolve(Game game)
        {
            if (game == null)
            {
                yield break;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in ResolveFromActions(game))
            {
                if (seen.Add(path))
                {
                    yield return path;
                }
            }

            foreach (var path in ResolveFromInstallDirectory(game))
            {
                if (seen.Add(path))
                {
                    yield return path;
                }
            }
        }

        // 优先使用 Playnite 启动动作里的 exe。
        private static IEnumerable<string> ResolveFromActions(Game game)
        {
            if (game.GameActions == null)
            {
                yield break;
            }

            foreach (var action in game.GameActions)
            {
                if (action == null || action.Type != GameActionType.File)
                {
                    continue;
                }

                var expanded = ExpandGameVariables(game, action.Path)?.Trim().Trim('"');
                if (IsExecutableFile(expanded))
                {
                    yield return expanded;
                }
            }
        }

        // 没有启动动作时，从安装目录第一层找 exe。
        private static IEnumerable<string> ResolveFromInstallDirectory(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.InstallDirectory) || !Directory.Exists(game.InstallDirectory))
            {
                yield break;
            }

            var executablePaths = Directory.GetFiles(game.InstallDirectory, "*.exe", SearchOption.TopDirectoryOnly)
                .Where(path => !IsHelperExecutable(path))
                .ToList();
            if (executablePaths.Count != 1)
            {
                yield break;
            }

            yield return executablePaths[0];
        }

        // 展开本插件需要识别的 Playnite 变量。
        private static string ExpandGameVariables(Game game, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var result = value;
            if (!string.IsNullOrWhiteSpace(game.InstallDirectory))
            {
                result = ReplaceIgnoreCase(result, ExpandableVariables.InstallationDirectory, game.InstallDirectory);
                result = ReplaceIgnoreCase(result, ExpandableVariables.InstallationDirName, new DirectoryInfo(game.InstallDirectory).Name);
            }

            return result;
        }

        private static string ReplaceIgnoreCase(string value, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(oldValue))
            {
                return value;
            }

            var builder = new StringBuilder();
            var start = 0;
            var index = value.IndexOf(oldValue, start, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                builder.Append(value, start, index - start);
                builder.Append(newValue ?? string.Empty);
                start = index + oldValue.Length;
                index = value.IndexOf(oldValue, start, StringComparison.OrdinalIgnoreCase);
            }

            builder.Append(value, start, value.Length - start);
            return builder.ToString();
        }

        private static bool IsExecutableFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase)
                && File.Exists(path);
        }

        private static bool IsHelperExecutable(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            return name.IndexOf("crash", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("unins", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("redist", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
