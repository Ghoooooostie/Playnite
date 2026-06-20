using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace SwitchLocalMetadata
{
    // 按标题自动搜索可用背景图。
    public sealed class SwitchBackgroundSearchService
    {
        private static readonly string[] PreferredDomains =
        {
            "otomate.jp",
            "prot.co.jp",
            "dramaticcreate.com"
        };

        private static readonly Regex OGPImageRegex = new Regex("<meta[^>]+property=[\"']og:image[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex OGPWidthRegex = new Regex("<meta[^>]+property=[\"']og:image:width[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex OGPHeightRegex = new Regex("<meta[^>]+property=[\"']og:image:height[\"'][^>]+content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BingResultRegex = new Regex("<a\\s+href=\"(https?://[^\"]+)\"\\s+h=\"ID=", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly SwitchLocalMetadataSettings settings;
        private readonly SwitchBackgroundSearchCache cache;
        private readonly IWebDownloader downloader;

        public SwitchBackgroundSearchService(SwitchLocalMetadataSettings settings, SwitchBackgroundSearchCache cache, IWebDownloader downloader)
        {
            this.settings = settings;
            this.cache = cache;
            this.downloader = downloader;
        }

        // 返回远程背景图，没有命中时返回空。
        public MetadataFile TryGetBackgroundImage(SwitchLocalRomInfo romInfo)
        {
            if (romInfo == null || !settings.EnableOnlineBackgroundSearch)
            {
                return null;
            }

            var cacheKey = !string.IsNullOrWhiteSpace(romInfo.TitleId) ? romInfo.TitleId : romInfo.DisplayName;
            if (cache.TryGet(cacheKey, out var cached))
            {
                return cached.NotFound || string.IsNullOrWhiteSpace(cached.ImageUrl)
                    ? null
                    : new MetadataFile(cached.ImageUrl);
            }

            foreach (var query in SwitchTitleSearchNormalizer.BuildSearchQueries(romInfo.DisplayName))
            {
                var sourceUrl = FindOfficialPage(query) ?? FindSteamGridDbPage(query);
                if (string.IsNullOrWhiteSpace(sourceUrl))
                {
                    continue;
                }

                var imageUrl = ExtractOgImage(sourceUrl);
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    cache.Set(cacheKey, new SwitchBackgroundCacheEntry
                    {
                        ImageUrl = imageUrl,
                        SourceUrl = sourceUrl,
                        CachedAt = DateTime.UtcNow
                    });
                    return new MetadataFile(imageUrl);
                }
            }

            cache.Set(cacheKey, new SwitchBackgroundCacheEntry
            {
                NotFound = true,
                CachedAt = DateTime.UtcNow
            });
            return null;
        }

        private string FindOfficialPage(string query)
        {
            var result = SearchPage(query + " Nintendo Switch");
            return result.FirstOrDefault(IsPreferredDomain);
        }

        private string FindSteamGridDbPage(string query)
        {
            var result = SearchPage(query + " site:steamgriddb.com");
            return result
                .Select(ToHeroesPage)
                .FirstOrDefault(url => url.IndexOf("steamgriddb.com/game/", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private IEnumerable<string> SearchPage(string query)
        {
            try
            {
                var url = "https://www.bing.com/search?q=" + Uri.EscapeDataString(query);
                var html = downloader.DownloadString(url);
                return BingResultRegex.Matches(html ?? string.Empty)
                    .Cast<Match>()
                    .Select(match => WebUtility.HtmlDecode(match.Groups[1].Value))
                    .Where(link => !string.IsNullOrWhiteSpace(link))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private string ExtractOgImage(string pageUrl)
        {
            try
            {
                var html = downloader.DownloadString(pageUrl);
                var match = OGPImageRegex.Match(html ?? string.Empty);
                if (!match.Success)
                {
                    return null;
                }

                if (!IsLandscapeImage(html))
                {
                    return null;
                }

                return WebUtility.HtmlDecode(match.Groups[1].Value);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsPreferredDomain(string url)
        {
            return PreferredDomains.Any(domain => url.IndexOf(domain, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsLandscapeImage(string html)
        {
            var width = ReadDimension(OGPWidthRegex, html);
            var height = ReadDimension(OGPHeightRegex, html);
            return width > height && height > 0;
        }

        private static int ReadDimension(Regex regex, string html)
        {
            var match = regex.Match(html ?? string.Empty);
            return match.Success && int.TryParse(match.Groups[1].Value, out var value) ? value : 0;
        }

        private static string ToHeroesPage(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || url.IndexOf("steamgriddb.com/game/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return url;
            }

            if (url.IndexOf("/heroes", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return url;
            }

            return url.TrimEnd('/') + "/heroes";
        }
    }

    // 统一网页下载，便于测试替换。
    public interface IWebDownloader
    {
        string DownloadString(string url);
    }

    // 默认网页下载实现。
    public sealed class WebClientDownloader : IWebDownloader
    {
        public string DownloadString(string url)
        {
            using (var client = new WebClient())
            {
                client.Encoding = System.Text.Encoding.UTF8;
                client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0");
                return client.DownloadString(url);
            }
        }
    }
}
