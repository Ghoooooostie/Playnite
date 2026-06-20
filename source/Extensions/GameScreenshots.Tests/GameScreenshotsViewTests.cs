// 文件用途：验证截图插件页面使用不受 Playnite 列表主题影响的缩略图容器。
using GameScreenshots;
using NUnit.Framework;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

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

        [Test]
        public void Game_view_toolbar_contains_management_commands()
        {
            var game = new Game("ANGEL WHISPER") { Id = Guid.Parse("9ce0851c-135e-4e23-9f75-9d119e91d796") };
            var view = new GameScreenshotsGameView(CreateViewModel(game));

            var toolbar = BuildToolbar(view);
            var buttons = FindChildren<Button>(toolbar);

            Assert.IsTrue(buttons.Exists(a => HasCommandBinding(a, "ToggleManagementCommand")));
            Assert.IsTrue(buttons.Exists(a => HasCommandBinding(a, "DeleteSelectedCommand")));
            Assert.IsTrue(buttons.Exists(a => HasCommandBinding(a, "SetBackgroundCommand")));
        }

        [Test]
        public void Gallery_view_toolbar_contains_management_commands()
        {
            var view = new GameScreenshotsGalleryView(CreateViewModel(null));

            var toolbar = BuildToolbar(view);
            var buttons = FindChildren<Button>(toolbar);

            Assert.IsTrue(buttons.Exists(a => HasCommandBinding(a, "ToggleManagementCommand")));
            Assert.IsTrue(buttons.Exists(a => HasCommandBinding(a, "DeleteSelectedCommand")));
            Assert.IsTrue(buttons.Exists(a => HasCommandBinding(a, "SetBackgroundCommand")));
        }

        [Test]
        public void Game_view_screenshot_template_contains_selection_checkbox()
        {
            var game = new Game("ANGEL WHISPER") { Id = Guid.Parse("9ce0851c-135e-4e23-9f75-9d119e91d796") };
            var view = new GameScreenshotsGameView(CreateViewModel(game));

            var template = BuildScreenshotTemplate(view);

            Assert.IsTrue(TemplateContains(template, "CheckBox"));
            Assert.IsTrue(TemplateContains(template, "IsSelected"));
        }

        [Test]
        public void Gallery_view_screenshot_template_contains_selection_checkbox()
        {
            var view = new GameScreenshotsGalleryView(CreateViewModel(null));

            var template = BuildScreenshotTemplate(view);

            Assert.IsTrue(TemplateContains(template, "CheckBox"));
            Assert.IsTrue(TemplateContains(template, "IsSelected"));
        }

        // 调用私有构建方法，直接锁定这次出问题的控件类型。
        private static UIElement BuildList(object view)
        {
            var method = view.GetType().GetMethod("BuildList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (UIElement)method.Invoke(view, null);
        }

        // 调用私有工具栏构建方法。
        private static UIElement BuildToolbar(object view)
        {
            var method = view.GetType().GetMethod("BuildToolbar", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            if (method.GetParameters().Length == 0)
            {
                return (UIElement)method.Invoke(view, null);
            }

            return (UIElement)method.Invoke(view, new object[] { false });
        }

        // 调用私有截图模板构建方法。
        private static DataTemplate BuildScreenshotTemplate(object view)
        {
            var method = view.GetType().GetMethod("BuildScreenshotTemplate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            return (DataTemplate)method.Invoke(view, null);
        }

        // 判断按钮是否绑定到指定命令。
        private static bool HasCommandBinding(Button button, string path)
        {
            var binding = BindingOperations.GetBinding(button, Button.CommandProperty);
            return binding != null && binding.Path != null && binding.Path.Path == path;
        }

        // 判断勾选框是否绑定到指定属性。
        private static bool HasIsCheckedBinding(CheckBox checkBox, string path)
        {
            var binding = BindingOperations.GetBinding(checkBox, ToggleButton.IsCheckedProperty);
            return binding != null && binding.Path != null && binding.Path.Path == path;
        }

        // 判断模板声明里是否包含指定内容。
        private static bool TemplateContains(DataTemplate template, string text)
        {
            var xaml = System.Windows.Markup.XamlWriter.Save(template);
            return xaml.Contains(text);
        }

        // 查找控件树里的指定类型子元素。
        private static List<T> FindChildren<T>(DependencyObject root) where T : DependencyObject
        {
            var results = new List<T>();
            if (root == null)
            {
                return results;
            }

            var current = root as T;
            if (current != null)
            {
                results.Add(current);
            }

            var panel = root as Panel;
            if (panel != null)
            {
                foreach (UIElement child in panel.Children)
                {
                    results.AddRange(FindChildren<T>(child));
                }
            }

            var border = root as Border;
            if (border != null)
            {
                results.AddRange(FindChildren<T>(border.Child));
            }

            var content = root as ContentControl;
            if (content != null)
            {
                results.AddRange(FindChildren<T>(content.Content as DependencyObject));
            }

            return results;
        }

        // 创建测试用视图模型。
        private static GameScreenshotsViewModel CreateViewModel(Game game)
        {
            return new GameScreenshotsViewModel(new FakeScreenshotStore(), null, null, null, game);
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

            public void DeleteScreenshots(IEnumerable<ScreenshotItem> screenshots)
            {
                throw new NotImplementedException();
            }
        }
    }
}
