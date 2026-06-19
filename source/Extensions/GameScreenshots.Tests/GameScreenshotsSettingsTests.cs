// 文件用途：验证截图插件设置页的快捷键录入逻辑。
using GameScreenshots;
using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace GameScreenshots.Tests
{
    // 验证快捷键设置可以由按键事件写入。
    [TestFixture]
    public class GameScreenshotsSettingsTests
    {
        [Test]
        public void Apply_hotkey_records_key_and_modifiers()
        {
            var settings = new GameScreenshotsSettings();
            var viewModel = new GameScreenshotsSettingsViewModel(null, settings);

            viewModel.ApplyHotkey(Key.F11, ModifierKeys.Control | ModifierKeys.Alt);

            Assert.AreEqual("F11", settings.HotkeyKey);
            Assert.AreEqual("Control, Alt", settings.HotkeyModifiers);
        }

        [Test]
        public void Hotkey_service_uses_message_only_window()
        {
            var parameters = ScreenshotHotkeyService.CreateMessageOnlyWindowParameters();

            Assert.AreEqual(new IntPtr(-3), parameters.ParentWindow);
            Assert.AreEqual(0, parameters.Width);
            Assert.AreEqual(0, parameters.Height);
        }

        [Test]
        public void Hotkey_service_detects_foreign_dispatcher_for_dispose()
        {
            Dispatcher foreignDispatcher = null;
            var thread = new Thread(new ThreadStart(delegate
            {
                foreignDispatcher = Dispatcher.CurrentDispatcher;
            }));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            Assert.IsTrue(ScreenshotHotkeyService.RequiresDispatcherInvoke(foreignDispatcher));
        }
    }
}
