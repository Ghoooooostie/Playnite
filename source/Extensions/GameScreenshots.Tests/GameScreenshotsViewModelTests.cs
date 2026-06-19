// 文件用途：验证截图页面数据会随新截图自动更新。
using GameScreenshots;
using NUnit.Framework;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameScreenshots.Tests
{
    // 验证打开的截图页面不会停留在旧列表。
    [TestFixture]
    public class GameScreenshotsViewModelTests
    {
        [Test]
        public void Game_view_refreshes_when_matching_game_screenshot_is_saved()
        {
            var game = new Game("Dave the Diver") { Id = Guid.Parse("c884ec6e-4ae5-4083-af3f-6da1de5aafb5") };
            var store = new FakeScreenshotStore();
            var capture = new FakeGameScreenshotService();
            var viewModel = new GameScreenshotsViewModel(store, capture, null, game);

            store.Items.Add(new ScreenshotItem
            {
                GameId = game.Id,
                GameName = game.Name,
                FileName = "20260619-202421.png",
                FilePath = "20260619-202421.png",
                CapturedAt = new DateTime(2026, 6, 19, 20, 24, 21)
            });
            capture.NotifySaved(store.Items[0]);

            Assert.AreEqual(1, viewModel.Screenshots.Count);
            Assert.AreEqual("20260619-202421.png", viewModel.Screenshots[0].FileName);
        }

        [Test]
        public void Gallery_view_refreshes_when_any_screenshot_is_saved()
        {
            var store = new FakeScreenshotStore();
            var capture = new FakeGameScreenshotService();
            var viewModel = new GameScreenshotsViewModel(store, capture, null, null);

            var item = new ScreenshotItem
            {
                GameId = Guid.Parse("aaa011cb-0072-43fc-bf0a-f517a4c8ed21"),
                GameName = "hr-arywa",
                FileName = "20260619-202129.png",
                FilePath = "20260619-202129.png",
                CapturedAt = new DateTime(2026, 6, 19, 20, 21, 29)
            };
            store.Items.Add(item);
            capture.NotifySaved(item);

            Assert.AreEqual(1, viewModel.Screenshots.Count);
            Assert.AreEqual("hr-arywa", viewModel.Screenshots[0].GameName);
        }

        [Test]
        public void Gallery_view_groups_screenshots_by_game_newest_group_first()
        {
            var store = new FakeScreenshotStore();
            store.Items.Add(new ScreenshotItem
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                GameName = "Dave the Diver",
                FileName = "20260619-202421.png",
                FilePath = "20260619-202421.png",
                CapturedAt = new DateTime(2026, 6, 19, 20, 24, 21)
            });
            store.Items.Add(new ScreenshotItem
            {
                GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                GameName = "ANGEL WHISPER",
                FileName = "20260619-233006.png",
                FilePath = "20260619-233006.png",
                CapturedAt = new DateTime(2026, 6, 19, 23, 30, 6)
            });
            store.Items.Add(new ScreenshotItem
            {
                GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                GameName = "Dave the Diver",
                FileName = "20260619-202419.png",
                FilePath = "20260619-202419.png",
                CapturedAt = new DateTime(2026, 6, 19, 20, 24, 19)
            });

            var viewModel = new GameScreenshotsViewModel(store, null, null, null);

            Assert.AreEqual(2, viewModel.ScreenshotGroups.Count);
            Assert.AreEqual("ANGEL WHISPER", viewModel.ScreenshotGroups[0].GameName);
            Assert.AreEqual(1, viewModel.ScreenshotGroups[0].Screenshots.Count);
            Assert.AreEqual("Dave the Diver", viewModel.ScreenshotGroups[1].GameName);
            Assert.AreEqual(2, viewModel.ScreenshotGroups[1].Screenshots.Count);
            Assert.AreEqual("20260619-202421.png", viewModel.ScreenshotGroups[1].Screenshots[0].FileName);
        }

        [Test]
        public void Disposed_view_does_not_refresh_after_screenshot_is_saved()
        {
            var store = new FakeScreenshotStore();
            var capture = new FakeGameScreenshotService();
            var viewModel = new GameScreenshotsViewModel(store, capture, null, null);

            viewModel.Dispose();
            store.Items.Add(new ScreenshotItem
            {
                GameId = Guid.Parse("aaa011cb-0072-43fc-bf0a-f517a4c8ed21"),
                GameName = "hr-arywa",
                FileName = "20260619-202129.png",
                FilePath = "20260619-202129.png",
                CapturedAt = new DateTime(2026, 6, 19, 20, 21, 29)
            });
            capture.NotifySaved(store.Items[0]);

            Assert.AreEqual(0, viewModel.Screenshots.Count);
        }

        // 测试用截图存储。
        private class FakeScreenshotStore : IScreenshotStore
        {
            public List<ScreenshotItem> Items { get; private set; }

            public FakeScreenshotStore()
            {
                Items = new List<ScreenshotItem>();
            }

            public ScreenshotItem SaveScreenshot(Guid gameId, string gameName, byte[] pngBytes, DateTime capturedAt)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ScreenshotItem> LoadGameScreenshots(Guid gameId)
            {
                return Items.Where(a => a.GameId == gameId);
            }

            public IEnumerable<ScreenshotItem> LoadAllScreenshots()
            {
                return Items;
            }
        }

        // 测试用截图服务。
        private class FakeGameScreenshotService : IGameScreenshotService
        {
            public event EventHandler<ScreenshotCapturedEventArgs> ScreenshotCaptured;

            public ScreenshotItem CaptureGame(Game game, DateTime capturedAt)
            {
                throw new NotImplementedException();
            }

            public void NotifySaved(ScreenshotItem item)
            {
                if (ScreenshotCaptured != null)
                {
                    ScreenshotCaptured(this, new ScreenshotCapturedEventArgs(item));
                }
            }
        }
    }
}
