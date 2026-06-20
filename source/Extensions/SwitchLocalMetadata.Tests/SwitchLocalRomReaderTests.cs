using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Models;
using SwitchLocalMetadata;

namespace SwitchLocalMetadata.Tests
{
    [TestFixture]
    public class SwitchLocalRomReaderTests
    {
        private const string SamplePath = @"H:\乙女\猛獣たちとお姫様 for Nintendo Switch\猛獣たちとお姫様 for Nintendo Switch [010035001D1B2000].xci";
        private const string SampleNspPath = @"H:\乙女\BROTHERS CONFLICT Precious Baby for Nintendo Switch\BROTHERS CONFLICT Precious Baby for Nintendo Switch [JPN][010037400DAAE000].nsp";
        private const string SampleXczPath = @"H:\乙女\【2024日文版】燃えよ！ 乙女道士 ～華遊恋語～[燃烧吧！乙女道士 ~华游恋语~] 、XCZ\燃えよ！ 乙女道士 ～華遊恋語～ v1.0.0[01001BA01EBFC000][www.yxwotome.com][XCI].xcz";
        private const string SampleNszPath = @"H:\乙女\【蔷薇】結合男子\本体\結合男子 - v1.0.0 [0100DA2019044000][game.yxwotome.com].nsz";

        [Test]
        public void TryRead_returns_control_nacp_data_and_icon_from_sample_xci()
        {
            Assert.That(File.Exists(SamplePath), Is.True, "示例 xci 文件不存在。请确认 H: 盘已挂载。 ");

            var result = SwitchLocalRomReader.TryRead(SamplePath, SwitchToolPathResolver.ResolveDefaults());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TitleId, Is.EqualTo("010035001D1B2000"));
            Assert.That(result.DisplayName, Is.EqualTo("猛獣たちとお姫様 for Nintendo Switch"));
            Assert.That(result.Publisher, Is.EqualTo("アイディアファクトリー株式会社"));
            Assert.That(result.ImageFileName, Does.EndWith(".jpg"));
            Assert.That(result.ImageBytes.Length, Is.GreaterThan(1024));
        }

        [Test]
        public void TryRead_returns_control_nacp_data_and_icon_from_sample_nsp()
        {
            Assert.That(File.Exists(SampleNspPath), Is.True, "示例 nsp 文件不存在。请确认 H: 盘已挂载。");

            var result = SwitchLocalRomReader.TryRead(SampleNspPath, SwitchToolPathResolver.ResolveDefaults());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TitleId, Is.EqualTo("010037400DAAE000"));
            Assert.That(result.ImageFileName, Does.EndWith(".jpg"));
            Assert.That(result.ImageBytes.Length, Is.GreaterThan(1024));
        }

        [Test]
        public void TryRead_returns_control_nacp_data_and_icon_from_sample_xcz()
        {
            Assert.That(File.Exists(SampleXczPath), Is.True, "示例 xcz 文件不存在。请确认 H: 盘已挂载。");

            var result = SwitchLocalRomReader.TryRead(SampleXczPath, SwitchToolPathResolver.ResolveDefaults());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TitleId, Is.EqualTo("01001BA01EBFC000"));
            Assert.That(result.ImageBytes.Length, Is.GreaterThan(1024));
        }

        [Test]
        public void TryRead_returns_control_nacp_data_and_icon_from_sample_nsz()
        {
            Assert.That(File.Exists(SampleNszPath), Is.True, "示例 nsz 文件不存在。请确认 H: 盘已挂载。");

            var result = SwitchLocalRomReader.TryRead(SampleNszPath, SwitchToolPathResolver.ResolveDefaults());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.TitleId, Is.EqualTo("0100DA2019044000"));
            Assert.That(result.ImageBytes.Length, Is.GreaterThan(1024));
        }

        [Test]
        public void Resolve_expands_rom_path_relative_to_install_directory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var romPath = Path.Combine(tempDir, "sample.xci");
                File.WriteAllBytes(romPath, new byte[] { 0 });
                var game = new Game
                {
                    InstallDirectory = tempDir,
                    Roms = new ObservableCollection<GameRom>
                    {
                        new GameRom("sample", ExpandableVariables.InstallationDirectory + "\\sample.xci")
                    }
                };

                var result = SwitchGamePathResolver.Resolve(game).ToList();

                Assert.That(result, Does.Contain(romPath));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [TestCase(".xci")]
        [TestCase(".nsp")]
        [TestCase(".xcz")]
        [TestCase(".nsz")]
        public void IsSupportedFile_accepts_switch_package_extensions(string extension)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var romPath = Path.Combine(tempDir, "sample" + extension);
                File.WriteAllBytes(romPath, new byte[] { 0 });

                Assert.That(SwitchLocalRomReader.IsSupportedFile(romPath), Is.True);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ResolveBackgroundImage_uses_local_landscape_image_near_rom()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var romPath = Path.Combine(tempDir, "sample.xci");
                var backgroundPath = Path.Combine(tempDir, "background.jpg");
                File.WriteAllBytes(romPath, new byte[] { 0 });
                File.WriteAllBytes(backgroundPath, new byte[] { 1, 2, 3 });

                var result = SwitchLocalRomReader.ResolveBackgroundImagePath(romPath);

                Assert.That(result, Is.EqualTo(backgroundPath));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void BuildSearchQueries_removes_switch_suffix_and_keeps_main_title()
        {
            var result = SwitchTitleSearchNormalizer.BuildSearchQueries("薄桜鬼 真改 風華伝 for Nintendo Switch");

            Assert.That(result, Does.Contain("薄桜鬼 真改 風華伝"));
        }
    }
}
