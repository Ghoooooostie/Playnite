using LocalExecutableMetadata;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace LocalExecutableMetadata.TestRunner
{
    // 轻量测试运行器，方便在没有 NUnit Console 时验证核心行为。
    internal static class Program
    {
        private const string SampleExePath = @"H:\game\房产达人2\House Flipper 2\HouseFlipper2.exe";
        private const string FeedTheCupsExePath = @"H:\game\Feed.the.Cups.Build.20057193\Feed the Cups.exe";

        private static int Main()
        {
            var tests = new List<Action>
            {
                TryReadReadsSampleGame,
                TryReadExtractsIcon,
                TryReadReturnsSteamCoverWhenLocalCoverIsMissing,
                ResolveFindsExecutableFromAction,
                ResolveDoesNotGuessWhenInstallDirectoryHasMultipleExecutables
            };

            var failed = 0;
            foreach (var test in tests)
            {
                try
                {
                    test();
                    Console.WriteLine("PASS " + test.Method.Name);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("FAIL " + test.Method.Name + ": " + ex.Message);
                }
            }

            Console.WriteLine("Passed: " + (tests.Count - failed) + ", Failed: " + failed);
            return failed == 0 ? 0 : 1;
        }

        // 验证能从示例 Unity 游戏读取名称、厂商和 Steam AppId。
        private static void TryReadReadsSampleGame()
        {
            Assert(File.Exists(SampleExePath), "示例 exe 文件不存在。请确认 H: 盘已挂载。");

            var result = LocalExecutableMetadataReader.TryRead(SampleExePath);

            Assert(result != null, "结果为空。");
            Assert(result.Name == "House Flipper 2", "名称错误：" + result.Name);
            Assert(result.Company == "Frozen District", "厂商错误：" + result.Company);
            Assert(result.SteamAppId == "1190970", "Steam AppId 错误：" + result.SteamAppId);
            Assert(result.InstallSize > 200000000UL, "安装大小过小：" + result.InstallSize);
        }

        // 验证 exe 图标能转成 PNG。
        private static void TryReadExtractsIcon()
        {
            Assert(File.Exists(SampleExePath), "示例 exe 文件不存在。请确认 H: 盘已挂载。");

            var result = LocalExecutableMetadataReader.TryRead(SampleExePath);

            Assert(result.IconFileName == "HouseFlipper2.png", "图标文件名错误：" + result.IconFileName);
            Assert(result.IconBytes != null && result.IconBytes.Length > 100, "图标内容为空。");
        }

        // 验证没有本地封面时使用 Steam 竖版封面。
        private static void TryReadReturnsSteamCoverWhenLocalCoverIsMissing()
        {
            Assert(File.Exists(FeedTheCupsExePath), "Feed the Cups 示例 exe 文件不存在。请确认 H: 盘已挂载。");

            var result = LocalExecutableMetadataReader.TryRead(FeedTheCupsExePath);

            Assert(result != null, "结果为空。");
            Assert(result.SteamAppId == "2336220", "Steam AppId 错误：" + result.SteamAppId);
            Assert(result.CoverImagePath == "https://cdn.cloudflare.steamstatic.com/steam/apps/2336220/library_600x900.jpg", "封面地址错误：" + result.CoverImagePath);
            Assert(result.ToCoverImageFile().Path == result.CoverImagePath, "封面元数据路径错误。");
        }

        // 验证 Playnite 启动动作路径可以解析。
        private static void ResolveFindsExecutableFromAction()
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

                Assert(result.Contains(exePath), "没有解析到 exe。");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // 验证多个 exe 时不会擅自猜主程序。
        private static void ResolveDoesNotGuessWhenInstallDirectoryHasMultipleExecutables()
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

                Assert(result.Count == 0, "多个 exe 时仍返回了路径。");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
