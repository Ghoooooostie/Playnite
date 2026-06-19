// 文件用途：验证游戏回顾插件入口在桌面和全屏中的可见性。
using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using Playnite.SDK.Plugins;

namespace GameActivityReview.Tests
{
    // 验证插件入口不会在全屏里打开难退出的窗口。
    [TestFixture]
    public class GameActivityReviewPluginTests
    {
        [Test]
        public void Plugin_does_not_add_fullscreen_main_menu_dialog_entry()
        {
            var plugin = (GameActivityReviewPlugin)FormatterServices.GetUninitializedObject(typeof(GameActivityReviewPlugin));

            var items = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).ToList();

            Assert.AreEqual(0, items.Count);
        }
    }
}
