// 文件用途：显示和关闭模拟器启动期间的全屏遮罩窗口。
using Playnite.SDK;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace EmulatorQuickLaunchHide
{
    // 提供遮罩窗口的最小操作。
    public interface IStartupOverlay
    {
        void Show();
        void Close();
    }

    // 创建覆盖当前 Playnite 屏幕的黑色遮罩。
    public class StartupOverlay : IStartupOverlay
    {
        private readonly IPlayniteAPI api;
        private Window window;

        public StartupOverlay(IPlayniteAPI api)
        {
            this.api = api;
        }

        public void Show()
        {
            RunOnUiThread(() =>
            {
                Close();

                var bounds = ResolveTargetBounds();
                window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    Topmost = true,
                    Background = Brushes.Black,
                    Left = bounds.Left,
                    Top = bounds.Top,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Content = new Grid
                    {
                        Background = Brushes.Black
                    }
                };

                window.Show();
                window.Activate();
            });
        }

        public void Close()
        {
            RunOnUiThread(() =>
            {
                if (window == null)
                {
                    return;
                }

                window.Close();
                window = null;
            });
        }

        // 尽量取 Playnite 当前窗口所在屏幕。
        private Rect ResolveTargetBounds()
        {
            var appWindow = api?.Dialogs?.GetCurrentAppWindow();
            Forms.Screen screen = null;
            if (appWindow != null)
            {
                screen = Forms.Screen.FromRectangle(new System.Drawing.Rectangle(
                    (int)appWindow.Left,
                    (int)appWindow.Top,
                    Math.Max(1, (int)appWindow.Width),
                    Math.Max(1, (int)appWindow.Height)));
            }

            screen = screen ?? Forms.Screen.PrimaryScreen;
            var bounds = screen.Bounds;
            return new Rect(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        }

        // 确保 WPF 窗口操作在 UI 线程执行。
        private static void RunOnUiThread(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }
    }
}
