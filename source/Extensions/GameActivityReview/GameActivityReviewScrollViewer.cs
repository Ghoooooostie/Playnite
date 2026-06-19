// 文件用途：提供全屏回顾页中可用方向键和翻页键滚动的容器。
using System.Windows.Controls;
using System.Windows.Input;

namespace GameActivityReview
{
    // 用键盘或手柄映射出的方向键滚动内容。
    public class GameActivityReviewScrollViewer : ScrollViewer
    {
        public ICommand CloseCommand { get; set; }

        public GameActivityReviewScrollViewer()
        {
            Focusable = true;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        // 按方向键或翻页键滚动回顾内容。
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                if (!CanScrollUp())
                {
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                    e.Handled = true;
                    return;
                }

                ScrollOneStepUp();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                if (!CanScrollDown())
                {
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                    e.Handled = true;
                    return;
                }

                ScrollOneStepDown();
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                PageUp();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                PageDown();
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                ScrollToTop();
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                ScrollToBottom();
                e.Handled = true;
            }
            else if (GameActivityReviewControllerInput.IsCancellation(e))
            {
                if (CloseCommand != null && CloseCommand.CanExecute(null))
                {
                    CloseCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        // 判断是否还能向上滚动。
        private bool CanScrollUp()
        {
            return VerticalOffset > 0;
        }

        // 判断是否还能向下滚动。
        private bool CanScrollDown()
        {
            return VerticalOffset < ScrollableHeight;
        }

        // 按一次上键滚动固定距离。
        private void ScrollOneStepUp()
        {
            ScrollToVerticalOffset(System.Math.Max(0, VerticalOffset - 72));
        }

        // 按一次下键滚动固定距离。
        private void ScrollOneStepDown()
        {
            ScrollToVerticalOffset(System.Math.Min(ScrollableHeight, VerticalOffset + 72));
        }
    }
}
