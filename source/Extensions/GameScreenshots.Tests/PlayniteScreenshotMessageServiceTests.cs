// 文件用途：验证截图提示不会使用阻塞游戏的弹窗。
using GameScreenshots;
using NUnit.Framework;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace GameScreenshots.Tests
{
    // 验证保存成功提示走通知中心，不打断游戏窗口。
    [TestFixture]
    public class PlayniteScreenshotMessageServiceTests
    {
        [Test]
        public void Info_message_uses_notification_instead_of_dialog()
        {
            var api = new FakePlayniteApi();
            var service = new PlayniteScreenshotMessageService(api);

            service.ShowInfo("截图已保存：20260619-202421.png");

            Assert.AreEqual(1, api.Notifications.Messages.Count);
            Assert.AreEqual("截图已保存：20260619-202421.png", api.Notifications.Messages[0].Text);
            Assert.AreEqual(0, api.Dialogs.ShowMessageCount);
        }

        // 测试用 Playnite API。
        private class FakePlayniteApi : IPlayniteAPI
        {
            public FakeDialogs Dialogs { get; private set; }
            public FakeNotifications Notifications { get; private set; }

            public FakePlayniteApi()
            {
                Dialogs = new FakeDialogs();
                Notifications = new FakeNotifications();
            }

            IDialogsFactory IPlayniteAPI.Dialogs { get { return Dialogs; } }
            INotificationsAPI IPlayniteAPI.Notifications { get { return Notifications; } }
            public IMainViewAPI MainView { get { throw new NotImplementedException(); } }
            public IGameDatabaseAPI Database { get { throw new NotImplementedException(); } }
            public IPlaynitePathsAPI Paths { get { throw new NotImplementedException(); } }
            public IPlayniteInfoAPI ApplicationInfo { get { throw new NotImplementedException(); } }
            public IWebViewFactory WebViews { get { throw new NotImplementedException(); } }
            public IResourceProvider Resources { get { throw new NotImplementedException(); } }
            public IUriHandlerAPI UriHandler { get { throw new NotImplementedException(); } }
            public IPlayniteSettingsAPI ApplicationSettings { get { throw new NotImplementedException(); } }
            public IAddons Addons { get { throw new NotImplementedException(); } }
            public IEmulationAPI Emulation { get { throw new NotImplementedException(); } }
            public string ExpandGameVariables(Game game, string inputString) { throw new NotImplementedException(); }
            public string ExpandGameVariables(Game game, string inputString, string emulatorDir) { throw new NotImplementedException(); }
            public GameAction ExpandGameVariables(Game game, GameAction action) { throw new NotImplementedException(); }
            public void StartGame(Guid gameId) { throw new NotImplementedException(); }
            public void InstallGame(Guid gameId) { throw new NotImplementedException(); }
            public void UninstallGame(Guid gameId) { throw new NotImplementedException(); }
            public void AddCustomElementSupport(Plugin source, AddCustomElementSupportArgs args) { throw new NotImplementedException(); }
            public void AddSettingsSupport(Plugin source, AddSettingsSupportArgs args) { throw new NotImplementedException(); }
            public void AddConvertersSupport(Plugin source, AddConvertersSupportArgs args) { throw new NotImplementedException(); }
            public List<GamepadController> GetConnectedControllers() { throw new NotImplementedException(); }
        }

        // 测试用通知 API。
        private class FakeNotifications : INotificationsAPI
        {
            public ObservableCollection<NotificationMessage> Messages { get; private set; }
            public int Count { get { return Messages.Count; } }

            public FakeNotifications()
            {
                Messages = new ObservableCollection<NotificationMessage>();
            }

            public void Add(NotificationMessage message)
            {
                Messages.Add(message);
            }

            public void Add(string id, string text, NotificationType type)
            {
                Add(new NotificationMessage(id, text, type));
            }

            public void Remove(string id)
            {
                for (var i = Messages.Count - 1; i >= 0; i--)
                {
                    if (Messages[i].Id == id)
                    {
                        Messages.RemoveAt(i);
                    }
                }
            }

            public void RemoveAll()
            {
                Messages.Clear();
            }
        }

        // 测试用对话框 API。
        private class FakeDialogs : IDialogsFactory
        {
            public int ShowMessageCount { get; private set; }

            public MessageBoxResult ShowErrorMessage(string messageBoxText) { return MessageBoxResult.OK; }
            public MessageBoxResult ShowErrorMessage(string messageBoxText, string caption) { return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText, string caption, MessageBoxButton button) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText, string caption) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxResult ShowMessage(string messageBoxText) { ShowMessageCount++; return MessageBoxResult.OK; }
            public MessageBoxOption ShowMessage(string messageBoxText, string caption, MessageBoxImage icon, List<MessageBoxOption> options) { ShowMessageCount++; return null; }
            public string SelectFolder() { throw new NotImplementedException(); }
            public string SelectFolder(string initialDir) { throw new NotImplementedException(); }
            public string SelectFile(string filter) { throw new NotImplementedException(); }
            public string SelectFile(string filter, string initialDir) { throw new NotImplementedException(); }
            public List<string> SelectFiles(string filter) { throw new NotImplementedException(); }
            public List<string> SelectFiles(string filter, string initialDir) { throw new NotImplementedException(); }
            public string SelectIconFile() { throw new NotImplementedException(); }
            public string SelectIconFile(string initialDir) { throw new NotImplementedException(); }
            public string SelectImagefile() { throw new NotImplementedException(); }
            public string SelectImagefile(string initialDir) { throw new NotImplementedException(); }
            public string SaveFile(string filter) { throw new NotImplementedException(); }
            public string SaveFile(string filter, string initialDir) { throw new NotImplementedException(); }
            public string SaveFile(string filter, bool promptOverwrite) { throw new NotImplementedException(); }
            public string SaveFile(string filter, bool promptOverwrite, string initialDir) { throw new NotImplementedException(); }
            public StringSelectionDialogResult SelectString(string messageBoxText, string caption, string defaultInput) { throw new NotImplementedException(); }
            public StringSelectionDialogResult SelectString(string messageBoxText, string caption, string defaultInput, List<MessageBoxToggle> options) { throw new NotImplementedException(); }
            public void ShowSelectableString(string messageBoxText, string caption, string defaultInput) { throw new NotImplementedException(); }
            public ImageFileOption ChooseImageFile(List<ImageFileOption> files, string caption = null, double itemWidth = 240, double itemHeight = 180) { throw new NotImplementedException(); }
            public GenericItemOption ChooseItemWithSearch(List<GenericItemOption> items, Func<string, List<GenericItemOption>> searchFunction, string defaultSearch = null, string caption = null) { throw new NotImplementedException(); }
            public GlobalProgressResult ActivateGlobalProgress(Action<GlobalProgressActionArgs> progresAction, GlobalProgressOptions progressOptions) { throw new NotImplementedException(); }
            public GlobalProgressResult ActivateGlobalProgress(Func<GlobalProgressActionArgs, Task> progresAction, GlobalProgressOptions progressOptions) { throw new NotImplementedException(); }
            public Window CreateWindow(WindowCreationOptions options) { throw new NotImplementedException(); }
            public Window GetCurrentAppWindow() { throw new NotImplementedException(); }
        }
    }
}
