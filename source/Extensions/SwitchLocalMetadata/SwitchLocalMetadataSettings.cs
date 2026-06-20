using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.IO;

namespace SwitchLocalMetadata
{
    // 保存 hactoolnet 和密钥路径配置。
    public class SwitchLocalMetadataSettings : ObservableObject
    {
        private string hactoolnetPath = string.Empty;
        private string prodKeysPath = string.Empty;
        private string titleKeysPath = string.Empty;
        private bool enableOnlineBackgroundSearch = true;

        public string HactoolnetPath { get => hactoolnetPath; set => SetValue(ref hactoolnetPath, value); }
        public string ProdKeysPath { get => prodKeysPath; set => SetValue(ref prodKeysPath, value); }
        public string TitleKeysPath { get => titleKeysPath; set => SetValue(ref titleKeysPath, value); }
        public bool EnableOnlineBackgroundSearch { get => enableOnlineBackgroundSearch; set => SetValue(ref enableOnlineBackgroundSearch, value); }
    }

    // Playnite 设置页模型，负责读写插件配置。
    public class SwitchLocalMetadataSettingsViewModel : ObservableObject, ISettings
    {
        private readonly SwitchLocalMetadataPlugin plugin;
        private SwitchLocalMetadataSettings editingClone;
        private SwitchLocalMetadataSettings settings;

        public SwitchLocalMetadataSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public SwitchLocalMetadataSettingsViewModel(SwitchLocalMetadataPlugin plugin)
        {
            this.plugin = plugin;
            Settings = plugin.LoadPluginSettings<SwitchLocalMetadataSettings>() ?? SwitchToolPathResolver.ResolveDefaults();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            AddMissingFileError(Settings.HactoolnetPath, "hactoolnet.exe", errors);
            AddMissingFileError(Settings.ProdKeysPath, "prod.keys", errors);
            if (!string.IsNullOrWhiteSpace(Settings.TitleKeysPath) && !File.Exists(Settings.TitleKeysPath))
            {
                errors.Add("title.keys 路径不存在。");
            }

            return errors.Count == 0;
        }

        private static void AddMissingFileError(string path, string name, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                errors.Add(name + " 路径不存在。");
            }
        }
    }
}
