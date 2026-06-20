using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Playnite.SDK;
using Playnite.SDK.Models;
using SwitchLocalMetadata;

namespace SwitchLocalMetadata.TestRunner
{
    internal static class Program
    {
        private const string SamplePath = @"H:\乙女\猛獣たちとお姫様 for Nintendo Switch\猛獣たちとお姫様 for Nintendo Switch [010035001D1B2000].xci";
        private const string SampleNspPath = @"H:\乙女\BROTHERS CONFLICT Precious Baby for Nintendo Switch\BROTHERS CONFLICT Precious Baby for Nintendo Switch [JPN][010037400DAAE000].nsp";
        private const string SampleXczPath = @"H:\乙女\【2024日文版】燃えよ！ 乙女道士 ～華遊恋語～[燃烧吧！乙女道士 ~华游恋语~] 、XCZ\燃えよ！ 乙女道士 ～華遊恋語～ v1.0.0[01001BA01EBFC000][www.yxwotome.com][XCI].xcz";
        private const string SampleNszPath = @"H:\乙女\【蔷薇】結合男子\本体\結合男子 - v1.0.0 [0100DA2019044000][game.yxwotome.com].nsz";

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

            var backgroundResolveResult = RunBackgroundPathResolverTest();
            if (backgroundResolveResult != 0)
            {
                return backgroundResolveResult;
            }

            if (!ShouldSkipOnlineTests())
            {
                var onlineSearchResult = RunOnlineBackgroundSearchTest();
                if (onlineSearchResult != 0)
                {
                    return onlineSearchResult;
                }

                var onlinePreferredResult = RunOnlineBackgroundPreferredOverLocalTest();
                if (onlinePreferredResult != 0)
                {
                    return onlinePreferredResult;
                }

                var squareRejectedResult = RunSquareImageRejectedTest();
                if (squareRejectedResult != 0)
                {
                    return squareRejectedResult;
                }
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

            var xczResult = RunXczReaderTest(settings);
            if (xczResult != 0)
            {
                return xczResult;
            }

            var nszResult = RunNszReaderTest(settings);
            if (nszResult != 0)
            {
                return nszResult;
            }

            var dynamicHeaderXciResult = RunDynamicHeaderXciReaderTest(settings);
            if (dynamicHeaderXciResult != 0)
            {
                return dynamicHeaderXciResult;
            }

            Console.WriteLine("PASS: " + result.TitleId + " " + result.DisplayName + " " + result.Publisher + " " + result.ImageFileName + " " + result.ImageBytes.Length);
            return 0;
        }

        private static bool ShouldSkipOnlineTests()
        {
            return string.Equals(Environment.GetEnvironmentVariable("SWITCHLOCAL_SKIP_ONLINE"), "1", StringComparison.Ordinal);
        }

        // 验证根分区头不是固定大小时，也能算出 secure 分区数据起点。
        private static int RunDynamicHeaderXciReaderTest(SwitchLocalMetadataSettings settings)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var xciPath = Path.Combine(tempDir, "dynamic-header.xci");
                CreateSyntheticXciWithDynamicRootHeader(xciPath);
                var output = "Root Partition:" + Environment.NewLine
                    + "    Offset:                         000000000100" + Environment.NewLine
                    + "    Files:                          root:/secure                                             000000000040-000000000100";

                var fileBaseOffset = HactoolnetSwitchMetadataExtractor.CalculateXciSecureDataOffset(xciPath, output);
                if (fileBaseOffset != 0x2B0)
                {
                    Console.Error.WriteLine("FAIL: wrong dynamic header XCI data offset " + fileBaseOffset.ToString("X"));
                    return 1;
                }

                return 0;
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // 创建只包含 HFS0 头的最小 XCI，用来验证偏移计算。
        private static void CreateSyntheticXciWithDynamicRootHeader(string path)
        {
            File.WriteAllBytes(path, new byte[0x400]);
            WriteHfs0Header(path, 0x100, 1, 0x30);
            WriteHfs0Header(path, 0x1C0, 2, 0x60);
        }

