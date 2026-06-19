// 文件用途：保存截图插件设置，并提供 Playnite 设置页数据模型。
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

namespace GameScreenshots
{
    // 保存截图快捷键设置。
    public class GameScreenshotsSettings : ObservableObject
    {
        private bool hotkeyEnabled = true;
        private string hotkeyKey = "F12";
        private string hotkeyModifiers = "Control, Shift";
        private string screenshotDirectory;

        public bool HotkeyEnabled
        {
            get { return hotkeyEnabled; }
            set { SetValue(ref hotkeyEnabled, value); }
        }

        public string HotkeyKey
        {
            get { return hotkeyKey; }
            set { SetValue(ref hotkeyKey, value); }
        }

        public string HotkeyModifiers
        {
            get { return hotkeyModifiers; }
            set { SetValue(ref hotkeyModifiers, value); }
        }

        public string ScreenshotDirectory
        {
            get { return screenshotDirectory; }
            set { SetValue(ref screenshotDirectory, value); }
        }
    }

    // Playnite 设置页模型，负责读写截图插件配置。
    public class GameScreenshotsSettingsViewModel : ObservableObject, ISettings
    {
        private readonly GameScreenshotsPlugin plugin;
        private GameScreenshotsSettings editingClone;
        private GameScreenshotsSettings settings;

        public GameScreenshotsSettings Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                OnPropertyChanged();
                OnPropertyChanged("HotkeyText");
            }
        }

        public string HotkeyText
        {
            get { return BuildHotkeyText(); }
        }

        public string DefaultScreenshotDirectory
        {
            get { return GetDefaultScreenshotDirectory(); }
        }

        public ICommand SelectScreenshotDirectoryCommand { get; private set; }

        public GameScreenshotsSettingsViewModel(GameScreenshotsPlugin plugin)
            : this(plugin, plugin == null ? new GameScreenshotsSettings() : plugin.LoadPluginSettings<GameScreenshotsSettings>() ?? new GameScreenshotsSettings())
        {
        }

        internal GameScreenshotsSettingsViewModel(GameScreenshotsPlugin plugin, GameScreenshotsSettings settings)
        {
            this.plugin = plugin;
            Settings = settings ?? new GameScreenshotsSettings();
            SelectScreenshotDirectoryCommand = new RelayCommand(SelectScreenshotDirectory);
            NormalizeSettings();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
            NormalizeSettings();
        }

        public void EndEdit()
        {
            NormalizeSettings();
            if (plugin != null)
            {
                plugin.SavePluginSettings(Settings);
                plugin.ReloadScreenshotServices();
                plugin.ReloadHotkey();
            }
        }

        public bool VerifySettings(out List<string> errors)
        {
            NormalizeSettings();
            errors = new List<string>();
            try
            {
                ScreenshotPathResolver.Resolve(Settings.ScreenshotDirectory, GetDefaultScreenshotDirectory());
            }
            catch (Exception e)
            {
                errors.Add("截图保存目录无效：" + e.Message);
            }

            return errors.Count == 0;
        }

        // 根据按键事件录入新的快捷键。
        internal bool ApplyHotkey(Key key, ModifierKeys modifiers)
        {
            if (IsModifierKey(key) || key == Key.None)
            {
                return false;
            }

            Settings.HotkeyKey = key.ToString();
            Settings.HotkeyModifiers = FormatModifiers(modifiers);
            OnPropertyChanged("HotkeyText");
            return true;
        }

        // 创建当前设置对应的快捷键。
        internal ScreenshotHotkey CreateHotkey()
        {
            NormalizeSettings();
            ModifierKeys modifiers;
            TryParseModifiers(Settings.HotkeyModifiers, out modifiers);
            return new ScreenshotHotkey(ParseKey(Settings.HotkeyKey), modifiers);
        }

        // 选择截图保存目录。
        private void SelectScreenshotDirectory()
        {
            if (plugin == null)
            {
                return;
            }

            var initial = string.IsNullOrWhiteSpace(Settings.ScreenshotDirectory) ? GetDefaultScreenshotDirectory() : Settings.ScreenshotDirectory;
            var selected = plugin.SelectScreenshotDirectory(initial);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                Settings.ScreenshotDirectory = selected;
            }
        }

        // 规范化设置，避免空按键导致注册失败。
        private void NormalizeSettings()
        {
            if (Settings == null)
            {
                Settings = new GameScreenshotsSettings();
            }

            if (ParseKey(Settings.HotkeyKey) == Key.None)
            {
                Settings.HotkeyKey = "F12";
            }

            ModifierKeys modifiers;
            if (!TryParseModifiers(Settings.HotkeyModifiers, out modifiers))
            {
                Settings.HotkeyModifiers = "Control, Shift";
            }

            OnPropertyChanged("HotkeyText");
            OnPropertyChanged("DefaultScreenshotDirectory");
        }

        // 获取默认截图目录。
        private string GetDefaultScreenshotDirectory()
        {
            if (plugin != null)
            {
                return plugin.GetDefaultScreenshotDirectory();
            }

            return Path.Combine(Path.GetTempPath(), "GameScreenshots");
        }

        // 生成设置页展示文本。
        private string BuildHotkeyText()
        {
            ModifierKeys modifiers;
            TryParseModifiers(Settings.HotkeyModifiers, out modifiers);
            var key = ParseKey(Settings.HotkeyKey);
            var prefix = FormatModifiersForDisplay(modifiers);
            return string.IsNullOrEmpty(prefix) ? key.ToString() : prefix + " + " + key;
        }

        // 解析按键名。
        private static Key ParseKey(string keyText)
        {
            if (string.IsNullOrWhiteSpace(keyText))
            {
                return Key.None;
            }

            try
            {
                return (Key)Enum.Parse(typeof(Key), keyText.Trim(), true);
            }
            catch
            {
                return Key.None;
            }
        }

        // 解析修饰键文本。
        private static bool TryParseModifiers(string modifiersText, out ModifierKeys modifiers)
        {
            modifiers = ModifierKeys.None;
            if (string.IsNullOrWhiteSpace(modifiersText) || modifiersText.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var parts = modifiersText.Split(new[] { ',', '+', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                try
                {
                    modifiers |= (ModifierKeys)Enum.Parse(typeof(ModifierKeys), part.Trim(), true);
                }
                catch
                {
                    modifiers = ModifierKeys.None;
                    return false;
                }
            }

            return true;
        }

        // 保存用修饰键文本。
        private static string FormatModifiers(ModifierKeys modifiers)
        {
            var parts = new List<string>();
            if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                parts.Add("Control");
            }

            if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                parts.Add("Shift");
            }

            if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                parts.Add("Alt");
            }

            if ((modifiers & ModifierKeys.Windows) == ModifierKeys.Windows)
            {
                parts.Add("Windows");
            }

            return parts.Count == 0 ? "None" : string.Join(", ", parts);
        }

        // 展示用修饰键文本。
        private static string FormatModifiersForDisplay(ModifierKeys modifiers)
        {
            var saved = FormatModifiers(modifiers);
            return saved == "None" ? string.Empty : saved.Replace(", ", " + ");
        }

        // 判断是否只是修饰键本身。
        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin;
        }
    }
}
