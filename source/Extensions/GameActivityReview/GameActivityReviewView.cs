using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GameActivityReview
{
    // 游戏时光回顾侧边栏页面。
    public class GameActivityReviewView : UserControl
    {
        public GameActivityReviewView(GameActivityReviewViewModel viewModel)
        {
            DataContext = viewModel;
            Content = BuildLayout();
        }

        // 创建页面主体布局。
        private UIElement BuildLayout()
        {
            var root = new DockPanel { Margin = new Thickness(18, 58, 18, 18) };

            var toolbar = BuildToolbar();
            DockPanel.SetDock(toolbar, Dock.Top);
            root.Children.Add(toolbar);

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = BuildContent()
            };
            root.Children.Add(scroll);
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
            actions.Children.Add(CreateButton("刷新", "RefreshCommand"));
            actions.Children.Add(CreateButton("生成分享海报", "ExportCommand"));
            toolbar.Children.Add(actions);

            var title = CreateText("游戏时光回顾", 28, FontWeights.SemiBold);
            toolbar.Children.Add(title);
            return toolbar;
        }

        // 创建内容区。
        private UIElement BuildContent()
        {
            var panel = new StackPanel();
            panel.Children.Add(BuildIntro());
            panel.Children.Add(BuildPeriodPicker());
            panel.Children.Add(BuildStatsGrid());
            panel.Children.Add(BuildDisplayTabs());
            panel.Children.Add(BuildReviewPreview());
            return panel;
        }

        // 创建说明区。
        private UIElement BuildIntro()
        {
            var text = CreateText("根据每一次启动与退出记录统计真实游玩时长，支持全部、今天、本周、本月、今年回顾。", 14, FontWeights.Normal);
            text.Margin = new Thickness(0, 0, 0, 14);
            text.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            return text;
        }

        // 创建时间范围选择器。
        private UIElement BuildPeriodPicker()
        {
            var combo = new ComboBox
            {
                Width = 180,
                DisplayMemberPath = "Label",
                SelectedValuePath = "Period",
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 14)
            };
            combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("PeriodOptions"));
            combo.SetBinding(Selector.SelectedValueProperty, new Binding("SelectedPeriod") { Mode = BindingMode.TwoWay });
            return combo;
        }

        // 创建统计卡片网格。
        private UIElement BuildStatsGrid()
        {
            var grid = new UniformGrid
            {
                Columns = 4,
                Margin = new Thickness(0, 0, 0, 18)
            };
            grid.Children.Add(CreateStatBox("累计投入", "Summary.TotalTimeText", "当前范围"));
            grid.Children.Add(CreateStatBox("启动记录", "Summary.SessionCount", "完整启动与退出会话"));
            grid.Children.Add(CreateStatBox("上榜游戏", "Summary.GameCount", "当前游戏库范围"));
            grid.Children.Add(CreateStatBox("榜首游戏", "Summary.TopGameName", "等待记录生成"));
            return grid;
        }

        // 创建两种展示方式页签。
        private UIElement BuildDisplayTabs()
        {
            var tabs = new TabControl { Margin = new Thickness(0, 0, 0, 18) };
            tabs.Items.Add(new TabItem { Header = "榜单", Content = BuildRanking() });
            tabs.Items.Add(new TabItem { Header = "图表", Content = BuildDailyChart() });
            return tabs;
        }
        // 创建榜单区域。
        private UIElement BuildRanking()
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
            panel.Children.Add(CreateText("游戏榜单", 22, FontWeights.SemiBold));

            var scope = CreateText(string.Empty, 13, FontWeights.Normal);
            scope.Margin = new Thickness(0, 4, 0, 10);
            scope.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            scope.SetBinding(TextBlock.TextProperty, new Binding("Summary.DateRangeText") { StringFormat = "{0}" });
            panel.Children.Add(scope);

            var list = new ListView
            {
                MinHeight = 260,
                MaxHeight = 460
            };
            list.SetResourceReference(Control.BorderBrushProperty, "NormalBorderBrush");
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("TopGames"));
            list.MouseDoubleClick += delegate
            {
                if (list.SelectedItem != null)
                {
                    ((GameActivityReviewViewModel)DataContext).OpenGameCommand.Execute(list.SelectedItem);
                }
            };

            var gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn { Header = "游戏", DisplayMemberBinding = new Binding("GameName"), Width = 360 });
            gridView.Columns.Add(new GridViewColumn { Header = "时长", DisplayMemberBinding = new Binding("TimeText"), Width = 160 });
            gridView.Columns.Add(new GridViewColumn { Header = "次数", DisplayMemberBinding = new Binding("SessionCount"), Width = 80 });
            list.View = gridView;
            panel.Children.Add(list);
            return panel;
        }

        // 创建每日游玩时长图表。
        private UIElement BuildDailyChart()
        {
            var panel = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            panel.Children.Add(BuildChartOverviewCard());
            panel.Children.Add(BuildTopGameBars());
            return panel;
        }

        // 创建参考图样式的总览卡片。
        private UIElement BuildChartOverviewCard()
        {
            var border = CreatePanelBorder();
            border.Margin = new Thickness(0, 0, 0, 18);
            border.Padding = new Thickness(18);

            var panel = new StackPanel();
            panel.Children.Add(BuildChartHeader());
            panel.Children.Add(BuildChartMainNumbers());
            panel.Children.Add(BuildVerticalDailyChart());
            border.Child = panel;
            return border;
        }

        // 创建图表卡片顶部范围信息。
        private UIElement BuildChartHeader()
        {
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };

            var range = CreateText(string.Empty, 13, FontWeights.Normal);
            range.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            range.SetBinding(TextBlock.TextProperty, new Binding("Summary.DateRangeText"));
            DockPanel.SetDock(range, Dock.Right);
            header.Children.Add(range);

            var title = CreateText(string.Empty, 18, FontWeights.SemiBold);
            title.SetBinding(TextBlock.TextProperty, new Binding("Summary.PeriodTitle"));
            header.Children.Add(title);
            return header;
        }

        // 创建总时长和日均两组核心数字。
        private UIElement BuildChartMainNumbers()
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var totalPanel = new StackPanel();
            totalPanel.Children.Add(CreateSmallMutedText("总游玩时长"));
            var total = CreateText(string.Empty, 34, FontWeights.SemiBold);
            total.Margin = new Thickness(0, 4, 0, 0);
            total.TextTrimming = TextTrimming.CharacterEllipsis;
            total.SetBinding(TextBlock.TextProperty, new Binding("Summary.TotalTimeText"));
            totalPanel.Children.Add(total);
            Grid.SetColumn(totalPanel, 0);
            grid.Children.Add(totalPanel);

            var averagePanel = new StackPanel { Margin = new Thickness(18, 0, 0, 0) };
            var averageLabel = CreateSmallMutedText(string.Empty);
            averageLabel.SetBinding(TextBlock.TextProperty, new Binding("Summary.AverageLabel"));
            averagePanel.Children.Add(averageLabel);
            var average = CreateText(string.Empty, 22, FontWeights.SemiBold);
            average.Margin = new Thickness(0, 8, 0, 0);
            average.SetBinding(TextBlock.TextProperty, new Binding("Summary.AverageDailyTimeText"));
            averagePanel.Children.Add(average);
            Grid.SetColumn(averagePanel, 1);
            grid.Children.Add(averagePanel);
            return grid;
        }

        // 创建每日竖向柱状图。
        private UIElement BuildVerticalDailyChart()
        {
            var panel = new StackPanel();
            var axis = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

            var average = CreateText("平均", 12, FontWeights.Normal);
            average.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            DockPanel.SetDock(average, Dock.Left);
            axis.Children.Add(average);

            var unit = CreateText(string.Empty, 12, FontWeights.Normal);
            unit.HorizontalAlignment = HorizontalAlignment.Right;
            unit.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            unit.SetBinding(TextBlock.TextProperty, new Binding("Summary.ChartUnitLabel"));
            axis.Children.Add(unit);
            panel.Children.Add(axis);

            var chartFrame = new Border
            {
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(0, 8, 0, 4),
                MinHeight = 168
            };
            chartFrame.SetResourceReference(Border.BorderBrushProperty, "NormalBorderBrush");

            var scroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var items = new ItemsControl();
            items.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Summary.DailyItems"));
            items.ItemsPanel = BuildHorizontalItemsPanel();
            items.ItemTemplate = BuildVerticalDailyChartTemplate();
            scroll.Content = items;
            chartFrame.Child = scroll;
            panel.Children.Add(chartFrame);
            return panel;
        }

        // 创建均分铺满宽度的 ItemsPanel。
        private ItemsPanelTemplate BuildHorizontalItemsPanel()
        {
            var panelFactory = new FrameworkElementFactory(typeof(UniformGrid));
            panelFactory.SetValue(UniformGrid.RowsProperty, 1);
            return new ItemsPanelTemplate(panelFactory);
        }

        // 创建每日竖向柱模板。
        private DataTemplate BuildVerticalDailyChartTemplate()
        {
            var template = new DataTemplate(typeof(GameActivityDailyItem));
            var root = new FrameworkElementFactory(typeof(Grid));
            root.SetValue(FrameworkElement.MarginProperty, new Thickness(0));

            var chartRow = new FrameworkElementFactory(typeof(RowDefinition));
            chartRow.SetValue(RowDefinition.HeightProperty, new GridLength(132));
            root.AppendChild(chartRow);

            var labelRow = new FrameworkElementFactory(typeof(RowDefinition));
            labelRow.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            root.AppendChild(labelRow);

            var bar = new FrameworkElementFactory(typeof(ProgressBar));
            bar.SetValue(ProgressBar.MaximumProperty, 100d);
            bar.SetValue(ProgressBar.OrientationProperty, Orientation.Vertical);
            bar.SetValue(FrameworkElement.WidthProperty, 22d);
            bar.SetValue(FrameworkElement.HeightProperty, 126d);
            bar.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            bar.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom);
            bar.SetBinding(ProgressBar.ValueProperty, new Binding("Percent"));
            bar.SetBinding(FrameworkElement.ToolTipProperty, new Binding("TimeText"));
            bar.SetValue(Grid.RowProperty, 0);
            root.AppendChild(bar);

            var label = new FrameworkElementFactory(typeof(TextBlock));
            label.SetBinding(TextBlock.TextProperty, new Binding("Label"));
            label.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 8, 0, 0));
            label.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            label.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            label.SetValue(Grid.RowProperty, 1);
            root.AppendChild(label);

            template.VisualTree = root;
            return template;
        }

        // 创建常用游戏排行条区域。
        private UIElement BuildTopGameBars()
        {
            var panel = new StackPanel();
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            header.Children.Add(CreateText("常用", 22, FontWeights.SemiBold));
            panel.Children.Add(header);

            var border = CreatePanelBorder();
            border.Padding = new Thickness(16);

            var list = new ItemsControl();
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("TopGames"));
            list.ItemTemplate = BuildTopGameBarTemplate();
            border.Child = list;
            panel.Children.Add(border);
            return panel;
        }

        // 创建常用游戏横向排行条模板。
        private DataTemplate BuildTopGameBarTemplate()
        {
            var template = new DataTemplate(typeof(GameActivityRankItem));
            var row = new FrameworkElementFactory(typeof(Grid));
            row.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 18));
            row.SetValue(FrameworkElement.MinHeightProperty, 58d);

            var iconColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            iconColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(58));
            row.AppendChild(iconColumn);

            var contentColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            contentColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            row.AppendChild(contentColumn);

            var timeColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            timeColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(112));
            row.AppendChild(timeColumn);

            var icon = BuildGameIconTemplatePart();
            icon.SetValue(Grid.ColumnProperty, 0);
            row.AppendChild(icon);

            var content = new FrameworkElementFactory(typeof(Grid));
            content.SetValue(Grid.ColumnProperty, 1);
            content.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));

            var nameRow = new FrameworkElementFactory(typeof(RowDefinition));
            nameRow.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            content.AppendChild(nameRow);

            var barRow = new FrameworkElementFactory(typeof(RowDefinition));
            barRow.SetValue(RowDefinition.HeightProperty, GridLength.Auto);
            content.AppendChild(barRow);

            var name = new FrameworkElementFactory(typeof(TextBlock));
            name.SetBinding(TextBlock.TextProperty, new Binding("GameName"));
            name.SetValue(TextBlock.FontSizeProperty, 18d);
            name.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            name.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            name.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            name.SetValue(Grid.RowProperty, 0);
            content.AppendChild(name);

            var bar = new FrameworkElementFactory(typeof(ProgressBar));
            bar.SetValue(ProgressBar.MaximumProperty, 100d);
            bar.SetValue(FrameworkElement.HeightProperty, 8d);
            bar.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 10, 0, 0));
            bar.SetBinding(ProgressBar.ValueProperty, new Binding("Percent"));
            bar.SetValue(Grid.RowProperty, 1);
            content.AppendChild(bar);
            row.AppendChild(content);

            var time = new FrameworkElementFactory(typeof(TextBlock));
            time.SetBinding(TextBlock.TextProperty, new Binding("TimeText"));
            time.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            time.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            time.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            time.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            time.SetValue(Grid.ColumnProperty, 2);
            row.AppendChild(time);

            template.VisualTree = row;
            return template;
        }

        // 创建常用游戏图标模板片段。
        private FrameworkElementFactory BuildGameIconTemplatePart()
        {
            var frame = new FrameworkElementFactory(typeof(Border));
            frame.SetValue(FrameworkElement.WidthProperty, 46d);
            frame.SetValue(FrameworkElement.HeightProperty, 46d);
            frame.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            frame.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            frame.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            frame.SetResourceReference(Border.BackgroundProperty, "ControlBackgroundBrush");
            frame.SetResourceReference(Border.BorderBrushProperty, "NormalBorderBrush");
            frame.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var grid = new FrameworkElementFactory(typeof(Grid));

            var initial = new FrameworkElementFactory(typeof(TextBlock));
            initial.SetBinding(TextBlock.TextProperty, new Binding("Initial"));
            initial.SetValue(TextBlock.FontSizeProperty, 18d);
            initial.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            initial.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            initial.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            initial.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            initial.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            grid.AppendChild(initial);

            var image = new FrameworkElementFactory(typeof(Image));
            image.SetBinding(Image.SourceProperty, new Binding("IconPath"));
            image.SetValue(Image.StretchProperty, Stretch.UniformToFill);
            image.SetValue(FrameworkElement.WidthProperty, 44d);
            image.SetValue(FrameworkElement.HeightProperty, 44d);
            image.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            image.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            grid.AppendChild(image);

            frame.AppendChild(grid);
            return frame;
        }

        // 创建弱化的小字标签。
        private TextBlock CreateSmallMutedText(string text)
        {
            var block = CreateText(text, 13, FontWeights.Normal);
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            return block;
        }
        // 创建回顾预览区域。
        private UIElement BuildReviewPreview()
        {
            var border = CreatePanelBorder();
            border.Padding = new Thickness(16);
            var panel = new StackPanel();
            panel.Children.Add(CreateText("海报预览说明", 14, FontWeights.Normal));

            var review = CreateText(string.Empty, 22, FontWeights.SemiBold);
            review.Margin = new Thickness(0, 12, 0, 0);
            review.TextWrapping = TextWrapping.Wrap;
            review.SetBinding(TextBlock.TextProperty, new Binding("Summary.ReviewText"));
            panel.Children.Add(review);
            border.Child = panel;
            return border;
        }

        // 创建单个统计卡片。
        private UIElement CreateStatBox(string label, string valuePath, string description)
        {
            var border = CreatePanelBorder();
            border.Margin = new Thickness(0, 0, 10, 0);
            border.Padding = new Thickness(14);
            var panel = new StackPanel();
            var labelBlock = CreateText(label, 13, FontWeights.Normal);
            labelBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            panel.Children.Add(labelBlock);

            var valueBlock = CreateText(string.Empty, 26, FontWeights.SemiBold);
            valueBlock.Margin = new Thickness(0, 8, 0, 6);
            valueBlock.TextTrimming = TextTrimming.CharacterEllipsis;
            valueBlock.SetBinding(TextBlock.TextProperty, new Binding(valuePath));
            panel.Children.Add(valueBlock);

            var descBlock = CreateText(description, 12, FontWeights.Normal);
            descBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            panel.Children.Add(descBlock);
            border.Child = panel;
            return border;
        }

        // 创建通用面板边框。
        private Border CreatePanelBorder()
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
            border.SetResourceReference(Border.BackgroundProperty, "ControlBackgroundBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "NormalBorderBrush");
            return border;
        }

        // 创建按钮并绑定命令。
        private Button CreateButton(string text, string commandPath)
        {
            var label = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.NoWrap,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");

            var button = new Button
            {
                Content = label,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(12, 6, 12, 6),
                MinWidth = text.Length > 2 ? 132 : 64,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            button.SetBinding(Button.CommandProperty, new Binding(commandPath));
            return button;
        }

        // 创建文本控件。
        private TextBlock CreateText(string text, double size, FontWeight weight)
        {
            var block = new TextBlock
            {
                Text = text,
                FontSize = size,
                FontWeight = weight,
                TextWrapping = TextWrapping.Wrap
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            return block;
        }
    }
}
