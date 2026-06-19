// 文件用途：提供独立侧边栏截图画廊页面。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GameScreenshots
{
    // 展示所有游戏截图。
    public class GameScreenshotsGalleryView : UserControl
    {
        public GameScreenshotsGalleryView(GameScreenshotsViewModel viewModel)
        {
            DataContext = viewModel;
            Content = BuildLayout(false);
            Unloaded += delegate { viewModel.Dispose(); };
        }

        // 创建页面布局。
        private UIElement BuildLayout(bool showCapture)
        {
            var root = new DockPanel { Margin = new Thickness(18, 58, 18, 18) };
            var toolbar = BuildToolbar(showCapture);
            DockPanel.SetDock(toolbar, Dock.Top);
            root.Children.Add(toolbar);
            root.Children.Add(BuildList());
            return root;
        }

        // 创建顶部工具条。
        private UIElement BuildToolbar(bool showCapture)
        {
            var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(actions, Dock.Right);
            actions.Children.Add(CreateButton("刷新", "RefreshCommand"));
            actions.Children.Add(CreateButton("管理", "ToggleManagementCommand", "ManagementButtonText"));
            actions.Children.Add(CreateButton("删除所选", "DeleteSelectedCommand"));
            toolbar.Children.Add(actions);

            var title = CreateText(28, FontWeights.SemiBold);
            title.SetBinding(TextBlock.TextProperty, new Binding("Title"));
            toolbar.Children.Add(title);
            return toolbar;
        }

        // 创建截图列表。
        private UIElement BuildList()
        {
            var groups = new ItemsControl
            {
                Focusable = false
            };
            groups.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ScreenshotGroups"));
            groups.ItemTemplate = BuildGameGroupTemplate();

            return new ScrollViewer
            {
                Content = groups,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        // 创建游戏分组模板。
        private DataTemplate BuildGameGroupTemplate()
        {
            var template = new DataTemplate(typeof(ScreenshotGameGroup));
            var root = new FrameworkElementFactory(typeof(StackPanel));
            root.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 22));

            var title = new FrameworkElementFactory(typeof(TextBlock));
            title.SetValue(TextBlock.FontSizeProperty, 22d);
            title.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            title.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            title.SetBinding(TextBlock.TextProperty, new Binding("GameName"));
            title.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            root.AppendChild(title);

            var count = new FrameworkElementFactory(typeof(TextBlock));
            count.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 10));
            count.SetBinding(TextBlock.TextProperty, new Binding("Count") { StringFormat = "{0} 张截图" });
            count.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            root.AppendChild(count);

            var list = new FrameworkElementFactory(typeof(ItemsControl));
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Screenshots"));
            list.SetValue(ItemsControl.ItemsPanelProperty, BuildWrapPanelTemplate());
            list.SetValue(ItemsControl.ItemTemplateProperty, BuildScreenshotTemplate());
            root.AppendChild(list);

            template.VisualTree = root;
            return template;
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

            var game = new FrameworkElementFactory(typeof(TextBlock));
            game.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 8, 0, 0));
            game.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            game.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            game.SetBinding(TextBlock.TextProperty, new Binding("GameName"));
            game.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            panel.AppendChild(game);

            var time = new FrameworkElementFactory(typeof(TextBlock));
            time.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 2, 0, 0));
            time.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
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
