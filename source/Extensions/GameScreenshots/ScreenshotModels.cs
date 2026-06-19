// 文件用途：定义截图插件的核心模型和服务接口。
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameScreenshots
{
    // 单张截图的展示和文件信息。
    public class ScreenshotItem : ObservableObject
    {
        private bool isSelected;

        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CapturedAt { get; set; }

        public bool IsSelected
        {
            get { return isSelected; }
            set { SetValue(ref isSelected, value); }
        }

        public string CapturedAtText
        {
            get { return CapturedAt.ToString("yyyy-MM-dd HH:mm:ss"); }
        }

        public ImageSource ThumbnailSource
        {
            get { return LoadThumbnailSource(); }
        }

        // 显式加载本地图片，避免扩展页面中字符串路径不显示。
        private ImageSource LoadThumbnailSource()
        {
            if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
            {
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(FilePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }

    // 某个游戏下的一组截图，用于画廊按游戏分区展示。
    public class ScreenshotGameGroup
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public ObservableCollection<ScreenshotItem> Screenshots { get; private set; }

        public int Count
        {
            get { return Screenshots.Count; }
        }

        public ScreenshotGameGroup()
        {
            Screenshots = new ObservableCollection<ScreenshotItem>();
        }
    }

    // 截图保存完成事件参数。
    public class ScreenshotCapturedEventArgs : EventArgs
    {
        public ScreenshotItem Item { get; private set; }

        public ScreenshotCapturedEventArgs(ScreenshotItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            Item = item;
        }
    }

    // 游戏截图目录的元数据。
    public class ScreenshotGameMetadata
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
    }

    // 全局截图快捷键。
    public class ScreenshotHotkey
    {
        public Key Key { get; private set; }
        public ModifierKeys Modifiers { get; private set; }

        public ScreenshotHotkey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }
    }

    // 负责按游戏保存和读取截图。
    public interface IScreenshotStore
    {
        ScreenshotItem SaveScreenshot(Guid gameId, string gameName, byte[] pngBytes, DateTime capturedAt);
        System.Collections.Generic.IEnumerable<ScreenshotItem> LoadGameScreenshots(Guid gameId);
        System.Collections.Generic.IEnumerable<ScreenshotItem> LoadAllScreenshots();
        void DeleteScreenshots(System.Collections.Generic.IEnumerable<ScreenshotItem> screenshots);
    }

    // 负责截取当前屏幕。
    public interface IScreenshotCaptureService
    {
        byte[] CapturePng();
    }

    // 负责完成单个游戏截图流程。
    public interface IGameScreenshotService
    {
        event EventHandler<ScreenshotCapturedEventArgs> ScreenshotCaptured;
        ScreenshotItem CaptureGame(Game game, DateTime capturedAt);
    }

    // 负责注册和注销系统级截图快捷键。
    public interface IScreenshotHotkeyService
    {
        void Register(ScreenshotHotkey hotkey, Action action);
        void Unregister();
    }

    // 负责取得 Playnite 当前选中的游戏。
    public interface IGameSelectionProvider
    {
        Game GetSelectedGame();
    }

    // 负责显示截图插件消息。
    public interface IScreenshotMessageService
    {
        void ShowInfo(string message);
        void ShowError(string message);
    }

    // 负责打开单个游戏截图窗口。
    public interface IScreenshotWindowService
    {
        void OpenGameScreenshots(Game game);
    }
}
