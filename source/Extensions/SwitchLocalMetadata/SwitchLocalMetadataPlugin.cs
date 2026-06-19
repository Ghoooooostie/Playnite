using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace SwitchLocalMetadata
{
    // Playnite 元数据插件入口，负责声明支持字段并创建本地 Provider。
    public class SwitchLocalMetadataPlugin : MetadataPlugin
    {
        private readonly SwitchLocalMetadataSettingsViewModel settings;

        public override string Name => "Switch Local Metadata";

        public override Guid Id { get; } = Guid.Parse("B49F4F91-73E2-46E8-B66E-3D09BD6BE2FA");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Platform,
            MetadataField.Links,
            MetadataField.Icon,
            MetadataField.CoverImage,
            MetadataField.InstallSize
        };

        public SwitchLocalMetadataPlugin(IPlayniteAPI api) : base(api)
        {
            settings = new SwitchLocalMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new SwitchLocalMetadataProvider(options.GameData, settings.Settings);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SwitchLocalMetadataSettingsView();
        }
    }

    // 按需返回单个游戏的本地 Switch 元数据。
    public class SwitchLocalMetadataProvider : OnDemandMetadataProvider
    {
        private readonly SwitchLocalRomInfo romInfo;

        public override List<MetadataField> AvailableFields { get; }

        public SwitchLocalMetadataProvider(Game game, SwitchLocalMetadataSettings settings)
        {
            romInfo = SwitchGamePathResolver.Resolve(game)
                .Select(path => SwitchLocalRomReader.TryRead(path, settings))
                .FirstOrDefault(info => info != null);

            AvailableFields = romInfo == null
                ? new List<MetadataField>()
                : new List<MetadataField>
                {
                    MetadataField.Name,
                    MetadataField.Developers,
                    MetadataField.Publishers,
                    MetadataField.Platform,
                    MetadataField.Links,
                    MetadataField.Icon,
                    MetadataField.CoverImage,
                    MetadataField.InstallSize
                };
        }

        // 返回 NACP 标题。
        public override string GetName(GetMetadataFieldArgs args)
        {
            return romInfo?.DisplayName;
        }

        // NACP 只有厂商字段，这里同时填开发商，便于 Playnite 展示。
        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            if (!string.IsNullOrWhiteSpace(romInfo?.Publisher))
            {
                yield return new MetadataNameProperty(romInfo.Publisher);
            }
        }

        // NACP 只有厂商字段，这里填入发行商。
        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            if (!string.IsNullOrWhiteSpace(romInfo?.Publisher))
            {
                yield return new MetadataNameProperty(romInfo.Publisher);
            }
        }

        // 固定标记为 Nintendo Switch 平台。
        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            if (romInfo != null)
            {
                yield return new MetadataSpecProperty("nintendo_switch");
            }
        }

        // 把 Title ID 放进链接字段，方便在 Playnite 详情里查看。
        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            if (!string.IsNullOrEmpty(romInfo?.TitleId))
            {
                yield return new Link("Switch Title ID", romInfo.TitleId);
            }
        }

        // 使用 control 图标作为图标。
        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            return romInfo?.ToMetadataFile();
        }

        // 使用 control 图标作为封面。
        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            return romInfo?.ToMetadataFile();
        }

        // 安装大小用 ROM 文件大小表达。
        public override ulong? GetInstallSize(GetMetadataFieldArgs args)
        {
            return romInfo == null ? null : (ulong?)romInfo.FileSize;
        }
    }
}
