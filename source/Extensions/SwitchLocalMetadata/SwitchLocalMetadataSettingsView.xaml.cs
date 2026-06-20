using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SwitchLocalMetadata
{
    // Playnite 插件设置界面，用代码创建，避免扩展构建依赖 XAML 生成。
    public class SwitchLocalMetadataSettingsView : UserControl
    {
        public SwitchLocalMetadataSettingsView()
        {
            var panel = new StackPanel { Margin = new Thickness(10) };
            AddPathInput(panel, "hactoolnet.exe", "Settings.HactoolnetPath");
            AddPathInput(panel, "prod.keys", "Settings.ProdKeysPath");
            AddPathInput(panel, "title.keys", "Settings.TitleKeysPath");
            AddCheckBox(panel, "联网搜索背景图", "Settings.EnableOnlineBackgroundSearch");
            Content = panel;
        }

        // 添加一个路径输入框。
        private static void AddPathInput(Panel panel, string label, string bindingPath)
        {
            panel.Children.Add(new TextBlock { Text = label });
            var box = new TextBox { Margin = new Thickness(0, 4, 0, 10) };
            box.SetBinding(TextBox.TextProperty, new Binding(bindingPath) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            panel.Children.Add(box);
        }

        // 添加一个开关项。
        private static void AddCheckBox(Panel panel, string label, string bindingPath)
        {
            var box = new CheckBox { Content = label, Margin = new Thickness(0, 4, 0, 10) };
            box.SetBinding(CheckBox.IsCheckedProperty, new Binding(bindingPath) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            panel.Children.Add(box);
        }
    }
}
