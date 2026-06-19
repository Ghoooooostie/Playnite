// 文件用途：提供单个游戏的截图查看和截图按钮窗口。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GameScreenshots
{
    // 展示当前游戏截图，并允许立即截图。
    public class GameScreenshotsGameView : UserControl
    {
        public GameScreenshotsGameView(GameScreenshotsViewModel viewModel)
        {
            DataContext = viewModel;
            Content = BuildLayout();
            Unloaded += delegate { viewModel.Dispose(); };
        }

        // 创建页面布局。
        private UIElement BuildLayout()
        {
            var root = new DockPanel { Margin = new Thickness(18) };
            var toolbar = BuildToolbar();
            DockPanel.SetDock(toolbar, Dock.Top);
            root.Children.Add(toolbar);
            root.Children.Add(BuildList());
            return root;
        }

        // 创建顶部工具条。
        private UIElement BuildToolbar()
        {
            var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(actions, Dock.Right);
            actions.Children.Add(CreateButton("截图", "CaptureCommand"));
            actions.Children.Add(CreateButton("刷新", "RefreshCommand"));
            actions.Children.Add(CreateButton("管理", "ToggleManagementCommand", "ManagementButtonText"));
            actions.Children.Add(CreateButton("删除所选", "DeleteSelectedCommand"));
            toolbar.Children.Add(actions);

            var title = CreateText(24, FontWeights.SemiBold);
            title.SetBinding(TextBlock.TextProperty, new Binding("Title"));
            toolbar.Children.Add(title);
            return toolbar;
        }

        // 创建截图列表。
        private UIElement BuildList()
        {
            var list = new ItemsControl
            {
                Focusable = false
            };
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Screenshots"));
            list.ItemsPanel = BuildWrapPanelTemplate();
            list.ItemTemplate = BuildScreenshotTemplate();

            return new ScrollViewer
            {
                Content = list,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        // 创建换行缩略图容器。
        private ItemsPanelTemplate BuildWrapPanelTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(WrapPanel));
            factory.SetValue(WrapPanel.OrientationProperty, Orientation.Horizontal);
            return new ItemsPanelTemplate(factory);
        }

        // 创建截图卡片模板。
        private DataTemplate BuildScreenshotTemplate()
        {
            var template = new DataTemplate(typeof(ScreenshotItem));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(FrameworkElement.WidthProperty, 220d);
            border.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 12));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new Thickness(8));
            border.SetValue(FrameworkElement.CursorProperty, Cursors.Hand);
            border.SetResourceReference(Border.BackgroundProperty, "ControlBackgroundBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "NormalBorderBrush");
            border.AddHandler(UIElement.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OpenScreenshotFromItem));

            var panel = new FrameworkElementFactory(typeof(StackPanel));
            var check = new FrameworkElementFactory(typeof(CheckBox));
            check.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
            check.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 6));
            check.SetValue(UIElement.IsHitTestVisibleProperty, false);
            check.SetBinding(ToggleButton.IsCheckedProperty, new Binding("IsSelected") { Mode = BindingMode.TwoWay });
            check.SetBinding(UIElement.VisibilityProperty, new Binding("DataContext.IsManaging")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UserControl), 1),
                Converter = new BooleanToVisibilityConverter()
            });
            panel.AppendChild(check);

            var image = new FrameworkElementFactory(typeof(Image));
            image.SetValue(FrameworkElement.HeightProperty, 120d);
            image.SetValue(Image.StretchProperty, Stretch.UniformToFill);
            image.SetBinding(Image.SourceProperty, new Binding("ThumbnailSource"));
            panel.AppendChild(image);

            var file = new FrameworkElementFactory(typeof(TextBlock));
            file.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 8, 0, 0));
            file.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            file.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            file.SetBinding(TextBlock.TextProperty, new Binding("FileName"));
            file.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            panel.AppendChild(file);

            var time = new FrameworkElementFactory(typeof(TextBlock));
            time.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 0));
            time.SetBinding(TextBlock.TextProperty, new Binding("CapturedAtText"));
            time.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            panel.AppendChild(time);

            border.AppendChild(panel);
            template.VisualTree = border;
            return template;
        }

        // 打开被点击的截图文件。
        private void OpenScreenshotFromItem(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var item = element == null ? null : element.DataContext as ScreenshotItem;
            var viewModel = DataContext as GameScreenshotsViewModel;
            if (viewModel != null && item != null)
            {
                viewModel.OpenScreenshotCommand.Execute(item);
                e.Handled = true;
            }
        }

        // 创建按钮。
        private Button CreateButton(string text, string commandPath, string contentPath = null)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(12, 6, 12, 6),
                MinWidth = 72
            };
            button.SetBinding(Button.CommandProperty, new Binding(commandPath));
            if (!string.IsNullOrWhiteSpace(contentPath))
            {
                button.SetBinding(ContentControl.ContentProperty, new Binding(contentPath));
            }

            return button;
        }

        // 创建文本控件。
        private TextBlock CreateText(double size, FontWeight weight)
        {
            var block = new TextBlock
            {
                FontSize = size,
                FontWeight = weight,
                TextWrapping = TextWrapping.Wrap
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            return block;
        }
    }
}
