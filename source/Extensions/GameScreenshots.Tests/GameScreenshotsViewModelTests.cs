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
            var viewModel = new GameScreenshotsViewModel(store, capture, null, null, game);

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
            var viewModel = new GameScreenshotsViewModel(store, capture, null, null, null);

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
        public void Gallery_view_title_is_gallery()
        {
            var viewModel = new GameScreenshotsViewModel(new FakeScreenshotStore(), null, null, null, null);

            Assert.AreEqual("画廊", viewModel.Title);
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

            var viewModel = new GameScreenshotsViewModel(store, null, null, null, null);

            Assert.AreEqual(2, viewModel.ScreenshotGroups.Count);
            Assert.AreEqual("ANGEL WHISPER", viewModel.ScreenshotGroups[0].GameName);
            Assert.AreEqual(1, viewModel.ScreenshotGroups[0].Screenshots.Count);
            Assert.AreEqual("Dave the Diver", viewModel.ScreenshotGroups[1].GameName);
            Assert.AreEqual(2, viewModel.ScreenshotGroups[1].Screenshots.Count);
            Assert.AreEqual("20260619-202421.png", viewModel.ScreenshotGroups[1].Screenshots[0].FileName);
        }

        [Test]
        public void Delete_selected_screenshots_removes_files_and_refreshes_gallery_groups()
        {
            var store = new FakeScreenshotStore();
            var firstGame = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var secondGame = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var first = CreateItem(firstGame, "Dave the Diver", "20260620-100000.png", new DateTime(2026, 6, 20, 10, 0, 0));
            var second = CreateItem(firstGame, "Dave the Diver", "20260620-100100.png", new DateTime(2026, 6, 20, 10, 1, 0));
            var third = CreateItem(secondGame, "ANGEL WHISPER", "20260620-110000.png", new DateTime(2026, 6, 20, 11, 0, 0));
            store.Items.Add(first);
            store.Items.Add(second);
            store.Items.Add(third);
            var viewModel = new GameScreenshotsViewModel(store, null, null, null, null);

            viewModel.ToggleManagementCommand.Execute(null);
            viewModel.ToggleSelectionCommand.Execute(first);
            viewModel.ToggleSelectionCommand.Execute(third);
            viewModel.DeleteSelectedCommand.Execute(null);

            Assert.IsTrue(viewModel.IsManaging);
            Assert.AreEqual(1, store.Items.Count);
            Assert.AreEqual(second.FileName, store.Items[0].FileName);
            Assert.AreEqual(2, store.DeletedItems.Count);
            Assert.AreEqual(1, viewModel.Screenshots.Count);
            Assert.AreEqual(1, viewModel.ScreenshotGroups.Count);
            Assert.AreEqual("Dave the Diver", viewModel.ScreenshotGroups[0].GameName);
            Assert.AreEqual(0, viewModel.SelectedCount);
        }

        [Test]
        public void Leaving_management_clears_selected_screenshots()
        {
            var store = new FakeScreenshotStore();
            var item = CreateItem(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Dave the Diver",
                "20260620-100000.png",
                new DateTime(2026, 6, 20, 10, 0, 0));
            store.Items.Add(item);
            var viewModel = new GameScreenshotsViewModel(store, null, null, null, null);

            viewModel.ToggleManagementCommand.Execute(null);
            viewModel.ToggleSelectionCommand.Execute(item);
            viewModel.ToggleManagementCommand.Execute(null);

            Assert.IsFalse(viewModel.IsManaging);
            Assert.IsFalse(item.IsSelected);
            Assert.AreEqual(0, viewModel.SelectedCount);
        }

        [Test]
        public void Management_button_text_changes_with_management_mode()
        {
            var store = new FakeScreenshotStore();
            var viewModel = new GameScreenshotsViewModel(store, null, null, null, null);

            Assert.AreEqual("管理", viewModel.ManagementButtonText);

            viewModel.ToggleManagementCommand.Execute(null);

            Assert.AreEqual("完成", viewModel.ManagementButtonText);
        }

        [Test]
        public void Set_background_uses_selected_screenshot_and_game()
        {
            var store = new FakeScreenshotStore();
            var game = new Game("Dave the Diver") { Id = Guid.Parse("c884ec6e-4ae5-4083-af3f-6da1de5aafb5") };
            var item = CreateItem(game.Id, game.Name, "20260620-120000.png", new DateTime(2026, 6, 20, 12, 0, 0));
            store.Items.Add(item);
            var background = new FakeGameBackgroundService();
            var messages = new FakeScreenshotMessageService();
            var viewModel = new GameScreenshotsViewModel(store, null, messages, background, game);

            viewModel.ToggleManagementCommand.Execute(null);
            viewModel.ToggleSelectionCommand.Execute(item);
            viewModel.SetBackgroundCommand.Execute(null);

            Assert.AreEqual(game.Id, background.LastGame.Id);
            Assert.AreEqual(item.FilePath, background.LastImagePath);
            Assert.AreEqual(1, messages.InfoMessages.Count);
        }

        [Test]
        public void Gallery_set_background_uses_selected_screenshot_game()
        {
            var store = new FakeScreenshotStore();
            var gameId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var item = CreateItem(gameId, "Dave the Diver", "20260620-120000.png", new DateTime(2026, 6, 20, 12, 0, 0));
            store.Items.Add(item);
            var background = new FakeGameBackgroundService();
            var viewModel = new GameScreenshotsViewModel(store, null, null, background, null);

            viewModel.ToggleManagementCommand.Execute(null);
            viewModel.ToggleSelectionCommand.Execute(item);
            viewModel.SetBackgroundCommand.Execute(null);

            Assert.AreEqual(gameId, background.LastGame.Id);
            Assert.AreEqual(item.FilePath, background.LastImagePath);
            Assert.AreEqual(item.GameName, background.LastGame.Name);
        }

        [Test]
        public void Set_background_requires_single_selection()
        {
            var store = new FakeScreenshotStore();
            var item = CreateItem(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Dave the Diver",
                "20260620-120000.png",
                new DateTime(2026, 6, 20, 12, 0, 0));
            store.Items.Add(item);
            var background = new FakeGameBackgroundService();
            var viewModel = new GameScreenshotsViewModel(store, null, null, background, null);

            Assert.IsFalse(viewModel.SetBackgroundCommand.CanExecute(null));

            viewModel.ToggleManagementCommand.Execute(null);
            Assert.IsFalse(viewModel.SetBackgroundCommand.CanExecute(null));

            viewModel.ToggleSelectionCommand.Execute(item);
            Assert.IsTrue(viewModel.SetBackgroundCommand.CanExecute(null));
        }

        [Test]
        public void Disposed_view_does_not_refresh_after_screenshot_is_saved()
        {
            var store = new FakeScreenshotStore();
            var capture = new FakeGameScreenshotService();
            var viewModel = new GameScreenshotsViewModel(store, capture, null, null, null);

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

        // 创建测试截图项。
        private static ScreenshotItem CreateItem(Guid gameId, string gameName, string fileName, DateTime capturedAt)
        {
            return new ScreenshotItem
            {
                GameId = gameId,
                GameName = gameName,
                FileName = fileName,
                FilePath = fileName,
                CapturedAt = capturedAt
            };
        }

        // 测试用截图存储。
        private class FakeScreenshotStore : IScreenshotStore
        {
            public List<ScreenshotItem> Items { get; private set; }
            public List<ScreenshotItem> DeletedItems { get; private set; }

            public FakeScreenshotStore()
            {
                Items = new List<ScreenshotItem>();
                DeletedItems = new List<ScreenshotItem>();
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

            public void DeleteScreenshots(IEnumerable<ScreenshotItem> screenshots)
            {
                foreach (var item in screenshots.ToList())
                {
                    DeletedItems.Add(item);
                    Items.RemoveAll(a => a.FilePath == item.FilePath);
                }
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

        // 测试用背景设置服务。
        private class FakeGameBackgroundService : IGameBackgroundService
        {
            public Game LastGame { get; private set; }
            public string LastImagePath { get; private set; }

            public void SetBackground(Game game, string imagePath)
            {
                LastGame = game;
                LastImagePath = imagePath;
            }
        }

        // 测试用消息服务。
        private class FakeScreenshotMessageService : IScreenshotMessageService
        {
            public List<string> InfoMessages { get; private set; }
            public List<string> ErrorMessages { get; private set; }

            public FakeScreenshotMessageService()
            {
                InfoMessages = new List<string>();
                ErrorMessages = new List<string>();
            }

            public void ShowInfo(string message)
            {
                InfoMessages.Add(message);
            }

            public void ShowError(string message)
            {
                ErrorMessages.Add(message);
            }
        }
    }
}
