using SwitchSmartImport.Tests;
using System;
using System.Collections.Generic;

namespace SwitchSmartImport.TestRunner
{
    // 文件用途：轻量测试运行器，方便在没有 NUnit Console 时验证智能导入插件。
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            var pluginTests = new SwitchSmartImportPluginTests();
            var storeTests = new SwitchPendingImportStoreTests();
            var classifierTests = new SwitchPackageClassifierTests();
            var mergerTests = new SwitchCandidateMergerTests();
            var scannerTests = new SwitchImportScannerTests();
            var scheduledTests = new SwitchScheduledScanServiceTests();
            var executorTests = new SwitchImportExecutorTests();
            var metadataRefreshTests = new SwitchMetadataRefreshServiceTests();
            var pendingViewTests = new SwitchPendingImportViewTests();
            var tests = new List<Action>
            {
                pluginTests.Plugin_adds_main_menu_entries,
                pluginTests.Plugin_exposes_settings,
                pluginTests.Plugin_main_menu_opens_pending_import_window,
                storeTests.Settings_default_to_no_metadata_source_and_manual_confirmation,
                storeTests.Pending_store_round_trips_candidates,
                storeTests.Settings_view_model_allows_managing_scan_paths,
                storeTests.Settings_view_model_reuses_smallest_scan_path_number,
                storeTests.Settings_view_model_rejects_invalid_scheduled_scan_minutes,
                storeTests.Settings_view_model_runs_scan_and_opens_pending_list,
                storeTests.Settings_view_model_refreshes_platform_and_emulator_choices,
                classifierTests.Classifier_recognizes_base_and_update_in_same_directory,
                classifierTests.Classifier_recognizes_dlc_from_file_name,
                classifierTests.Classifier_extracts_normalized_base_title_id_for_update_package,
                classifierTests.Classifier_treats_add_on_package_as_dlc,
                classifierTests.Classifier_treats_patch_file_without_title_id_as_update,
                classifierTests.Classifier_treats_version_only_file_as_update,
                mergerTests.Merger_keeps_single_candidate_and_highest_patch,
                mergerTests.Merger_skips_update_when_base_is_missing,
                mergerTests.Merger_matches_update_with_base_by_normalized_title_id,
                mergerTests.Merger_keeps_single_candidate_when_duplicate_bases_share_normalized_name,
                mergerTests.Merger_prefers_non_update_directory_package_when_duplicate_bases_exist,
                mergerTests.Merger_keeps_single_candidate_when_file_name_and_parent_directory_point_to_same_title,
                scannerTests.Scanner_ignores_non_switch_files_and_collects_switch_packages,
                scheduledTests.Scheduled_scan_only_updates_pending_store,
                scheduledTests.Scheduled_scan_interval_can_be_updated,
                scheduledTests.Scheduled_scan_invokes_completed_event_after_saving_pending_store,
                executorTests.Import_executor_creates_one_game_from_candidate,
                executorTests.Import_executor_requires_default_emulator_configuration,
                executorTests.Import_executor_updates_existing_game_when_same_rom_path_already_exists,
                executorTests.Import_executor_updates_existing_game_when_same_name_and_directory_use_another_base_format,
                executorTests.Import_executor_updates_existing_game_when_rom_directory_alias_matches_another_base_format,
                pluginTests.Plugin_run_scan_auto_imports_when_manual_confirmation_is_disabled,
                pluginTests.Plugin_scheduled_scan_auto_imports_and_notifies_when_manual_confirmation_is_disabled,
                metadataRefreshTests.Metadata_refresh_is_skipped_when_source_is_none,
                metadataRefreshTests.Metadata_refresh_uses_switch_local_metadata_and_overwrites_existing_values,
                metadataRefreshTests.Metadata_refresh_continues_when_one_game_provider_fails,
                pendingViewTests.Pending_view_imports_selected_candidates_and_refreshes_metadata,
                pendingViewTests.Pending_view_shows_saved_time_in_summary,
                pendingViewTests.Pending_view_does_not_throw_when_import_configuration_is_missing,
                pendingViewTests.Pending_view_runs_import_in_progress_service,
                pendingViewTests.Pending_view_runs_import_in_background_and_reports_notifications,
                pendingViewTests.Pending_view_reports_error_notification_when_background_import_fails
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
                    Console.WriteLine("FAIL " + test.Method.Name + ": " + ex);
                }
            }

            Console.WriteLine("Passed: " + (tests.Count - failed) + ", Failed: " + failed);
            return failed == 0 ? 0 : 1;
        }
    }
}
