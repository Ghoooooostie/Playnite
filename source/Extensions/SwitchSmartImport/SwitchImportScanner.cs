// 文件用途：按设置扫描多个目录，收集并归并 Switch 待导入候选。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SwitchSmartImport
{
    // Switch 扫描器接口。
    public interface ISwitchImportScanner
    {
        SwitchCandidateMergeResult Scan();
    }

    // Switch 目录扫描器。
    public class SwitchImportScanner : ISwitchImportScanner
    {
        private readonly SwitchSmartImportSettings settings;

        public SwitchImportScanner(SwitchSmartImportSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException("settings");
        }

        // 扫描设置中的启用目录。
        public SwitchCandidateMergeResult Scan()
        {
            var packages = new List<SwitchPackageInfo>();
            foreach (var path in settings.ScanPaths?.Where(a => a != null && a.Enabled && !string.IsNullOrWhiteSpace(a.Path)).OrderBy(a => a.Priority) ?? Enumerable.Empty<SwitchScanPathConfig>())
            {
                if (!Directory.Exists(path.Path))
                {
                    continue;
                }

                var option = settings.IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var file in Directory.EnumerateFiles(path.Path, "*", option))
                {
                    if (!SwitchPackageClassifier.IsSupportedExtension(Path.GetExtension(file)))
                    {
                        continue;
                    }

                    packages.Add(SwitchPackageClassifier.Classify(file));
                }
            }

            return SwitchCandidateMerger.Merge(packages);
        }
    }
}
