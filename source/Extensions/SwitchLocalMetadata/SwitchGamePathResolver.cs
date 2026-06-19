using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SwitchLocalMetadata
{
    // 从 Playnite 游戏对象中找出本地 ROM 路径。
    public static class SwitchGamePathResolver
    {
        private static readonly string[] SupportedExtensions = { ".xci", ".nsp" };

        public static IEnumerable<string> Resolve(Game game)
        {
            if (game == null)
            {
                yield break;
            }

            foreach (var path in ResolveFromRoms(game))
            {
                yield return path;
            }

            var romPaths = ResolveFromRoms(game).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var path in romPaths)
            {
                yield return path;
            }

            foreach (var path in ResolveFromActions(game, romPaths))
            {
                yield return path;
            }

            if (IsSupportedFile(game.InstallDirectory))
            {
                yield return game.InstallDirectory;
            }
        }

        private static IEnumerable<string> ResolveFromRoms(Game game)
        {
            if (game.Roms == null)
            {
                yield break;
            }

            foreach (var rom in game.Roms)
            {
                var path = ExpandRomPath(game, rom?.Path);
                if (IsSupportedFile(path))
                {
                    yield return path;
                }
            }
        }

        private static IEnumerable<string> ResolveFromActions(Game game, IReadOnlyList<string> romPaths)
        {
            if (game.GameActions == null)
            {
                yield break;
            }

            foreach (var action in game.GameActions)
            {
                var candidates = new[] { action?.Path, action?.Arguments, action?.AdditionalArguments };
                foreach (var candidate in candidates)
                {
                    foreach (var expandedCandidate in ExpandActionCandidate(game, candidate, romPaths))
                    {
                        var path = ExtractExistingRomPath(expandedCandidate);
                        if (path != null)
                        {
                            yield return path;
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> ExpandActionCandidate(Game game, string candidate, IReadOnlyList<string> romPaths)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                yield break;
            }

            yield return candidate;
            if (romPaths.HasItems())
            {
                foreach (var romPath in romPaths)
                {
                    yield return ExpandGameVariables(game, candidate, romPath);
                }
            }
            else
            {
                yield return ExpandGameVariables(game, candidate, null);
            }
        }

        // 展开 Playnite 保存的 ROM 相对路径。
        private static string ExpandRomPath(Game game, string path)
        {
            var expanded = ExpandGameVariables(game, path, null)?.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(expanded))
            {
                return expanded;
            }

            if (!Path.IsPathRooted(expanded) && !string.IsNullOrWhiteSpace(game.InstallDirectory))
            {
                expanded = Path.Combine(game.InstallDirectory, expanded);
            }

            return expanded;
        }

        // 展开本插件需要识别的 Playnite 变量。
        private static string ExpandGameVariables(Game game, string value, string romPath)
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

            if (!string.IsNullOrWhiteSpace(romPath))
            {
                result = ReplaceIgnoreCase(result, ExpandableVariables.ImagePath, romPath);
                result = ReplaceIgnoreCase(result, ExpandableVariables.ImageNameNoExtension, Path.GetFileNameWithoutExtension(romPath));
                result = ReplaceIgnoreCase(result, ExpandableVariables.ImageName, Path.GetFileName(romPath));
                result = ReplaceIgnoreCase(result, ExpandableVariables.ImageDir, Path.GetDirectoryName(romPath));
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

        // 从参数字符串里取出已存在的 ROM 路径。
        private static string ExtractExistingRomPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim().Trim('"');
            if (IsSupportedFile(trimmed))
            {
                return trimmed;
            }

            foreach (var extension in SupportedExtensions)
            {
                var marker = extension;
                var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                while (index >= 0)
                {
                    var end = index + marker.Length;
                    var start = value.LastIndexOf('"', index);
                    if (start >= 0)
                    {
                        var quoted = value.Substring(start + 1, end - start - 1);
                        if (IsSupportedFile(quoted))
                        {
                            return quoted;
                        }
                    }

                    index = value.IndexOf(marker, index + marker.Length, StringComparison.OrdinalIgnoreCase);
                }
            }

            return null;
        }

        private static bool IsSupportedFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
                && File.Exists(path);
        }
    }
}
