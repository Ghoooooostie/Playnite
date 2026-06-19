using Playnite.SDK.Models;

namespace LocalExecutableMetadata
{
    // 保存从本地 Windows 游戏目录读取出的元数据。
    public class LocalExecutableGameInfo
    {
        public string Name { get; set; }

        public string Company { get; set; }

        public string SteamAppId { get; set; }

        public string ExecutablePath { get; set; }

        public string InstallDirectory { get; set; }

        public string IconFileName { get; set; }

        public byte[] IconBytes { get; set; }

        public string CoverImagePath { get; set; }

        public ulong InstallSize { get; set; }

        // 把 exe 图标转换为 Playnite 可接收的元数据文件。
        public MetadataFile ToIconFile()
        {
            return IconBytes == null || string.IsNullOrWhiteSpace(IconFileName)
                ? null
                : new MetadataFile(IconFileName, IconBytes);
        }

        // 把本地封面路径或 Steam 封面 URL 转换为 Playnite 可接收的元数据文件。
        public MetadataFile ToCoverImageFile()
        {
            return string.IsNullOrWhiteSpace(CoverImagePath)
                ? null
                : new MetadataFile(CoverImagePath);
        }
    }
}
