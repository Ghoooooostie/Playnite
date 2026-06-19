// 文件用途：提供全屏首页主区域里的游戏回顾栏目页面。
using Playnite.SDK.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GameActivityReview
{
    // 全屏内嵌回顾面板，可通过返回键关闭并通过方向键滚动。
    public class GameActivityReviewFullscreenPanelView : PluginUserControl
    {
        private readonly GameActivityReviewFullscreenState state;

        public GameActivityReviewFullscreenPanelView(GameActivityReviewViewModel viewModel, GameActivityReviewFullscreenState state)
        {
            this.state = state;
            Focusable = false;
            DataContext = viewModel;
            Content = BuildLayout(viewModel);
            SetBinding(VisibilityProperty, new Binding("IsPanelOpen")
            {
                Source = state,
                Converter = new BooleanToVisibilityConverter()
            });
            state.PropertyChanged += OnStatePropertyChanged;
            Unloaded += delegate { state.PropertyChanged -= OnStatePropertyChanged; };
        }

        // 创建覆盖游戏列表区域的回顾页面。
        private UIElement BuildLayout(GameActivityReviewViewModel viewModel)
        {
            var border = new Border
            {
                Background = Brushes.Transparent
            };
            border.SetResourceReference(Border.BackgroundProperty, "MainBackgourndBrush");
            border.Child = new GameActivityReviewView(viewModel, true, state.ClosePanelCommand);
            border.InputBindings.Add(new KeyBinding(state.ClosePanelCommand, new KeyGesture(Key.Back)));
            border.InputBindings.Add(new KeyBinding(state.ClosePanelCommand, new KeyGesture(Key.Escape)));
            return border;
        }

        // 打开后把焦点放到滚动容器，让方向键控制滚动。
        private void OnStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(GameActivityReviewFullscreenState.IsPanelOpen) || !state.IsPanelOpen)
            {
                return;
            }

            Dispatcher.BeginInvoke(new System.Action(FocusScrollViewer));
        }

        // 查找并聚焦滚动容器。
        private void FocusScrollViewer()
        {
            var scroll = FindVisualChild<GameActivityReviewScrollViewer>(this, new HashSet<DependencyObject>());
            scroll?.Focus();
        }

        // 查找指定类型子元素。
        private static T FindVisualChild<T>(DependencyObject parent, HashSet<DependencyObject> visited) where T : DependencyObject
        {
            if (parent == null || visited == null || !visited.Add(parent))
            {
                return null;
            }

            foreach (var child in GetVisualChildren(parent))
            {
                if (child is T match)
                {
                    return match;
                }

                var nested = FindVisualChild<T>(child, visited);
                if (nested != null)
                {
                    return nested;
                }
            }

            foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
            {
                if (child is T match)
                {
                    return match;
                }

                var nested = FindVisualChild<T>(child, visited);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        // 安全读取视觉树子节点。
        private static IEnumerable<DependencyObject> GetVisualChildren(DependencyObject parent)
        {
            var count = 0;
            try
            {
                count = VisualTreeHelper.GetChildrenCount(parent);
            }
            catch (InvalidOperationException)
            {
                yield break;
            }

            for (var index = 0; index < count; index++)
            {
                yield return VisualTreeHelper.GetChild(parent, index);
            }
        }
    }
}
