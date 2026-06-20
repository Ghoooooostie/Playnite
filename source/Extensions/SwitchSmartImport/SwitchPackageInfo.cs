// 文件用途：定义 Switch 包分类后的结构和归并结果。
using System.Collections.Generic;

namespace SwitchSmartImport
{
    // Switch 包类型。
    public enum SwitchPackageType
    {
        Unknown,
        Base,
        Update,
        Dlc
    }

    // 单个 Switch 文件的识别结果。
    public class SwitchPackageInfo
    {
        public string FilePath { get; set; }

        public string DisplayName { get; set; }

        public string NormalizedName { get; set; }

        public string TitleId { get; set; }

        public string BaseTitleId { get; set; }

        public SwitchPackageType PackageType { get; set; }

        public string Version { get; set; }

        public long VersionRank { get; set; }

        public IReadOnlyCollection<string> Aliases { get; set; }
    }

    // 归并后的候选结果。
    public class SwitchCandidateMergeResult
    {
        public List<SwitchImportCandidate> Candidates { get; set; } = new List<SwitchImportCandidate>();

        public List<SwitchSkippedItem> SkippedItems { get; set; } = new List<SwitchSkippedItem>();
    }
}
