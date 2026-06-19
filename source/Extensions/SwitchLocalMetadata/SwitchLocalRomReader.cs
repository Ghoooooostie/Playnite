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
        private static readonly string[] SupportedExtensions = { ".xci", ".nsp" };

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

        public static bool IsSupportedFile(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)
                && File.Exists(path);
        }
    }
}
