using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LocalExecutableMetadata
{
    // Playnite 元数据插件入口，负责创建本地 Windows exe Provider。
    public class LocalExecutableMetadataPlugin : MetadataPlugin
    {
        public override string Name => "Local Executable Metadata";

        public override Guid Id { get; } = Guid.Parse("2B76AB8B-5E61-4B92-84CB-23F3739346CB");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Platform,
            MetadataField.Links,
            MetadataField.Icon,
            MetadataField.InstallSize
        };

        public LocalExecutableMetadataPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new MetadataPluginProperties
            {
                HasSettings = false
            };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new LocalExecutableMetadataProvider(options.GameData);
        }
    }

    // 按需返回单个 Windows exe 游戏的本地元数据。
    public class LocalExecutableMetadataProvider : OnDemandMetadataProvider
    {
        private readonly LocalExecutableGameInfo gameInfo;

        public override List<MetadataField> AvailableFields { get; }

        public LocalExecutableMetadataProvider(Game game)
        {
            gameInfo = LocalExecutablePathResolver.Resolve(game)
                .Select(LocalExecutableMetadataReader.TryRead)
                .FirstOrDefault(info => info != null);

            AvailableFields = gameInfo == null
                ? new List<MetadataField>()
                : new List<MetadataField>
                {
                    MetadataField.Name,
                    MetadataField.Developers,
                    MetadataField.Publishers,
                    MetadataField.Platform,
                    MetadataField.Links,
                    MetadataField.Icon,
                    MetadataField.InstallSize
                };
        }

        // 返回本地读取到的游戏名。
        public override string GetName(GetMetadataFieldArgs args)
        {
            return gameInfo?.Name;
        }

        // 本地目录通常只有公司名，这里同时填开发商。
        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            if (!string.IsNullOrWhiteSpace(gameInfo?.Company))
            {
                yield return new MetadataNameProperty(gameInfo.Company);
            }
        }

        // 本地目录通常只有公司名，这里同时填发行商。
        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            if (!string.IsNullOrWhiteSpace(gameInfo?.Company))
            {
                yield return new MetadataNameProperty(gameInfo.Company);
            }
        }

        // 固定标记为 Windows PC 平台。
        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            if (gameInfo != null)
            {
                yield return new MetadataSpecProperty("pc_windows");
            }
        }

        // 把本地识别出的 Steam AppId 放进链接字段。
        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            if (!string.IsNullOrWhiteSpace(gameInfo?.SteamAppId))
            {
                yield return new Link("Steam AppId", gameInfo.SteamAppId);
            }
        }

        // 使用 exe 关联图标。
        public override MetadataFile GetIcon(GetMetadataFieldArgs args)
        {
            return gameInfo?.ToIconFile();
        }

        // 安装大小用游戏目录大小表达。
        public override ulong? GetInstallSize(GetMetadataFieldArgs args)
        {
            return gameInfo == null ? null : (ulong?)gameInfo.InstallSize;
        }
    }
}
