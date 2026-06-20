// 文件用途：验证 Switch 候选归并器只保留一个本体并记录最高补丁。
using NUnit.Framework;

namespace SwitchSmartImport.Tests
{
    // 验证补丁合并和缺少本体时的跳过行为。
    [TestFixture]
    public class SwitchCandidateMergerTests
    {
        [Test]
        public void Merger_keeps_single_candidate_and_highest_patch()
        {
            var result = SwitchCandidateMerger.Merge(new[]
            {
                new SwitchPackageInfo { DisplayName = "PanicPalette", NormalizedName = "panicpalette", PackageType = SwitchPackageType.Base, TitleId = "010063C0212BE000", FilePath = @"H:\base.nsp" },
                new SwitchPackageInfo { DisplayName = "PanicPalette", NormalizedName = "panicpalette", PackageType = SwitchPackageType.Update, TitleId = "010063C0212BE800", Version = "1.0.1", VersionRank = 1001, FilePath = @"H:\u101.nsp" },
                new SwitchPackageInfo { DisplayName = "PanicPalette", NormalizedName = "panicpalette", PackageType = SwitchPackageType.Update, TitleId = "010063C0212BE800", Version = "1.0.3", VersionRank = 1003, FilePath = @"H:\u103.nsp" }
            });

            Assert.AreEqual(1, result.Candidates.Count);
            Assert.AreEqual("1.0.3", result.Candidates[0].HighestPatchVersion);
            Assert.AreEqual(1, result.SkippedItems.Count);
        }

        [Test]
        public void Merger_skips_update_when_base_is_missing()
        {
            var result = SwitchCandidateMerger.Merge(new[]
            {
                new SwitchPackageInfo { DisplayName = "PanicPalette", NormalizedName = "panicpalette", PackageType = SwitchPackageType.Update, TitleId = "010063C0212BE800", Version = "1.0.3", VersionRank = 1003, FilePath = @"H:\u103.nsp" }
            });

            Assert.AreEqual(0, result.Candidates.Count);
            Assert.AreEqual(1, result.SkippedItems.Count);
            Assert.AreEqual("缺少本体", result.SkippedItems[0].Reason);
        }

        [Test]
        public void Merger_matches_update_with_base_by_normalized_title_id()
        {
            var result = SwitchCandidateMerger.Merge(new[]
            {
                new SwitchPackageInfo { DisplayName = "The Red Bell's Lament", NormalizedName = "theredbellslament", PackageType = SwitchPackageType.Base, TitleId = "01006660233C6000", BaseTitleId = "01006660233C6000", FilePath = @"H:\本体\base.nsp" },
                new SwitchPackageInfo { DisplayName = "The Red Bell's Lament", NormalizedName = "theredbellslamentv102", PackageType = SwitchPackageType.Update, TitleId = "01006660233C6800", BaseTitleId = "01006660233C6000", Version = "1.0.2", VersionRank = 1002, FilePath = @"H:\补丁\upd.nsp" }
            });

            Assert.AreEqual(1, result.Candidates.Count);
            Assert.AreEqual("1.0.2", result.Candidates[0].HighestPatchVersion);
            Assert.AreEqual(0, result.SkippedItems.Count);
        }

        [Test]
        public void Merger_keeps_single_candidate_when_duplicate_bases_share_normalized_name()
        {
            var result = SwitchCandidateMerger.Merge(new[]
            {
                new SwitchPackageInfo { DisplayName = "Honey Vibes", NormalizedName = "honeyvibes", PackageType = SwitchPackageType.Base, FilePath = @"H:\乙女\Honey Vibes.xci" },
                new SwitchPackageInfo { DisplayName = "Honey Vibes", NormalizedName = "honeyvibes", PackageType = SwitchPackageType.Base, TitleId = "0100FB301E70A000", BaseTitleId = "0100FB301E70A000", FilePath = @"H:\乙女\备份\Honey Vibes [0100FB301E70A000].xci" }
            });

            Assert.AreEqual(1, result.Candidates.Count);
            Assert.AreEqual(@"H:\乙女\备份\Honey Vibes [0100FB301E70A000].xci", result.Candidates[0].BasePath);
        }

        [Test]
        public void Merger_prefers_non_update_directory_package_when_duplicate_bases_exist()
        {
            var result = SwitchCandidateMerger.Merge(new[]
            {
                new SwitchPackageInfo { DisplayName = "Taisho x Alice Heads and Tails", NormalizedName = "taishoxaliceheadsandtails", PackageType = SwitchPackageType.Base, FilePath = @"H:\乙女\本体\Taisho x Alice Heads and Tails [0100B1F0123B6000].nsp" },
                new SwitchPackageInfo { DisplayName = "Taisho x Alice Heads and Tails", NormalizedName = "taishoxaliceheadsandtails", PackageType = SwitchPackageType.Base, FilePath = @"H:\乙女\升级档\Taisho x Alice Heads and Tails [JPN] [1.0.1].nsp" }
            });

            Assert.AreEqual(1, result.Candidates.Count);
            Assert.AreEqual(@"H:\乙女\本体\Taisho x Alice Heads and Tails [0100B1F0123B6000].nsp", result.Candidates[0].BasePath);
        }

        [Test]
        public void Merger_keeps_single_candidate_when_file_name_and_parent_directory_point_to_same_title()
        {
            var result = SwitchCandidateMerger.Merge(new[]
            {
                SwitchPackageClassifier.Classify(@"H:\乙女\レンドフルール for Nintendo Switch [www.yxwotome.com][0100B5800C0E4000].xci"),
                SwitchPackageClassifier.Classify(@"H:\乙女\【日文版】レンドフルール for Nintendo Switch\Reine des Fleurs .nsp")
            });

            Assert.AreEqual(1, result.Candidates.Count);
        }
    }
}
