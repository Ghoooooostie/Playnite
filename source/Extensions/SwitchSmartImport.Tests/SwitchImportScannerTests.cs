// 文件用途：验证 Switch 扫描器只收集支持的包并完成归并。
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;

namespace SwitchSmartImport.Tests
{
    // 验证真实目录扫描的最小行为。
    [TestFixture]
    public class SwitchImportScannerTests
    {
        [Test]
        public void Scanner_ignores_non_switch_files_and_collects_switch_packages()
        {
            var root = Path.Combine(Path.GetTempPath(), "SwitchSmartImportScanTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "PanicPalette [010063C0212BE000][v0][Base].nsp"), "base");
            File.WriteAllText(Path.Combine(root, "PanicPalette [010063C0212BE800][v65536][Update].nsp"), "update");
            File.WriteAllText(Path.Combine(root, "readme.txt"), "text");
            File.WriteAllText(Path.Combine(root, "archive.rar"), "rar");

            var settings = new SwitchSmartImportSettings
            {
                IncludeSubdirectories = true,
                ScanPaths = new ObservableCollection<SwitchScanPathConfig>
                {
                    new SwitchScanPathConfig { Name = "测试目录", Path = root, Enabled = true }
                }
            };

            var result = new SwitchImportScanner(settings).Scan();

            Assert.AreEqual(1, result.Candidates.Count);
            Assert.AreEqual("PanicPalette", result.Candidates[0].GameName);
            Assert.AreEqual("v65536", result.Candidates[0].HighestPatchVersion);
            Assert.AreEqual(0, result.SkippedItems.FindAll(a => a.Path.EndsWith(".txt") || a.Path.EndsWith(".rar")).Count);
        }
    }
}
