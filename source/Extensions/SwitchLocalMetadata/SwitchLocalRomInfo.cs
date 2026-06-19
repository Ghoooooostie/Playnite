using Playnite.SDK.Models;

namespace SwitchLocalMetadata
{
    // 保存从本地 Switch ROM 里提取到的元数据。
    public sealed class SwitchLocalRomInfo
    {
        public string SourcePath { get; private set; }
        public string TitleId { get; private set; }
        public string DisplayName { get; private set; }
        public string Publisher { get; private set; }
        public string ImageFileName { get; private set; }
        public byte[] ImageBytes { get; private set; }
        public long FileSize { get; private set; }

        public SwitchLocalRomInfo(string sourcePath, string titleId, string displayName, string publisher, string imageFileName, byte[] imageBytes, long fileSize)
        {
            SourcePath = sourcePath;
            TitleId = titleId;
            DisplayName = displayName;
            Publisher = publisher;
            ImageFileName = imageFileName;
            ImageBytes = imageBytes;
            FileSize = fileSize;
        }

        // 转换为 Playnite 可接收的内存图片文件。
        public MetadataFile ToMetadataFile()
        {
            return ImageBytes == null || ImageBytes.Length == 0 ? null : new MetadataFile(ImageFileName, ImageBytes, SourcePath);
        }
    }
}
