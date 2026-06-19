using GameScreenshots.Tests;
using System;
using System.Collections.Generic;

namespace GameScreenshots.TestRunner
{
    // 轻量测试运行器，方便在没有 NUnit Console 时验证截图插件。
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            var storeTests = new ScreenshotStoreTests();
            var pluginTests = new GameScreenshotsPluginTests();
            var settingsTests = new GameScreenshotsSettingsTests();
            var viewTests = new GameScreenshotsViewTests();
            var viewModelTests = new GameScreenshotsViewModelTests();
            var pathTests = new ScreenshotPathResolverTests();
            var messageTests = new PlayniteScreenshotMessageServiceTests();
            var tests = new List<Action>
            {
                storeTests.Store_saves_screenshot_under_game_id_directory,
                storeTests.Store_loads_game_screenshots_newest_first,
                storeTests.Store_loads_all_screenshots_across_games,
                pluginTests.Hotkey_captures_currently_selected_game,
                pluginTests.Hotkey_does_not_capture_when_no_game_is_selected,
                pluginTests.Plugin_adds_game_menu_items_for_capture_and_view,
                pluginTests.Plugin_registers_fullscreen_home_screenshots_control,
                pluginTests.Fullscreen_home_control_shows_latest_screenshots_for_selected_game,
                pluginTests.Dispose_unregisters_hotkey,
                settingsTests.Apply_hotkey_records_key_and_modifiers,
                settingsTests.Hotkey_service_uses_message_only_window,
                settingsTests.Hotkey_service_detects_foreign_dispatcher_for_dispose,
                viewTests.Game_view_uses_scroll_viewer_with_items_control_for_screenshots,
                viewTests.Gallery_view_uses_scroll_viewer_with_items_control_for_screenshots,
                viewModelTests.Game_view_refreshes_when_matching_game_screenshot_is_saved,
                viewModelTests.Gallery_view_refreshes_when_any_screenshot_is_saved,
                viewModelTests.Gallery_view_groups_screenshots_by_game_newest_group_first,
                viewModelTests.Disposed_view_does_not_refresh_after_screenshot_is_saved,
                pathTests.Empty_custom_directory_uses_default_screenshot_directory,
                pathTests.Custom_directory_overrides_default_screenshot_directory,
                messageTests.Info_message_uses_notification_instead_of_dialog
            };

            var failed = 0;
            foreach (var test in tests)
            {
                try
                {
                    test();
                    Console.WriteLine("PASS " + test.Method.Name);
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("FAIL " + test.Method.Name + ": " + ex.ToString());
                }
            }

            Console.WriteLine("Passed: " + (tests.Count - failed) + ", Failed: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}




