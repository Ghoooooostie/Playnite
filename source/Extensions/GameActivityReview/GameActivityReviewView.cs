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
            panel.Children.Add(CreateText("每日游玩时长", 22, FontWeights.SemiBold));

            var scope = CreateText(string.Empty, 13, FontWeights.Normal);
            scope.Margin = new Thickness(0, 4, 0, 12);
            scope.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            scope.SetBinding(TextBlock.TextProperty, new Binding("Summary.DateRangeText") { StringFormat = "{0}" });
            panel.Children.Add(scope);

            var list = new ItemsControl();
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Summary.DailyItems"));
            list.ItemTemplate = BuildDailyChartTemplate();
            panel.Children.Add(list);
            return panel;
        }

        // 创建每日图表行模板。
        private DataTemplate BuildDailyChartTemplate()
        {
            var template = new DataTemplate(typeof(GameActivityDailyItem));
            var row = new FrameworkElementFactory(typeof(Grid));
            row.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 8));
            row.SetValue(FrameworkElement.MinHeightProperty, 28d);

            var labelColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            labelColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(72));
            row.AppendChild(labelColumn);

            var barColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            barColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            row.AppendChild(barColumn);

            var timeColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            timeColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(110));
            row.AppendChild(timeColumn);

            var label = new FrameworkElementFactory(typeof(TextBlock));
            label.SetBinding(TextBlock.TextProperty, new Binding("Label"));
            label.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            label.SetValue(Grid.ColumnProperty, 0);
            row.AppendChild(label);

            var bar = new FrameworkElementFactory(typeof(ProgressBar));
            bar.SetValue(ProgressBar.MaximumProperty, 100d);
            bar.SetValue(FrameworkElement.HeightProperty, 18d);
            bar.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bar.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0));
            bar.SetBinding(ProgressBar.ValueProperty, new Binding("Percent"));
            bar.SetValue(Grid.ColumnProperty, 1);
            row.AppendChild(bar);

            var time = new FrameworkElementFactory(typeof(TextBlock));
            time.SetBinding(TextBlock.TextProperty, new Binding("TimeText"));
            time.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            time.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            time.SetResourceReference(TextBlock.ForegroundProperty, "TextBrushDarker");
            time.SetValue(Grid.ColumnProperty, 2);
            row.AppendChild(time);

            template.VisualTree = row;
            return template;
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
