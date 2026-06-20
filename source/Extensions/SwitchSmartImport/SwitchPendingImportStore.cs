// 文件用途：保存和读取 Switch 智能导入的待确认列表缓存。
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SwitchSmartImport
{
    // 待确认缓存存储接口。
    public interface ISwitchPendingImportStore
    {
        void Save(List<SwitchImportCandidate> candidates, DateTime savedAt, List<SwitchSkippedItem> skippedItems = null);

        SwitchPendingImportSnapshot Load();
    }

    // 待确认缓存持久化服务。
    public class SwitchPendingImportStore : ISwitchPendingImportStore
    {
        private readonly string storePath;

        public SwitchPendingImportStore(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("rootPath");
            }

            storePath = Path.Combine(rootPath, "pending-imports.json");
        }

        public void Save(List<SwitchImportCandidate> candidates, DateTime savedAt, List<SwitchSkippedItem> skippedItems = null)
        {
            var snapshot = new SwitchPendingImportSnapshot
            {
                SavedAt = savedAt,
                Candidates = candidates ?? new List<SwitchImportCandidate>(),
                SkippedItems = skippedItems ?? new List<SwitchSkippedItem>()
            };

            var dir = Path.GetDirectoryName(storePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var stream = new FileStream(storePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var serializer = new DataContractJsonSerializer(typeof(SwitchPendingImportSnapshot));
                serializer.WriteObject(stream, snapshot);
            }
        }

        public SwitchPendingImportSnapshot Load()
        {
            if (!File.Exists(storePath))
            {
                return new SwitchPendingImportSnapshot();
            }

            using (var stream = new FileStream(storePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length == 0)
                {
                    return new SwitchPendingImportSnapshot();
                }

                var serializer = new DataContractJsonSerializer(typeof(SwitchPendingImportSnapshot));
                return serializer.ReadObject(stream) as SwitchPendingImportSnapshot ?? new SwitchPendingImportSnapshot();
            }
        }
    }
}
