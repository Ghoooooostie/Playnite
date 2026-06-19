using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using SwitchLocalMetadata;

namespace SwitchLocalMetadata.TestRunner
{
    internal static class Program
    {
        private const string SamplePath = @"H:\乙女\猛獣たちとお姫様 for Nintendo Switch\猛獣たちとお姫様 for Nintendo Switch [010035001D1B2000].xci";
        private const string SampleNspPath = @"H:\乙女\BROTHERS CONFLICT Precious Baby for Nintendo Switch\BROTHERS CONFLICT Precious Baby for Nintendo Switch [JPN][010037400DAAE000].nsp";

        private static int Main()
        {
            try
            {
                return Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: exception " + ex.GetType().FullName);
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static int Run()
        {
            var pathResolveResult = RunPathResolverTest();
            if (pathResolveResult != 0)
            {
                return pathResolveResult;
            }

            if (!File.Exists(SamplePath))
            {
                Console.Error.WriteLine("FAIL: sample file missing");
                return 1;
            }

            var settings = SwitchToolPathResolver.ResolveDefaults();
            var result = SwitchLocalRomReader.TryRead(SamplePath, settings);
            if (result == null)
            {
                Console.Error.WriteLine("FAIL: result is null");
                return 1;
            }

            if (result.TitleId != "010035001D1B2000")
            {
                Console.Error.WriteLine("FAIL: wrong title id " + result.TitleId);
                return 1;
            }

            if (result.DisplayName != "猛獣たちとお姫様 for Nintendo Switch")
            {
                Console.Error.WriteLine("FAIL: wrong display name " + result.DisplayName);
                return 1;
            }

            if (result.Publisher != "アイディアファクトリー株式会社")
            {
                Console.Error.WriteLine("FAIL: wrong publisher " + result.Publisher);
                return 1;
            }

            if (result.ImageBytes == null || result.ImageBytes.Length <= 1024)
            {
                Console.Error.WriteLine("FAIL: image missing");
                return 1;
            }

            var nspResult = RunNspReaderTest(settings);
            if (nspResult != 0)
            {
                return nspResult;
            }

            Console.WriteLine("PASS: " + result.TitleId + " " + result.DisplayName + " " + result.Publisher + " " + result.ImageFileName + " " + result.ImageBytes.Length);
            return 0;
        }

        private static int RunNspReaderTest(SwitchLocalMetadataSettings settings)
        {
            if (!File.Exists(SampleNspPath))
            {
                Console.Error.WriteLine("FAIL: sample NSP file missing");
                return 1;
            }

            var result = SwitchLocalRomReader.TryRead(SampleNspPath, settings);
            if (result == null)
            {
                Console.Error.WriteLine("FAIL: NSP result is null");
                return 1;
            }

            if (result.TitleId != "010037400DAAE000")
            {
                Console.Error.WriteLine("FAIL: wrong NSP title id " + result.TitleId);
                return 1;
            }

            if (result.ImageBytes == null || result.ImageBytes.Length <= 1024)
            {
                Console.Error.WriteLine("FAIL: NSP image missing");
                return 1;
            }

            return 0;
        }

        private static int RunPathResolverTest()
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

                if (!SwitchGamePathResolver.Resolve(game).Contains(romPath))
                {
                    Console.Error.WriteLine("FAIL: relative ROM path was not expanded");
                    return 1;
                }

                return 0;
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
