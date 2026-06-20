// 文件用途：定义待确认导入项和跳过项的数据结构。
using Playnite.SDK;
using System;
using System.Collections.Generic;

namespace SwitchSmartImport
{
    // 元数据来源设置。
    public enum SwitchMetadataSource
    {
        None,
        SwitchLocalMetadata
    }

    // 单条待导入候选。
    public class SwitchImportCandidate : ObservableObject
    {
        private string gameName;
        private string basePath;
        private string highestPatchVersion;
        private bool import = true;
        private Guid selectedPlatformId;

        public string GameName
        {
            get => gameName;
            set => SetValue(ref gameName, value);
        }

        public string BasePath
        {
            get => basePath;
            set => SetValue(ref basePath, value);
        }

        public string HighestPatchVersion
        {
            get => highestPatchVersion;
            set => SetValue(ref highestPatchVersion, value);
        }

        public bool Import
        {
            get => import;
            set => SetValue(ref import, value);
        }

        public Guid SelectedPlatformId
        {
            get => selectedPlatformId;
            set => SetValue(ref selectedPlatformId, value);
        }
    }

    // 被跳过项及原因。
    public class SwitchSkippedItem
    {
        public string Path { get; set; }
        public string Reason { get; set; }
    }

    // 待确认缓存模型。
    public class SwitchPendingImportSnapshot
    {
        public DateTime SavedAt { get; set; }
        public List<SwitchImportCandidate> Candidates { get; set; } = new List<SwitchImportCandidate>();
        public List<SwitchSkippedItem> SkippedItems { get; set; } = new List<SwitchSkippedItem>();
    }
}
