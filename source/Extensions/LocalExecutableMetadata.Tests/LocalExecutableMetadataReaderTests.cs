using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace LocalExecutableMetadata.Tests
{
    [TestFixture]
    public class LocalExecutableMetadataReaderTests
    {
        private const string SampleExePath = @"H:\game\房产达人2\House Flipper 2\HouseFlipper2.exe";

        [Test]
        public void TryRead_reads_unity_app_info_and_steam_app_id_from_sample_game()
        {
            Assert.That(File.Exists(SampleExePath), Is.True, "示例 exe 文件不存在。请确认 H: 盘已挂载。");

            var result = LocalExecutableMetadataReader.TryRead(SampleExePath);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("House Flipper 2"));
            Assert.That(result.Company, Is.EqualTo("Frozen District"));
            Assert.That(result.SteamAppId, Is.EqualTo("1190970"));
            Assert.That(result.ExecutablePath, Is.EqualTo(SampleExePath));
            Assert.That(result.InstallDirectory, Is.EqualTo(Path.GetDirectoryName(SampleExePath)));
            Assert.That(result.InstallSize, Is.GreaterThan(200000000UL));
        }

        [Test]
        public void TryRead_extracts_associated_exe_icon()
        {
            Assert.That(File.Exists(SampleExePath), Is.True, "示例 exe 文件不存在。请确认 H: 盘已挂载。");

            var result = LocalExecutableMetadataReader.TryRead(SampleExePath);

            Assert.That(result.IconFileName, Is.EqualTo("HouseFlipper2.png"));
            Assert.That(result.IconBytes, Is.Not.Null);
            Assert.That(result.IconBytes.Length, Is.GreaterThan(100));
        }

        [Test]
        public void Resolve_finds_executable_from_file_action_with_install_directory_variable()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "LocalExecutableMetadataTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var exePath = Path.Combine(tempDir, "SampleGame.exe");
                File.WriteAllBytes(exePath, new byte[] { 0 });
                var game = new Game
                {
                    InstallDirectory = tempDir,
                    GameActions = new ObservableCollection<GameAction>
                    {
                        new GameAction
                        {
                            Type = GameActionType.File,
                            Path = ExpandableVariables.InstallationDirectory + "\\SampleGame.exe"
                        }
                    }
                };

                var result = LocalExecutablePathResolver.Resolve(game).ToList();

                Assert.That(result, Does.Contain(exePath));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void Resolve_does_not_guess_when_install_directory_has_multiple_executables()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "LocalExecutableMetadataTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllBytes(Path.Combine(tempDir, "Game.exe"), new byte[] { 0 });
                File.WriteAllBytes(Path.Combine(tempDir, "Launcher.exe"), new byte[] { 0 });
                var game = new Game
                {
                    InstallDirectory = tempDir
                };

                var result = LocalExecutablePathResolver.Resolve(game).ToList();

                Assert.That(result, Is.Empty);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
