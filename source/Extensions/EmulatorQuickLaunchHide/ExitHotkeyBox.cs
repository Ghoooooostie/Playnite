// 文件用途：在插件设置页中录入结束快捷键，支持单键 F5 和组合键。
using System.Windows.Controls;
using System.Windows.Input;

namespace EmulatorQuickLaunchHide
{
    // 只读输入框，获得焦点后按下快捷键即可写入绑定值。
    public class ExitHotkeyBox : TextBox
    {
        public static readonly System.Windows.DependencyProperty HotkeyProperty =
            System.Windows.DependencyProperty.Register(
                nameof(Hotkey),
                typeof(EmulatorExitHotkey),
                typeof(ExitHotkeyBox),
                new System.Windows.FrameworkPropertyMetadata(
                    null,
                    System.Windows.FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnHotkeyChanged));

        public EmulatorExitHotkey Hotkey
        {
            get => (EmulatorExitHotkey)GetValue(HotkeyProperty);
            set => SetValue(HotkeyProperty, value);
        }

        public ExitHotkeyBox()
        {
            IsReadOnly = true;
            IsUndoEnabled = false;
            PreviewKeyDown += HandlePreviewKeyDown;
            UpdateText();
        }

        // 录入用户按下的快捷键。
        private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (IsModifierKey(key))
            {
                return;
            }

            Hotkey = new EmulatorExitHotkey(key, Keyboard.Modifiers);
            UpdateText();
        }

        // 判断是否只是按下了修饰键。
        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps;
        }

        // 绑定值变化时刷新显示文本。
        private static void OnHotkeyChanged(System.Windows.DependencyObject sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            ((ExitHotkeyBox)sender).UpdateText();
        }

        // 显示当前快捷键。
        private void UpdateText()
        {
            if (Hotkey == null || Hotkey.Key == Key.None)
            {
                Text = string.Empty;
                return;
            }

            var prefix = Hotkey.Modifiers == ModifierKeys.None ? string.Empty : Hotkey.Modifiers.ToString().Replace(", ", " + ") + " + ";
            Text = prefix + Hotkey.Key;
        }
    }
}