        // 写入 HFS0 头部必要字段。
        private static void WriteHfs0Header(string path, long offset, uint fileCount, uint stringTableSize)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                stream.Position = offset;
                var header = new byte[16];
                Encoding.ASCII.GetBytes("HFS0").CopyTo(header, 0);
                BitConverter.GetBytes(fileCount).CopyTo(header, 4);
                BitConverter.GetBytes(stringTableSize).CopyTo(header, 8);
                stream.Write(header, 0, header.Length);
            }
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

        // 验证 ROM 同目录常见横图会被识别为背景。
        private static int RunBackgroundPathResolverTest()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            try
            {
                var romPath = Path.Combine(tempDir, "sample.xci");
                var backgroundPath = Path.Combine(tempDir, "background.jpg");
                File.WriteAllBytes(romPath, new byte[] { 0 });
                File.WriteAllBytes(backgroundPath, new byte[] { 1, 2, 3 });

                var resolvedPath = SwitchLocalRomReader.ResolveBackgroundImagePath(romPath);
                if (!string.Equals(resolvedPath, backgroundPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("FAIL: background image path was not resolved");
                    return 1;
                }

                return 0;
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // 验证搜索结果页和 og:image 页面可被解析成联网背景图。
        private static int RunOnlineBackgroundSearchTest()
        {
            var cache = new SwitchBackgroundSearchCache(Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName()));
            var downloader = new FakeDownloader();
            downloader.Set("https://www.bing.com/search?q=%E8%96%84%E6%A1%9C%E9%AC%BC%20%E7%9C%9F%E6%94%B9%20%E9%A2%A8%E8%8F%AF%E4%BC%9D%20Nintendo%20Switch",
                "<a href=\"https://www.otomate.jp/hakuoki/shinkai/switch/\" h=\"ID=SERP,1\"></a>");
            downloader.Set("https://www.otomate.jp/hakuoki/shinkai/switch/",
                "<meta property=\"og:image\" content=\"https://www.otomate.jp/hakuoki/shinkai/switch/img/ogp.jpg\">");

            var service = new SwitchBackgroundSearchService(
                new SwitchLocalMetadataSettings { EnableOnlineBackgroundSearch = true },
                cache,
                downloader);
            var romInfo = new SwitchLocalRomInfo("sample.xci", "0100TEST", "薄桜鬼 真改 風華伝 for Nintendo Switch", "IDEA FACTORY", "icon.jpg", new byte[] { 1 }, null, 1);

            var result = service.TryGetBackgroundImage(romInfo);
            if (result == null || result.Path != "https://www.otomate.jp/hakuoki/shinkai/switch/img/ogp.jpg")
            {
                Console.Error.WriteLine("FAIL: online background image was not resolved");
                return 1;
            }

            return 0;
        }

        private static int RunXczReaderTest(SwitchLocalMetadataSettings settings)
        {
            if (!File.Exists(SampleXczPath))
            {
                Console.Error.WriteLine("FAIL: sample XCZ file missing");
                return 1;
            }

            var result = SwitchLocalRomReader.TryRead(SampleXczPath, settings);
            if (result == null)
            {
                Console.Error.WriteLine("FAIL: XCZ result is null");
                return 1;
            }

            if (result.TitleId != "01001BA01EBFC000")
            {
                Console.Error.WriteLine("FAIL: wrong XCZ title id " + result.TitleId);
                return 1;
            }

            if (result.ImageBytes == null || result.ImageBytes.Length <= 1024)
            {
                Console.Error.WriteLine("FAIL: XCZ image missing");
                return 1;
            }

            return 0;
        }

        private static int RunNszReaderTest(SwitchLocalMetadataSettings settings)
        {
            if (!File.Exists(SampleNszPath))
            {
                Console.Error.WriteLine("FAIL: sample NSZ file missing");
                return 1;
            }

            var result = SwitchLocalRomReader.TryRead(SampleNszPath, settings);
            if (result == null)
            {
                Console.Error.WriteLine("FAIL: NSZ result is null");
                return 1;
            }

            if (result.TitleId != "0100DA2019044000")
            {
                Console.Error.WriteLine("FAIL: wrong NSZ title id " + result.TitleId);
                return 1;
            }

            if (result.ImageBytes == null || result.ImageBytes.Length <= 1024)
            {
                Console.Error.WriteLine("FAIL: NSZ image missing");
                return 1;
            }

            return 0;
        }

        // 验证联网命中时应优先于本地横图。
        private static int RunOnlineBackgroundPreferredOverLocalTest()
        {
            var cache = new SwitchBackgroundSearchCache(Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName()));
            var downloader = new FakeDownloader();
            downloader.Set("https://www.bing.com/search?q=%E8%96%84%E6%A1%9C%E9%AC%BC%20%E7%9C%9F%E6%94%B9%20%E9%A2%A8%E8%8F%AF%E4%BC%9D%20Nintendo%20Switch",
                "<a href=\"https://www.otomate.jp/hakuoki/shinkai/switch/\" h=\"ID=SERP,1\"></a>");
            downloader.Set("https://www.otomate.jp/hakuoki/shinkai/switch/",
                "<meta property=\"og:image\" content=\"https://www.otomate.jp/hakuoki/shinkai/switch/img/ogp.jpg\">");
            var service = new SwitchBackgroundSearchService(
                new SwitchLocalMetadataSettings { EnableOnlineBackgroundSearch = true },
                cache,
                downloader);
            var romInfo = new SwitchLocalRomInfo("sample.xci", "0100TEST2", "薄桜鬼 真改 風華伝 for Nintendo Switch", "IDEA FACTORY", "icon.jpg", new byte[] { 1 }, "D:\\temp\\background.jpg", 1);

            var remote = service.TryGetBackgroundImage(romInfo);
            if (remote == null || remote.Path != "https://www.otomate.jp/hakuoki/shinkai/switch/img/ogp.jpg")
            {
                Console.Error.WriteLine("FAIL: online image should win over local image");
                return 1;
            }

            return 0;
        }

