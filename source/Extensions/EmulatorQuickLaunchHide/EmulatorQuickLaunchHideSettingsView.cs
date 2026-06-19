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

            panel.Children.Add(new TextBlock { Text = "遮罩保持秒数" });

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
                Text = "启动和结束时都会使用这个时间。",
                TextWrapping = TextWrapping.Wrap
            });

            var hotkeyEnabled = new CheckBox
            {
                Content = "启用结束快捷键",
                Margin = new Thickness(0, 12, 0, 4)
            };
            hotkeyEnabled.SetBinding(
                CheckBox.IsCheckedProperty,
                new Binding("Settings.ExitHotkeyEnabled")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
            panel.Children.Add(hotkeyEnabled);

            panel.Children.Add(new TextBlock { Text = "结束快捷键" });
            var hotkeyBox = new ExitHotkeyBox
            {
                Margin = new Thickness(0, 4, 0, 6),
                Width = 180,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            hotkeyBox.SetBinding(
                ExitHotkeyBox.HotkeyProperty,
                new Binding("ExitHotkey")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
            panel.Children.Add(hotkeyBox);

            panel.Children.Add(new TextBlock
            {
                Text = "点输入框后直接按 F5。按下后立刻遮罩，关闭模拟器并回到 Playnite。",
                TextWrapping = TextWrapping.Wrap
            });

            Content = panel;
        }
    }
}
