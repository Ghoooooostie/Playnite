// 文件用途：提供模拟器启动遮罩插件的 Playnite 设置界面。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EmulatorQuickLaunchHide
{
    // Playnite 插件设置界面，用代码创建，避免扩展构建依赖 XAML 生成。
    public class EmulatorQuickLaunchHideSettingsView : UserControl
    {
        public EmulatorQuickLaunchHideSettingsView()
        {
            var panel = new StackPanel { Margin = new Thickness(10) };

            panel.Children.Add(new TextBlock { Text = "遮罩保持时间（秒）" });

            var box = new TextBox
            {
                Margin = new Thickness(0, 4, 0, 6),
                Width = 120,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            box.SetBinding(
                TextBox.TextProperty,
                new Binding("Settings.OverlayHoldSeconds")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });

            panel.Children.Add(box);
            panel.Children.Add(new TextBlock
            {
                Text = "启动模拟器后，先保持遮罩几秒，再切到模拟器窗口。",
                TextWrapping = TextWrapping.Wrap
            });

            Content = panel;
        }
    }
}
