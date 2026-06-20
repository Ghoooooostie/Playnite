// 文件用途：按文件名和路径识别 Switch 本体、补丁、DLC。
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SwitchSmartImport
{
    // Switch 包分类器。
    public static class SwitchPackageClassifier
    {
        private static readonly Regex TitleIdRegex = new Regex(@"(?<![A-Fa-f0-9])01[A-Fa-f0-9]{14}(?![A-Fa-f0-9])", RegexOptions.Compiled);
        private static readonly Regex DotVersionRegex = new Regex(@"(?<!\d)(\d+\.\d+(?:\.\d+){0,2})(?!\d)", RegexOptions.Compiled);
        private static readonly Regex RawVersionRegex = new Regex(@"(?<![A-Za-z0-9])(v\d+)(?![A-Za-z0-9])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex VersionOnlyNameRegex = new Regex(@"^v?\d+(\.\d+){1,2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly string[] SupportedExtensions = { ".nsp", ".nsz", ".xci", ".xcz" };

        // 识别单个 Switch 包。
        public static SwitchPackageInfo Classify(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path");
            }

            var extension = Path.GetExtension(path);
            if (!IsSupportedExtension(extension))
            {
                throw new NotSupportedException("不支持的 Switch 文件类型。");
            }

            var fileName = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            var titleId = ExtractTitleId(path);
            var type = DetectPackageType(path, fileName, titleId);
            var version = ExtractVersion(path, fileName);
            var displayName = ExtractDisplayName(fileName);

            return new SwitchPackageInfo
            {
                FilePath = path,
                DisplayName = displayName,
                NormalizedName = SwitchTitleAliasHelper.Normalize(displayName),
                TitleId = titleId,
                BaseTitleId = SwitchTitleAliasHelper.NormalizeBaseTitleId(titleId),
                PackageType = type,
                Version = version,
                VersionRank = ParseVersionRank(version),
                Aliases = SwitchTitleAliasHelper.ExtractAliasesFromPath(path, displayName)
            };
        }

        // 判断扩展名是否支持。
        public static bool IsSupportedExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            foreach (var item in SupportedExtensions)
            {
                if (string.Equals(item, extension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // 提取 Title ID。
        public static string ExtractTitleId(string text)
        {
            var match = TitleIdRegex.Match(text ?? string.Empty);
            return match.Success ? match.Value.ToUpperInvariant() : null;
        }

        private static SwitchPackageType DetectPackageType(string path, string fileName, string titleId)
        {
            var combined = ((path ?? string.Empty) + " " + (fileName ?? string.Empty)).ToLowerInvariant();
            var titleIdType = DetectTypeFromTitleId(titleId);
            var hasVersion = !string.IsNullOrWhiteSpace(ExtractVersion(path, fileName));

            if (combined.Contains("[dlc") || combined.Contains(@"\dlc\") || combined.Contains("追加内容") || combined.Contains("附加内容") || combined.Contains("add-on"))
            {
                return SwitchPackageType.Dlc;
            }

            if (combined.Contains("[update]") || combined.Contains("[upd]") || combined.Contains(@"\update\") || combined.Contains(@"\patch\") || combined.Contains("补丁"))
            {
                return titleIdType == SwitchPackageType.Dlc ? SwitchPackageType.Dlc : SwitchPackageType.Update;
            }

            if (combined.Contains("[base]") || combined.Contains("[app]") || combined.Contains("本体"))
            {
                return SwitchPackageType.Base;
            }

            if (LooksLikeUpdateWithoutTitleId(path, fileName, titleId, hasVersion))
            {
                return SwitchPackageType.Update;
            }

            return titleIdType;
        }

        private static string ExtractVersion(string path, string fileName)
        {
            var fileVersion = ExtractVersionToken(fileName);
            if (!string.IsNullOrWhiteSpace(fileVersion))
            {
                return fileVersion;
            }

            return ExtractVersionToken(path);
        }

        private static string ExtractVersionToken(string text)
        {
            var dotVersion = DotVersionRegex.Match(text ?? string.Empty);
            if (dotVersion.Success)
            {
                return dotVersion.Groups[1].Value;
            }

            var rawVersion = RawVersionRegex.Match(text ?? string.Empty);
            if (rawVersion.Success)
            {
                return rawVersion.Groups[1].Value.ToLowerInvariant();
            }

            return null;
        }

        private static string ExtractDisplayName(string fileName)
        {
            return SwitchTitleAliasHelper.CleanDisplayName(fileName);
        }

        private static long ParseVersionRank(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return 0;
            }

            if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
                long.TryParse(version.Substring(1), out var rawValue))
            {
                return rawValue;
            }

            if (Version.TryParse(version, out var dotVersion))
            {
                return (dotVersion.Major * 1000000L) + (dotVersion.Minor * 1000L) + Math.Max(0, dotVersion.Build);
            }

            return 0;
        }

        // 把更新/DLC 的 Title ID 归一成对应本体 ID。
        // 用 Title ID 末尾特征判断本体、补丁、DLC。
        private static SwitchPackageType DetectTypeFromTitleId(string titleId)
        {
            if (string.IsNullOrWhiteSpace(titleId) ||
                !ulong.TryParse(titleId, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var numericId))
            {
                return SwitchPackageType.Unknown;
            }

            var suffix = numericId & 0xFFFUL;
            if (suffix == 0)
            {
                return SwitchPackageType.Base;
            }

            if (suffix == 0x800)
            {
                return SwitchPackageType.Update;
            }

            return SwitchPackageType.Dlc;
        }

        // 识别没有 Title ID，但明显是补丁的文件。
        private static bool LooksLikeUpdateWithoutTitleId(string path, string fileName, string titleId, bool hasVersion)
        {
            if (!string.IsNullOrWhiteSpace(titleId))
            {
                return false;
            }

            var normalizedPath = (path ?? string.Empty).ToLowerInvariant();
            if (normalizedPath.Contains("升级档") || normalizedPath.Contains("补丁") || normalizedPath.Contains(@"\update\") || normalizedPath.Contains(@"\patch\"))
            {
                return true;
            }

            if (VersionOnlyNameRegex.IsMatch(fileName ?? string.Empty))
            {
                return true;
            }

            return hasVersion;
        }
    }
}
