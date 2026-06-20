// 文件用途：承载 Switch 待确认导入中心的最小桌面视图。
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace SwitchSmartImport
{
    // 待确认导入中心视图。
    public class SwitchPendingImportView : UserControl
    {
        private readonly SwitchPendingImportViewModel viewModel;

        public SwitchPendingImportView(SwitchPendingImportViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            viewModel.StateChanged += OnViewModelStateChanged;
            Content = BuildLayout();
        }

        private UIElement BuildLayout()
        {
            var root = new DockPanel
            {
                Margin = new Thickness(18)
            };

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

        private UIElement BuildToolbar()
        {
            var toolbar = new DockPanel
            {
                Margin = new Thickness(0, 0, 0, 14)
            };

            var button = new Button
            {
                Content = "确认导入",
                Width = 120,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            button.Click += delegate
            {
                viewModel.ImportSelected();
            };
            DockPanel.SetDock(button, Dock.Right);
            toolbar.Children.Add(button);

            var title = new TextBlock
            {
                Text = "待确认导入列表",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold
            };
            toolbar.Children.Add(title);
            return toolbar;
        }

        private UIElement BuildContent()
        {
            var panel = new StackPanel();
            panel.Children.Add(BuildSummary());
            panel.Children.Add(BuildCandidatesSection());
            panel.Children.Add(BuildSkippedSection());
            return panel;
        }

        private UIElement BuildSummary()
        {
            var savedAtText = viewModel.SavedAt == DateTime.MinValue
                ? "最近扫描：暂无"
                : "最近扫描：" + viewModel.SavedAt.ToString("yyyy-MM-dd HH:mm");

            return new TextBlock
            {
                Text = "候选数量：" + viewModel.Candidates.Count + "  " + savedAtText,
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = Brushes.DimGray
            };
        }

        private UIElement BuildCandidatesSection()
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 16)
            };
            panel.Children.Add(new TextBlock
            {
                Text = "待导入候选",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });
            panel.Children.Add(BuildCandidatesList());
            return panel;
        }

        private UIElement BuildSkippedSection()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "已跳过项目",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });
            panel.Children.Add(BuildSkippedList());
            return panel;
        }

        private UIElement BuildCandidatesList()
        {
            var items = new ItemsControl();
            items.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Candidates"));
            items.ItemTemplate = BuildCandidateTemplate();

            return new ScrollViewer
            {
                Content = items,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
        }

        private UIElement BuildSkippedList()
        {
            var items = new ItemsControl();
            items.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("SkippedItems"));
            items.ItemTemplate = BuildSkippedTemplate();

            return new ScrollViewer
            {
                Content = items,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
        }

        private DataTemplate BuildCandidateTemplate()
        {
            var template = new DataTemplate(typeof(SwitchImportCandidate));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.BorderBrushProperty, Brushes.Gainsboro);
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new Thickness(10));
            border.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 8));

            var stack = new FrameworkElementFactory(typeof(StackPanel));

            var top = new FrameworkElementFactory(typeof(DockPanel));
            var check = new FrameworkElementFactory(typeof(CheckBox));
            check.SetValue(DockPanel.DockProperty, Dock.Right);
            check.SetBinding(CheckBox.IsCheckedProperty, new Binding("Import") { Mode = BindingMode.TwoWay });
            top.AppendChild(check);

            var name = new FrameworkElementFactory(typeof(TextBox));
            name.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
            name.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
            name.SetValue(TextBox.FontWeightProperty, FontWeights.SemiBold);
            name.SetBinding(TextBox.TextProperty, new Binding("GameName") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            top.AppendChild(name);
            stack.AppendChild(top);

            var path = new FrameworkElementFactory(typeof(TextBlock));
            path.SetValue(TextBlock.MarginProperty, new Thickness(0, 4, 0, 0));
            path.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            path.SetValue(TextBlock.ForegroundProperty, Brushes.DimGray);
            path.SetBinding(TextBlock.TextProperty, new Binding("BasePath"));
            stack.AppendChild(path);

            var patch = new FrameworkElementFactory(typeof(TextBlock));
            patch.SetValue(TextBlock.MarginProperty, new Thickness(0, 4, 0, 0));
            patch.SetValue(TextBlock.ForegroundProperty, Brushes.DimGray);
            patch.SetBinding(TextBlock.TextProperty, new Binding("HighestPatchVersion") { StringFormat = "最高补丁：{0}" });
            stack.AppendChild(patch);

            var platform = new FrameworkElementFactory(typeof(ComboBox));
            platform.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 8, 0, 0));
            platform.SetValue(ItemsControl.DisplayMemberPathProperty, "Name");
            platform.SetValue(Selector.SelectedValuePathProperty, "Id");
            platform.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("DataContext.PlatformOptions")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(UserControl), 1)
            });
            platform.SetBinding(Selector.SelectedValueProperty, new Binding("SelectedPlatformId") { Mode = BindingMode.TwoWay });
            stack.AppendChild(platform);

            border.AppendChild(stack);
            template.VisualTree = border;
            return template;
        }

        private DataTemplate BuildSkippedTemplate()
        {
            var template = new DataTemplate(typeof(SwitchSkippedItem));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.BorderBrushProperty, Brushes.Gainsboro);
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            border.SetValue(Border.PaddingProperty, new Thickness(10));
            border.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 8));

            var stack = new FrameworkElementFactory(typeof(StackPanel));

            var reason = new FrameworkElementFactory(typeof(TextBlock));
            reason.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            reason.SetBinding(TextBlock.TextProperty, new Binding("Reason"));
            stack.AppendChild(reason);

            var path = new FrameworkElementFactory(typeof(TextBlock));
            path.SetValue(TextBlock.MarginProperty, new Thickness(0, 4, 0, 0));
            path.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            path.SetValue(TextBlock.ForegroundProperty, Brushes.DimGray);
            path.SetBinding(TextBlock.TextProperty, new Binding("Path"));
            stack.AppendChild(path);

            border.AppendChild(stack);
            template.VisualTree = border;
            return template;
        }

        private void OnViewModelStateChanged()
        {
            var dispatcher = Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                RefreshBindings();
                return;
            }

            dispatcher.BeginInvoke(new Action(RefreshBindings), DispatcherPriority.Normal);
        }

        private void RefreshBindings()
        {
            DataContext = null;
            DataContext = viewModel;
        }
    }
}
