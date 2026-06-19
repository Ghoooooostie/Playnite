// 文件用途：把截图目录设置解析成可用于保存和读取的绝对路径。
using System;
using System.IO;

namespace GameScreenshots
{
    // 统一处理默认目录和用户自定义目录。
    public static class ScreenshotPathResolver
    {
        // 解析当前应该使用的截图根目录。
        public static string Resolve(string customDirectory, string defaultDirectory)
        {
            if (string.IsNullOrWhiteSpace(defaultDirectory))
            {
                throw new ArgumentException("默认截图目录不能为空。", "defaultDirectory");
            }

            var selected = string.IsNullOrWhiteSpace(customDirectory) ? defaultDirectory : customDirectory.Trim();
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(selected));
        }
    }
}
