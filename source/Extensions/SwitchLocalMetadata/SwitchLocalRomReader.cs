using Playnite.SDK.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SwitchLocalMetadata
{
    // 读取本地 xci/nsp 文件的标题 ID、展示名、厂商和内嵌图标。
    public static class SwitchLocalRomReader
    {
        private static readonly Regex TitleIdRegex = new Regex(@"(?<![A-Fa-f0-9])01[A-Fa-f0-9]{14}(?![A-Fa-f0-9])", RegexOptions.Compiled);
        private static readonly string[] SupportedExtensions = { ".xci", ".nsp", ".xcz", ".nsz" };
        private static readonly string[] BackgroundImageNames =
        {
            "background.jpg",
            "background.png",
            "bg.jpg",
            "bg.png",
            "fanart.jpg",
            "fanart.png",
            "banner.jpg",
            "banner.png"
        };

        public static SwitchLocalRomInfo TryRead(string path, SwitchLocalMetadataSettings settings)
        {
            return new HactoolnetSwitchMetadataExtractor(settings).TryRead(path);
        }

        public static SwitchLocalRomInfo TryRead(string path)
        {
            return TryRead(path, SwitchToolPathResolver.ResolveDefaults());
        }

        // 从完整路径中查找 16 位 Switch Title ID。
        public static string ExtractTitleId(string path)
        {
            var match = TitleIdRegex.Match(path ?? string.Empty);
            return match.Success ? match.Value.ToUpperInvariant() : null;
        }

        // 清理 ROM 文件名，去掉末尾 Title ID 和扩展名。
        public static string ExtractDisplayName(string path, string titleId)
        {
            var name = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            if (!string.IsNullOrEmpty(titleId))
            {
                name = name.Replace("[" + titleId + "]", string.Empty)
                    .Replace("(" + titleId + ")", string.Empty)
                    .Replace(titleId, string.Empty);
            }

            return Regex.Replace(name, @"\s+", " ").Trim(' ', '-', '_');
        }

        // 从 ROM 同目录查找常见横版背景图文件。
        public static string ResolveBackgroundImagePath(string romPath)
        {
            if (string.IsNullOrWhiteSpace(romPath))
            {
                return null;
            }

            var romDirectory = Path.GetDirectoryName(romPath);
            if (string.IsNullOrWhiteSpace(romDirectory) || !Directory.Exists(romDirectory))
            {
                return null;
            }

            foreach (var fileName in BackgroundImageNames)
            {
                var candidate = Path.Combine(romDirectory, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static bool IsSupportedFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
                && File.Exists(path);
        }
    }
}
