// 文件用途：提供截图插件的 Playnite 设置界面。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GameScreenshots
{
    // 设置界面用代码创建，避免扩展构建依赖 XAML 生成。
    public class GameScreenshotsSettingsView : UserControl
    {
        public GameScreenshotsSettingsView()
        {
            var panel = new StackPanel { Margin = new Thickness(10) };

            var enabled = new CheckBox
            {
                Content = "启用全局截图快捷键",
                Margin = new Thickness(0, 0, 0, 10)
            };
            enabled.SetBinding(CheckBox.IsCheckedProperty, new Binding("Settings.HotkeyEnabled") { Mode = BindingMode.TwoWay });
            panel.Children.Add(enabled);

            panel.Children.Add(new TextBlock { Text = "截图快捷键" });
            panel.Children.Add(CreateHotkeyBox());

            panel.Children.Add(new TextBlock { Text = "截图保存目录" });
            panel.Children.Add(CreateDirectorySelector());

            var defaultPath = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            defaultPath.SetBinding(TextBlock.TextProperty, new Binding("DefaultScreenshotDirectory") { StringFormat = "留空时使用默认目录：{0}" });
            panel.Children.Add(defaultPath);

            panel.Children.Add(new TextBlock
            {
                Text = "点击输入框后，直接按下新的快捷键。截图会归档到当前选中的游戏。",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = panel;
        }

        // 创建自动录入快捷键的输入框。
        private static TextBox CreateHotkeyBox()
        {
            var box = new TextBox
            {
                Margin = new Thickness(0, 4, 0, 10),
                Width = 220,
                HorizontalAlignment = HorizontalAlignment.Left,
                IsReadOnly = true,
                Focusable = true
            };
            box.SetBinding(TextBox.TextProperty, new Binding("HotkeyText") { Mode = BindingMode.OneWay });
            box.PreviewMouseDown += delegate
            {
                if (!box.IsKeyboardFocusWithin)
                {
                    box.Focus();
                }
            };
            box.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                var viewModel = box.DataContext as GameScreenshotsSettingsViewModel;
                if (viewModel == null)
                {
                    return;
                }

                var key = e.Key == Key.System ? e.SystemKey : e.Key;
                if (viewModel.ApplyHotkey(key, Keyboard.Modifiers))
                {
                    e.Handled = true;
                }
            };
            return box;
        }

        // 创建截图目录选择控件。
        private static UIElement CreateDirectorySelector()
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 8) };
            var button = new Button
            {
                Content = "选择",
                Width = 72,
                Margin = new Thickness(8, 0, 0, 0)
            };
            button.SetBinding(Button.CommandProperty, new Binding("SelectScreenshotDirectoryCommand"));
            DockPanel.SetDock(button, Dock.Right);
            row.Children.Add(button);

            var box = new TextBox
            {
                MinWidth = 420,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            box.SetBinding(TextBox.TextProperty, new Binding("Settings.ScreenshotDirectory")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            row.Children.Add(box);
            return row;
        }
    }
}
