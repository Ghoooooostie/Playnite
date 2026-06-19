// 文件用途：验证默认全屏首页主题暴露插件可填充区域。
using NUnit.Framework;
using System.IO;

namespace Playnite.FullscreenApp.Tests.Views
{
    [TestFixture]
    public class FullscreenMainThemeTests
    {
        [Test]
        public void Default_main_view_exposes_screenshot_plugin_region_below_game_list()
        {
            var themePath = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                @"..\..\..\..\Playnite.FullscreenApp\Themes\Fullscreen\Default\Views\Main.xaml"));
            var xaml = File.ReadAllText(themePath);

            StringAssert.Contains("GameScreenshots_FullscreenHomeScreenshots", xaml);
        }

        [Test]
        public void Default_main_view_exposes_game_activity_review_region_after_recently_played()
        {
            var themePath = Path.GetFullPath(Path.Combine(
                TestContext.CurrentContext.TestDirectory,
                @"..\..\..\..\Playnite.FullscreenApp\Themes\Fullscreen\Default\Views\Main.xaml"));
            var xaml = File.ReadAllText(themePath);

            StringAssert.Contains("GameActivityReview_FullscreenHomeReview", xaml);
            StringAssert.Contains("GameActivityReview_FullscreenReviewPanel", xaml);
        }
    }
}
