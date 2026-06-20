// 文件用途：承载 Switch 智能导入设置页，提供扫描目录和导入规则配置。
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace SwitchSmartImport
{
    // 设置界面用代码创建，避免新增 XAML 生成负担。
    public class SwitchSmartImportSettingsView : UserControl
    {
        private readonly StackPanel scanPathItemsPanel;

        public SwitchSmartImportSettingsView(SwitchSmartImportSettingsViewModel viewModel)
        {
            DataContext = viewModel;
            scanPathItemsPanel = new StackPanel();
            DataContextChanged += delegate { RefreshScanPathItems(); };
            Content = BuildLayout();
            RefreshScanPathItems();
        }

        // 构建整个设置页。
        private UIElement BuildLayout()
        {
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var root = new StackPanel
            {
                Margin = new Thickness(14)
            };

            root.Children.Add(new TextBlock
            {
                Text = "Switch 智能导入设置",
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            });

            root.Children.Add(BuildScanSection());
            root.Children.Add(BuildRuleSection());
            root.Children.Add(BuildImportSection());

            scroll.Content = root;
            return scroll;
        }

        // 构建扫描设置区域。
        private UIElement BuildScanSection()
        {
            var panel = CreateSectionPanel("扫描目录");
            panel.Children.Add(BuildQuickActions());
            panel.Children.Add(CreateCheckBox("启动 Playnite 时先扫一遍", "Settings.ScanOnStartup"));
            panel.Children.Add(CreateCheckBox("启用定期扫描", "Settings.EnableScheduledScan"));
            panel.Children.Add(CreateNumberInput("扫描间隔（分钟）", "Settings.ScheduledScanMinutes"));
            panel.Children.Add(CreateCheckBox("扫描子目录", "Settings.IncludeSubdirectories"));
            panel.Children.Add(CreateScanPathEditor());
            return panel;
        }

        // 构建设置页里的快捷操作按钮。
        private UIElement BuildQuickActions()
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var scanButton = new Button
            {
                Content = "立即扫描",
                Width = 100,
                Height = 30
            };
            scanButton.SetBinding(Button.CommandProperty, new Binding("RunManualScanCommand"));
            row.Children.Add(scanButton);

            var pendingButton = new Button
            {
                Content = "打开待确认列表",
                Width = 130,
                Height = 30,
                Margin = new Thickness(10, 0, 0, 0)
            };
            pendingButton.SetBinding(Button.CommandProperty, new Binding("OpenPendingImportsCommand"));
            row.Children.Add(pendingButton);

            return row;
        }

        // 构建规则设置区域。
        private UIElement BuildRuleSection()
        {
            var panel = CreateSectionPanel("识别规则");
            panel.Children.Add(CreateCheckBox("待确认列表显示 DLC 跳过项", "Settings.ShowDlcInPendingList"));
            panel.Children.Add(CreateCheckBox("记录最高补丁信息", "Settings.RecordHighestPatchVersion"));
            panel.Children.Add(CreateCheckBox("优先归并重复包", "Settings.PreferMergedPackage"));
            panel.Children.Add(CreateCheckBox("导入前必须先确认", "Settings.RequireManualConfirmation"));
            return panel;
        }

        // 构建导入设置区域。
        private UIElement BuildImportSection()
        {
            var panel = CreateSectionPanel("导入与资料");
            panel.Children.Add(CreateCheckBox("导入时使用相对路径", "Settings.ImportWithRelativePaths"));
            panel.Children.Add(CreateChoiceComboBox("默认平台", "PlatformChoices", "Settings.DefaultPlatformId", "Name", "Id"));
            panel.Children.Add(CreateEmulatorComboBox());
            panel.Children.Add(CreateChoiceComboBox("默认配置", "EmulatorProfileChoices", "Settings.DefaultEmulatorProfileId", "Name", "Id"));
            panel.Children.Add(CreateComboBox("资料来源", "MetadataSourceOptions", "Settings.MetadataSource"));
            panel.Children.Add(new TextBlock
            {
                Text = "留空就不处理；选择 Switch Local Metadata 时会整批全量刷新，不只补空字段。",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });
            return panel;
        }

        // 构建扫描目录可视编辑器。
        private UIElement CreateScanPathEditor()
        {
            var container = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0)
            };

            var header = new DockPanel
            {
                Margin = new Thickness(0, 0, 0, 8)
            };

            var addButton = new Button
            {
                Content = "新增目录",
                Width = 92,
                Height = 28
            };
            addButton.Click += AddScanPathClicked;
            DockPanel.SetDock(addButton, Dock.Right);
            header.Children.Add(addButton);

            header.Children.Add(new TextBlock
            {
                Text = "按顺序扫描，前面的优先级更高。",
                VerticalAlignment = VerticalAlignment.Center
            });
            container.Children.Add(header);

            container.Children.Add(scanPathItemsPanel);

            return container;
        }

        // 创建分组面板。
        private static StackPanel CreateSectionPanel(string title)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 18)
            };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            });
            return panel;
        }

        // 创建布尔设置项。
        private static CheckBox CreateCheckBox(string text, string bindingPath)
        {
            var check = new CheckBox
            {
                Content = text,
                Margin = new Thickness(0, 0, 0, 8)
            };
            check.SetBinding(CheckBox.IsCheckedProperty, new Binding(bindingPath) { Mode = BindingMode.TwoWay });
            return check;
        }

        // 创建数字输入项。
        private static UIElement CreateNumberInput(string label, string bindingPath)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            row.Children.Add(new TextBlock
            {
                Text = label,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            });

            var box = new TextBox
            {
                Width = 80
            };
            box.SetBinding(TextBox.TextProperty, new Binding(bindingPath)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            row.Children.Add(box);
            return row;
        }

        // 创建下拉框设置项。
        private static UIElement CreateComboBox(string label, string itemSourcePath, string selectedPath)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            row.Children.Add(new TextBlock
            {
                Text = label,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            });

            var box = new ComboBox
            {
                Width = 220
            };
            box.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemSourcePath));
            box.SetBinding(Selector.SelectedItemProperty, new Binding(selectedPath) { Mode = BindingMode.TwoWay });
            row.Children.Add(box);
            return row;
        }

        // 创建带显示字段和值字段的下拉框设置项。
        private static UIElement CreateChoiceComboBox(string label, string itemSourcePath, string selectedPath, string displayMemberPath, string selectedValuePath)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            row.Children.Add(new TextBlock
            {
                Text = label,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            });

            var box = new ComboBox
            {
                Width = 260,
                DisplayMemberPath = displayMemberPath,
                SelectedValuePath = selectedValuePath
            };
            box.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(itemSourcePath));
            box.SetBinding(Selector.SelectedValueProperty, new Binding(selectedPath) { Mode = BindingMode.TwoWay });
            row.Children.Add(box);
            return row;
        }

        // 创建默认模拟器下拉框，并在切换时刷新配置列表。
        private UIElement CreateEmulatorComboBox()
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            row.Children.Add(new TextBlock
            {
                Text = "默认模拟器",
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            });

            var box = new ComboBox
            {
                Width = 260,
                DisplayMemberPath = "Name",
                SelectedValuePath = "Id"
            };
            box.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("EmulatorChoices"));
            box.SetBinding(Selector.SelectedValueProperty, new Binding("Settings.DefaultEmulatorId") { Mode = BindingMode.TwoWay });
            box.SelectionChanged += DefaultEmulatorSelectionChanged;
            row.Children.Add(box);
            return row;
        }

        // 新增扫描目录并立即刷新列表。
        private void AddScanPathClicked(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is SwitchSmartImportSettingsViewModel viewModel))
            {
                return;
            }

            viewModel.AddScanPath();
            RefreshScanPathItems();
        }

        // 刷新扫描目录列表显示。
        private void RefreshScanPathItems()
        {
            scanPathItemsPanel.Children.Clear();

            if (!(DataContext is SwitchSmartImportSettingsViewModel viewModel) || viewModel.Settings?.ScanPaths == null)
            {
                return;
            }

            foreach (var item in viewModel.Settings.ScanPaths)
            {
                scanPathItemsPanel.Children.Add(CreateScanPathCard(viewModel, item));
            }

            UpdateLayout();
        }

        // 默认模拟器切换时刷新配置列表。
        private void DefaultEmulatorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is SwitchSmartImportSettingsViewModel viewModel)
            {
                viewModel.OnDefaultEmulatorChanged();
            }
        }

        // 构建单条扫描目录卡片。
        private UIElement CreateScanPathCard(SwitchSmartImportSettingsViewModel viewModel, SwitchScanPathConfig item)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Gainsboro,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var stack = new StackPanel();

            var top = new DockPanel();
            top.Children.Add(CreateActionButton("删除", delegate
            {
                viewModel.RemoveScanPath(item);
                RefreshScanPathItems();
            }));
            top.Children.Add(CreateActionButton("下移", delegate
            {
                viewModel.MoveScanPathDown(item);
                RefreshScanPathItems();
            }, new Thickness(6, 0, 0, 0)));
            top.Children.Add(CreateActionButton("上移", delegate
            {
                viewModel.MoveScanPathUp(item);
                RefreshScanPathItems();
            }, new Thickness(6, 0, 0, 0)));

            var nameBox = new TextBox
            {
                Margin = new Thickness(0, 0, 8, 0),
                DataContext = item
            };
            nameBox.SetBinding(TextBox.TextProperty, new Binding("Name")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            top.Children.Add(nameBox);
            stack.Children.Add(top);

            var pathRow = new DockPanel
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            pathRow.Children.Add(CreateActionButton("选择目录", delegate
            {
                viewModel.SelectScanPath(item);
            }, dockRight: true, width: 84));

            var pathBox = new TextBox
            {
                Margin = new Thickness(0, 0, 8, 0),
                DataContext = item
            };
            pathBox.SetBinding(TextBox.TextProperty, new Binding("Path")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            pathRow.Children.Add(pathBox);
            stack.Children.Add(pathRow);

            var bottom = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var enabled = new CheckBox
            {
                Content = "启用",
                DataContext = item
            };
            enabled.SetBinding(CheckBox.IsCheckedProperty, new Binding("Enabled") { Mode = BindingMode.TwoWay });
            bottom.Children.Add(enabled);

            var typeBox = new ComboBox
            {
                Width = 140,
                Margin = new Thickness(12, 0, 0, 0),
                DataContext = item,
                ItemsSource = viewModel.ScanPathTypeOptions
            };
            typeBox.SetBinding(Selector.SelectedItemProperty, new Binding("TypeHint") { Mode = BindingMode.TwoWay });
            bottom.Children.Add(typeBox);

            stack.Children.Add(bottom);
            border.Child = stack;
            return border;
        }

        // 创建右侧操作按钮。
        private static Button CreateActionButton(string text, RoutedEventHandler handler, Thickness? margin = null, bool dockRight = true, double width = 56)
        {
            var button = new Button
            {
                Content = text,
                Width = width,
                Margin = margin ?? new Thickness(0)
            };
            if (dockRight)
            {
                DockPanel.SetDock(button, Dock.Right);
            }

            button.Click += handler;
            return button;
        }
    }
}
