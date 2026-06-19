// 文件用途：提供全屏首页顶部的游戏回顾紧凑显示控件。
using Playnite.SDK;
using Playnite.SDK.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace GameActivityReview
{
    // 全屏首页顶部栏目入口，不打开额外窗口。
    public class GameActivityReviewHomeView : PluginUserControl
    {
        private readonly GameActivityReviewFullscreenState state;

        public GameActivityReviewHomeView(GameActivityReviewViewModel viewModel, GameActivityReviewFullscreenState state)
        {
            this.state = state;
            DataContext = viewModel;
            Focusable = true;
            Content = BuildLayout();
        }

        // 创建与 Recently Played 同类的顶部栏目项。
        private UIElement BuildLayout()
        {
            var toggle = new ToggleButton
            {
                Content = "Play Time",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                FocusVisualStyle = null,
                Command = state.OpenPanelCommand,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Template = CreateQuickPresetTemplate()
            };
            toggle.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsPanelOpen")
            {
                Source = state,
                Mode = BindingMode.TwoWay
            });
            toggle.KeyDown += OnButtonKeyDown;
            return toggle;
        }

        // 键盘确认键打开回顾面板。
        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space || GameActivityReviewControllerInput.IsConfirmation(e))
            {
                state.OpenPanelCommand.Execute(null);
                e.Handled = true;
            }
        }

        // 创建顶部快速筛选项同款模板。
        private static ControlTemplate CreateQuickPresetTemplate()
        {
            const string xaml =
                "<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"ToggleButton\">" +
                "<Border Background=\"{TemplateBinding Background}\" Margin=\"10,0,10,0\">" +
                "<TextBlock x:Name=\"TextContent\" Style=\"{DynamicResource TextBlockBaseStyle}\" FontFamily=\"{DynamicResource FontTitilliumWeb}\">" +
                "<Grid>" +
                "<TextBlock Text=\"{TemplateBinding Content}\" FontWeight=\"SemiBold\" Visibility=\"Hidden\"/>" +
                "<StackPanel>" +
                "<TextBlock Text=\"{TemplateBinding Content}\" HorizontalAlignment=\"Center\" Foreground=\"{TemplateBinding Foreground}\"/>" +
                "<TextBlock Text=\"&#x25CF;\" Style=\"{DynamicResource TextBlockBaseStyle}\" FontSize=\"12\" HorizontalAlignment=\"Center\" x:Name=\"SelectionBullet\" Visibility=\"Hidden\"/>" +
                "</StackPanel>" +
                "</Grid>" +
                "</TextBlock>" +
                "</Border>" +
                "<ControlTemplate.Triggers>" +
                "<Trigger Property=\"IsFocused\" Value=\"True\"><Setter Property=\"Foreground\" Value=\"{DynamicResource GlyphBrush}\"/></Trigger>" +
                "<Trigger Property=\"IsChecked\" Value=\"True\"><Setter Property=\"FontWeight\" Value=\"SemiBold\" TargetName=\"TextContent\"/><Setter Property=\"Visibility\" Value=\"Visible\" TargetName=\"SelectionBullet\"/></Trigger>" +
                "</ControlTemplate.Triggers>" +
                "</ControlTemplate>";
            return (ControlTemplate)XamlReader.Parse(xaml);
        }
    }
}
