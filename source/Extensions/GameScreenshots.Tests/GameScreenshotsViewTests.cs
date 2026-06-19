// 文件用途：验证截图插件页面使用不受 Playnite 列表主题影响的缩略图容器。
using GameScreenshots;
using NUnit.Framework;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace GameScreenshots.Tests
{
    // 验证截图页面不会被 ListViewItem 主题模板吞掉卡片内容。
    [TestFixture]
    public class GameScreenshotsViewTests
    {
        [Test]
        public void Game_view_uses_scroll_viewer_with_items_control_for_screenshots()
        {
            var game = new Game("ANGEL WHISPER") { Id = Guid.Parse("9ce0851c-135e-4e23-9f75-9d119e91d796") };
            var view = new GameScreenshotsGameView(CreateViewModel(game));

            var list = BuildList(view);

            Assert.IsInstanceOf<ScrollViewer>(list);
            Assert.IsInstanceOf<ItemsControl>(((ScrollViewer)list).Content);
            Assert.IsNotInstanceOf<ListView>(((ScrollViewer)list).Content);
        }

        [Test]
        public void Gallery_view_uses_scroll_viewer_with_items_control_for_screenshots()
        {
            var view = new GameScreenshotsGalleryView(CreateViewModel(null));

            var list = BuildList(view);
            var content = ((ScrollViewer)list).Content as ItemsControl;

            Assert.IsInstanceOf<ScrollViewer>(list);
            Assert.IsNotNull(content);
            Assert.IsNotInstanceOf<ListView>(content);
            Assert.IsNotNull(content.ItemTemplate);
        }

        // 调用私有构建方法，直接锁定这次出问题的控件类型。
        private static UIElement BuildList(object view)
        {
            var method = view.GetType().GetMethod("BuildList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (UIElement)method.Invoke(view, null);
        }

        // 创建测试用视图模型。
        private static GameScreenshotsViewModel CreateViewModel(Game game)
        {
            return new GameScreenshotsViewModel(new FakeScreenshotStore(), null, null, game);
        }

        // 测试用空截图存储。
        private class FakeScreenshotStore : IScreenshotStore
        {
            public ScreenshotItem SaveScreenshot(Guid gameId, string gameName, byte[] pngBytes, DateTime capturedAt)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ScreenshotItem> LoadGameScreenshots(Guid gameId)
            {
                return new List<ScreenshotItem>();
            }

            public IEnumerable<ScreenshotItem> LoadAllScreenshots()
            {
                return new List<ScreenshotItem>();
            }
        }
    }
}