        // 验证方图 og:image 不能当背景图。
        private static int RunSquareImageRejectedTest()
        {
            var cache = new SwitchBackgroundSearchCache(Path.Combine(Path.GetTempPath(), "SwitchLocalMetadataTests", Path.GetRandomFileName()));
            var downloader = new FakeDownloader();
            downloader.Set("https://www.bing.com/search?q=TAISHO%20x%20ALICE%20HEADS%20%26%20TAILS%20Nintendo%20Switch",
                "<a href=\"https://www.steamgriddb.com/game/5327403/heroes\" h=\"ID=SERP,1\"></a>");
            downloader.Set("https://www.steamgriddb.com/game/5327403/heroes",
                "<meta property=\"og:image\" content=\"https://cdn2.steamgriddb.com/thumb/test.jpg\"><meta property=\"og:image:width\" content=\"1024\"><meta property=\"og:image:height\" content=\"1024\">");
            var service = new SwitchBackgroundSearchService(
                new SwitchLocalMetadataSettings { EnableOnlineBackgroundSearch = true },
                cache,
                downloader);
            var romInfo = new SwitchLocalRomInfo("sample.xci", "0100TEST3", "TAISHO x ALICE HEADS & TAILS", "PROTOTYPE", "icon.jpg", new byte[] { 1 }, null, 1);

            var result = service.TryGetBackgroundImage(romInfo);
            if (result != null)
            {
                Console.Error.WriteLine("FAIL: square image should not be used as background");
                return 1;
            }

            return 0;
        }

        private sealed class FakeDownloader : IWebDownloader
        {
            private readonly System.Collections.Generic.Dictionary<string, string> responses
                = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public void Set(string url, string body)
            {
                responses[url] = body;
            }

            public string DownloadString(string url)
            {
                return responses.TryGetValue(url, out var body) ? body : string.Empty;
            }
        }
    }
}
