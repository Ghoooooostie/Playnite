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
    }
}
