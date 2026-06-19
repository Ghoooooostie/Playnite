// 文件用途：验证截图保存目录设置会解析到稳定的真实目录。
using GameScreenshots;
using NUnit.Framework;
using System.IO;

namespace GameScreenshots.Tests
{
    // 验证默认目录和自定义目录的选择规则。
    [TestFixture]
    public class ScreenshotPathResolverTests
    {
        [Test]
        public void Empty_custom_directory_uses_default_screenshot_directory()
        {
            var defaultDirectory = Path.Combine(Path.GetTempPath(), "GameScreenshotsDefault");

            var resolved = ScreenshotPathResolver.Resolve(null, defaultDirectory);

            Assert.AreEqual(Path.GetFullPath(defaultDirectory), resolved);
        }

        [Test]
        public void Custom_directory_overrides_default_screenshot_directory()
        {
            var defaultDirectory = Path.Combine(Path.GetTempPath(), "GameScreenshotsDefault");
            var customDirectory = Path.Combine(Path.GetTempPath(), "GameScreenshotsCustom");

            var resolved = ScreenshotPathResolver.Resolve(customDirectory, defaultDirectory);

            Assert.AreEqual(Path.GetFullPath(customDirectory), resolved);
        }
    }
}
