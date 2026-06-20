// 文件用途：统一清理 Switch 游戏标题，并从路径里提取稳定别名用于判重。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SwitchSmartImport
{
    // Switch 标题别名工具。
    internal static class SwitchTitleAliasHelper
    {
        private static readonly Regex BracketRegex = new Regex(@"\[[^\]]*\]", RegexOptions.Compiled);
        private static readonly Regex FullWidthBracketRegex = new Regex(@"【[^】]*】", RegexOptions.Compiled);
        private static readonly Regex DotVersionRegex = new Regex(@"(?<!\d)(\d+\.\d+(?:\.\d+){0,2})(?!\d)", RegexOptions.Compiled);
        private static readonly Regex RawVersionRegex = new Regex(@"(?<![A-Za-z0-9])(v\d+)(?![A-Za-z0-9])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DuplicateSuffixRegex = new Regex(@"\(\d+\)$", RegexOptions.Compiled);
        private static readonly string[] GenericSegments =
        {
            "本体",
            "补丁",
            "更新",
            "升级档",
            "dlc",
            "update",
            "patch",
            "base",
            "app",
            "nsp",
            "nsz",
            "xci",
            "xcz"
        };

        // 归一化标题，供严格字符串比对使用。
        internal static string Normalize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var cleaned = name.ToLowerInvariant();
            cleaned = cleaned.Replace("for nintendo switch", string.Empty);
            cleaned = Regex.Replace(cleaned, @"[\s\-\._]+", string.Empty);
            cleaned = Regex.Replace(cleaned, @"[^\p{L}\p{Nd}]+", string.Empty);
            return cleaned;
        }

        // 提取用于判重的多个标题别名。
        internal static IReadOnlyCollection<string> ExtractAliasesFromPath(string path, string displayName = null)
        {
            var aliases = new HashSet<string>(StringComparer.Ordinal);
            AddAlias(aliases, displayName);
            AddAlias(aliases, Path.GetFileNameWithoutExtension(path));

            var parentDirectory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                AddAlias(aliases, Path.GetFileName(parentDirectory));
            }

            return aliases.ToList();
        }

        // 清理标题文本，保留适合界面展示的名字。
        internal static string CleanDisplayName(string value)
        {
            return CleanTitleText(value);
        }

        // 判断两组别名是否命中同一标题。
        internal static bool HasCommonAlias(IEnumerable<string> left, IEnumerable<string> right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            var aliasSet = new HashSet<string>(left.Where(a => !string.IsNullOrWhiteSpace(a)), StringComparer.Ordinal);
            return right.Any(alias => !string.IsNullOrWhiteSpace(alias) && aliasSet.Contains(alias));
        }

        // 把更新/DLC 的 Title ID 归一成对应本体 ID。
        internal static string NormalizeBaseTitleId(string titleId)
        {
            if (string.IsNullOrWhiteSpace(titleId))
            {
                return null;
            }

            if (!ulong.TryParse(titleId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var numericId))
            {
                return titleId;
            }

            var normalized = numericId & 0xFFFFFFFFFFFFE000UL;
            return normalized.ToString("X16");
        }

        // 清理原始文本并加入别名集合。
        private static void AddAlias(HashSet<string> aliases, string value)
        {
            var alias = Normalize(CleanTitleText(value));
            if (ShouldIgnoreAlias(alias))
            {
                return;
            }

            aliases.Add(alias);
        }

        // 去掉标题里的杂项标签。
        private static string CleanTitleText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var cleaned = BracketRegex.Replace(value, string.Empty);
            cleaned = FullWidthBracketRegex.Replace(cleaned, string.Empty);
            cleaned = cleaned.Replace("for Nintendo Switch", string.Empty);
            cleaned = DotVersionRegex.Replace(cleaned, string.Empty);
            cleaned = RawVersionRegex.Replace(cleaned, string.Empty);
            cleaned = DuplicateSuffixRegex.Replace(cleaned, string.Empty);
            cleaned = cleaned.Replace("_", " ").Trim();
            return cleaned.Trim('-', ' ', '.');
        }

        // 过滤明显不是游戏标题的别名。
        private static bool ShouldIgnoreAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return true;
            }

            if (alias.Length < 3)
            {
                return true;
            }

            return GenericSegments.Any(segment => string.Equals(alias, Normalize(segment), StringComparison.Ordinal));
        }
    }
}
