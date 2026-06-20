// 文件用途：验证 Switch 包分类器能识别本体、补丁、DLC 和版本信息。
using NUnit.Framework;

namespace SwitchSmartImport.Tests
{
    // 验证真实命名样例的分类结果。
    [TestFixture]
    public class SwitchPackageClassifierTests
    {
        [Test]
        public void Classifier_recognizes_base_and_update_in_same_directory()
        {
            var baseInfo = SwitchPackageClassifier.Classify(@"H:\乙女\PanicPalette [010063C0212BE000][v0][Base].nsp");
            var updateInfo = SwitchPackageClassifier.Classify(@"H:\乙女\PanicPalette [010063C0212BE800][v65536][Update].nsp");

            Assert.AreEqual(SwitchPackageType.Base, baseInfo.PackageType);
            Assert.AreEqual(SwitchPackageType.Update, updateInfo.PackageType);
            Assert.AreEqual("010063C0212BE000", baseInfo.TitleId);
            Assert.AreEqual("010063C0212BE800", updateInfo.TitleId);
            Assert.AreEqual("v65536", updateInfo.Version);
        }

        [Test]
        public void Classifier_recognizes_dlc_from_file_name()
        {
            var info = SwitchPackageClassifier.Classify(@"H:\乙女\結合男子 [0100DA2019045001][DLC 1].nsp");

            Assert.AreEqual(SwitchPackageType.Dlc, info.PackageType);
            Assert.AreEqual("結合男子", info.DisplayName);
        }

        [Test]
        public void Classifier_extracts_normalized_base_title_id_for_update_package()
        {
            var info = SwitchPackageClassifier.Classify(@"H:\乙女\TheRedBellsLamentv1.0.2[01006660233C6800][www.yxwotome.com][UPD].nsp");

            Assert.AreEqual("01006660233C6800", info.TitleId);
            Assert.AreEqual("01006660233C6000", info.BaseTitleId);
        }

        [Test]
        public void Classifier_treats_add_on_package_as_dlc()
        {
            var info = SwitchPackageClassifier.Classify(@"H:\乙女\Taisho x Alice All In One [English Text Mode Add-On] [010096000CA39001][game.yxwotome.com].nsp");

            Assert.AreEqual(SwitchPackageType.Dlc, info.PackageType);
            Assert.AreEqual("010096000CA38000", info.BaseTitleId);
        }

        [Test]
        public void Classifier_treats_patch_file_without_title_id_as_update()
        {
            var info = SwitchPackageClassifier.Classify(@"H:\乙女\【日文版】オランピアソワレ\オランピアソワレ[JPN] [1.0.1].nsz");

            Assert.AreEqual(SwitchPackageType.Update, info.PackageType);
        }

        [Test]
        public void Classifier_treats_version_only_file_as_update()
        {
            var info = SwitchPackageClassifier.Classify(@"H:\乙女\【日文版】オランピアソワレ\1.0.2.nsz");

            Assert.AreEqual(SwitchPackageType.Update, info.PackageType);
        }
    }
}
