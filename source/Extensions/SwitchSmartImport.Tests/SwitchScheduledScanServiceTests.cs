// 文件用途：验证定时扫描只更新待确认缓存，不触发其它流程。
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwitchSmartImport.Tests
{
    // 验证定时扫描行为边界。
    [TestFixture]
    public class SwitchScheduledScanServiceTests
    {
        [Test]
        public void Scheduled_scan_only_updates_pending_store()
        {
            var scanner = new FakeSwitchImportScanner(new SwitchCandidateMergeResult
            {
                Candidates = new List<SwitchImportCandidate>
                {
                    new SwitchImportCandidate { GameName = "测试游戏", BasePath = @"H:\乙女\测试游戏\base.nsp" }
                }
            });
            var store = new FakeSwitchPendingImportStore();
            var service = new SwitchScheduledScanService(scanner, store);

            service.RunOnce();

            Assert.AreEqual(1, scanner.ScanCount);
            Assert.AreEqual(1, store.SaveCount);
            Assert.AreEqual(1, store.LastCandidates.Count);
        }

        private class FakeSwitchImportScanner : ISwitchImportScanner
        {
            private readonly SwitchCandidateMergeResult result;

            public int ScanCount { get; private set; }

            public FakeSwitchImportScanner(SwitchCandidateMergeResult result)
            {
                this.result = result;
            }

            public SwitchCandidateMergeResult Scan()
            {
                ScanCount++;
                return result;
            }
        }

        private class FakeSwitchPendingImportStore : ISwitchPendingImportStore
        {
            public int SaveCount { get; private set; }

            public List<SwitchImportCandidate> LastCandidates { get; private set; } = new List<SwitchImportCandidate>();

            public List<SwitchSkippedItem> LastSkippedItems { get; private set; } = new List<SwitchSkippedItem>();

            public SwitchPendingImportSnapshot Load()
            {
                return new SwitchPendingImportSnapshot();
            }

            public void Save(List<SwitchImportCandidate> candidates, DateTime savedAt, List<SwitchSkippedItem> skippedItems = null)
            {
                SaveCount++;
                LastCandidates = candidates?.ToList() ?? new List<SwitchImportCandidate>();
                LastSkippedItems = skippedItems?.ToList() ?? new List<SwitchSkippedItem>();
            }
        }
    }
}
