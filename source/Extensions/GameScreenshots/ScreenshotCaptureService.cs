// 文件用途：截取当前虚拟屏幕并输出 PNG 字节。
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace GameScreenshots
{
    // 使用 Windows 屏幕复制 API 截取所有显示器区域。
    public class ScreenshotCaptureService : IScreenshotCaptureService
    {
        public byte[] CapturePng()
        {
            var bounds = SystemInformation.VirtualScreen;
            using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                }

                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }
    }
}
