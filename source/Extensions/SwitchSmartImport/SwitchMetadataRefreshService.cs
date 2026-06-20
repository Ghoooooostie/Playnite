// 文件用途：按设置调用 Switch Local Metadata 对导入游戏做全量资料刷新。
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SwitchSmartImport
{
    // Switch 资料刷新服务接口。
    public interface ISwitchMetadataRefreshService
    {
        void Refresh(IEnumerable<Game> games, SwitchMetadataSource source);
    }

    // Switch 导入后资料刷新服务。
    public class SwitchMetadataRefreshService : ISwitchMetadataRefreshService
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI api;

        public SwitchMetadataRefreshService(IPlayniteAPI api)
        {
            this.api = api ?? throw new ArgumentNullException("api");
        }

        // 按配置刷新指定游戏。
        public void Refresh(IEnumerable<Game> games, SwitchMetadataSource source)
        {
            if (source == SwitchMetadataSource.None || games == null)
            {
                return;
            }

            var metadataPlugin = ResolveMetadataPlugin(source);
            foreach (var game in games.Where(a => a != null))
            {
                try
                {
                    RefreshGame(game, metadataPlugin);
                    api.Database.Games.Update(game);
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Switch Smart Import metadata refresh failed for {game.Name}, {game.Id}");
                }
            }
        }

        // 查找配置要求的元数据插件。
        private MetadataPlugin ResolveMetadataPlugin(SwitchMetadataSource source)
        {
            if (source != SwitchMetadataSource.SwitchLocalMetadata)
            {
                throw new InvalidOperationException("不支持的元数据来源。");
            }

            var plugin = api.Addons?.Plugins?
                .OfType<MetadataPlugin>()
                .FirstOrDefault(a => string.Equals(a.Name, "Switch Local Metadata", StringComparison.Ordinal));

            if (plugin == null)
            {
                throw new InvalidOperationException("未找到 Switch Local Metadata 插件。");
            }

            return plugin;
        }

        // 对单个游戏做一次全量覆盖刷新。
        private void RefreshGame(Game game, MetadataPlugin plugin)
        {
            using (var provider = plugin.GetMetadataProvider(new MetadataRequestOptions(game, true)))
            {
                if (provider == null)
                {
                    return;
                }

                var args = new GetMetadataFieldArgs();
                var name = provider.GetName(args);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    game.Name = name;
                }

                var developers = provider.GetDevelopers(args)?.ToList();
                if (developers?.Count > 0)
                {
                    game.DeveloperIds = api.Database.Companies.Add(developers).Select(a => a.Id).ToList();
                }

                var publishers = provider.GetPublishers(args)?.ToList();
                if (publishers?.Count > 0)
                {
                    game.PublisherIds = api.Database.Companies.Add(publishers).Select(a => a.Id).ToList();
                }

                var platforms = provider.GetPlatforms(args)?.ToList();
                if (platforms?.Count > 0)
                {
                    game.PlatformIds = api.Database.Platforms.Add(platforms).Select(a => a.Id).ToList();
                }

                var links = provider.GetLinks(args)?.ToList();
                if (links?.Count > 0)
                {
                    game.Links = new ObservableCollection<Link>(links);
                }

                var icon = SaveMetadataFile(provider.GetIcon(args), game.Id);
                if (!string.IsNullOrWhiteSpace(icon))
                {
                    game.Icon = icon;
                }

                var cover = SaveMetadataFile(provider.GetCoverImage(args), game.Id);
                if (!string.IsNullOrWhiteSpace(cover))
                {
                    game.CoverImage = cover;
                }

                var background = SaveMetadataFile(provider.GetBackgroundImage(args), game.Id);
                if (!string.IsNullOrWhiteSpace(background))
                {
                    game.BackgroundImage = background;
                }

                var installSize = provider.GetInstallSize(args);
                if (installSize != null)
                {
                    game.InstallSize = installSize;
                }
            }
        }

        // 把元数据文件转成数据库可接受的本地路径。
        private string SaveMetadataFile(MetadataFile file, Guid gameId)
        {
            if (file == null || !file.HasImageData)
            {
                return null;
            }

            if (file.HasContent)
            {
                var extension = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".bin";
                }

                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
                File.WriteAllBytes(tempPath, file.Content);
                return api.Database.AddFile(tempPath, gameId);
            }

            if (!string.IsNullOrWhiteSpace(file.Path))
            {
                return api.Database.AddFile(file.Path, gameId);
            }

            return null;
        }
    }
}
