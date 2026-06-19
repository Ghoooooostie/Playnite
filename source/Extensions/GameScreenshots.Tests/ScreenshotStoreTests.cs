// 文件用途：验证截图文件按游戏归档，并能汇总读取。
using GameScreenshots;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace GameScreenshots.Tests
{
    // 验证截图存储层不依赖 Playnite UI。
    [TestFixture]
    public class ScreenshotStoreTests
    {
        [Test]
        public void Store_saves_screenshot_under_game_id_directory()
        {
            var root = CreateTempDirectory();
            var store = new ScreenshotStore(root);
            var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var bytes = new byte[] { 1, 2, 3 };

            var item = store.SaveScreenshot(gameId, "星之海", bytes, new DateTime(2026, 6, 19, 12, 3, 4));

            Assert.AreEqual(gameId, item.GameId);
            Assert.AreEqual("星之海", item.GameName);
            Assert.AreEqual("20260619-120304.png", item.FileName);
            Assert.IsTrue(File.Exists(item.FilePath));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(item.FilePath));
        }

        [Test]
        public void Store_loads_game_screenshots_newest_first()
        {
            var root = CreateTempDirectory();
            var store = new ScreenshotStore(root);
            var gameId = Guid.Parse("22222222-2222-2222-2222-222222222222");

            store.SaveScreenshot(gameId, "风来之国", new byte[] { 1 }, new DateTime(2026, 6, 18, 8, 0, 0));
            store.SaveScreenshot(gameId, "风来之国", new byte[] { 2 }, new DateTime(2026, 6, 19, 8, 0, 0));

            var items = store.LoadGameScreenshots(gameId).ToList();

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual("20260619-080000.png", items[0].FileName);
            Assert.AreEqual("20260618-080000.png", items[1].FileName);
            Assert.IsTrue(items.All(a => a.GameName == "风来之国"));
        }

        [Test]
        public void Store_loads_all_screenshots_across_games()
        {
            var root = CreateTempDirectory();
            var store = new ScreenshotStore(root);
            var firstGame = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var secondGame = Guid.Parse("44444444-4444-4444-4444-444444444444");

            store.SaveScreenshot(firstGame, "伊苏", new byte[] { 1 }, new DateTime(2026, 6, 17, 8, 0, 0));
            store.SaveScreenshot(secondGame, "轨迹", new byte[] { 2 }, new DateTime(2026, 6, 19, 8, 0, 0));

            var items = store.LoadAllScreenshots().ToList();

            Assert.AreEqual(2, items.Count);
            Assert.AreEqual("轨迹", items[0].GameName);
            Assert.AreEqual("伊苏", items[1].GameName);
        }

        // 创建测试临时目录。
        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "GameScreenshotsTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
