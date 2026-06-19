// 文件用途：提供全屏首页游戏列表下方的截图紧凑显示控件。
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace GameScreenshots
{
    // 全屏首页内嵌截图区域，不打开额外窗口。
    public class GameScreenshotsHomeView : PluginUserControl
    {
        private readonly GameScreenshotsHomeViewModel viewModel;

        public GameScreenshotsHomeView(GameScreenshotsHomeViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            Focusable = false;
            Content = BuildLayout();
            Unloaded += delegate { viewModel.Dispose(); };
        }

        // 当前游戏改变时刷新截图列表。
        public override void GameContextChanged(Game oldContext, Game newContext)
        {
            viewModel.SetGame(newContext);
        }

        // 创建全屏首页下方的紧凑横向布局。
        private UIElement BuildLayout()
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(14, 10, 14, 10),
                MinHeight = 118
            };
            border.SetResourceReference(Border.BackgroundProperty, "ControlBackgroundBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "NormalBorderBrush");

            var root = new DockPanel { LastChildFill = true };
            var header = BuildHeader();
            DockPanel.SetDock(header, Dock.Left);
            root.Children.Add(header);

            root.Children.Add(BuildScreenshotList());
            border.Child = root;
            return border;
        }

        // 创建左侧标题。
        private UIElement BuildHeader()
        {
            var panel = new StackPanel
            {
                Width = 220,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0)
            };

            var title = CreateText(20, FontWeights.SemiBold);
            title.SetBinding(TextBlock.TextProperty, new Binding("Title"));
            panel.Children.Add(title);

            var count = CreateText(13, FontWeights.Normal);
            count.Margin = new Thickness(0, 5, 0, 0);
            count.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            count.SetBinding(TextBlock.TextProperty, new Binding("Screenshots.Count") { StringFormat = "{0} 张最近截图" });
            panel.Children.Add(count);
            return panel;
        }

        // 创建横向截图列表。
        private UIElement BuildScreenshotList()
        {
            var grid = new Grid();
            var list = new ItemsControl
            {
                Focusable = false
            };
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Screenshots"));
            list.ItemsPanel = BuildHorizontalItemsPanel();
            list.ItemTemplate = BuildScreenshotTemplate();
            grid.Children.Add(list);

            var empty = CreateText(16, FontWeights.Normal);
            empty.Text = "暂无截图";
            empty.VerticalAlignment = VerticalAlignment.Center;
            empty.HorizontalAlignment = HorizontalAlignment.Left;
            empty.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            empty.SetBinding(VisibilityProperty, new Binding("HasNoScreenshots")
            {
                Converter = new BooleanToVisibilityConverter()
            });
            grid.Children.Add(empty);
            return grid;
        }

        // 创建横向排列容器。
        private ItemsPanelTemplate BuildHorizontalItemsPanel()
        {
            var factory = new FrameworkElementFactory(typeof(StackPanel));
            factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            return new ItemsPanelTemplate(factory);
        }

        // 创建截图缩略图模板。
        private DataTemplate BuildScreenshotTemplate()
        {
            var template = new DataTemplate(typeof(ScreenshotItem));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(FrameworkElement.WidthProperty, 160d);
            border.SetValue(FrameworkElement.HeightProperty, 90d);
            border.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetResourceReference(Border.BorderBrushProperty, "NormalBorderBrush");

            var image = new FrameworkElementFactory(typeof(Image));
            image.SetValue(Image.StretchProperty, Stretch.UniformToFill);
            image.SetBinding(Image.SourceProperty, new Binding("ThumbnailSource"));
            image.SetBinding(FrameworkElement.ToolTipProperty, new Binding("CapturedAtText"));
            border.AppendChild(image);

            template.VisualTree = border;
            return template;
        }

        // 创建主题文本。
        private TextBlock CreateText(double size, FontWeight weight)
        {
            var block = new TextBlock
            {
                FontSize = size,
                FontWeight = weight,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            block.SetResourceReference(TextBlock.FontFamilyProperty, "FontTitilliumWeb");
            return block;
        }
    }
}
