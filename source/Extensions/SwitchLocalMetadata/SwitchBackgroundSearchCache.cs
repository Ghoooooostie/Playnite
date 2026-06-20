using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SwitchLocalMetadata
{
    // 保存联网背景搜索结果，避免同一游戏重复请求。
    public sealed class SwitchBackgroundSearchCache
    {
        private readonly string cachePath;
        private readonly Dictionary<string, SwitchBackgroundCacheEntry> entries;

        public SwitchBackgroundSearchCache(string pluginUserDataPath)
        {
            var safeDirectory = string.IsNullOrWhiteSpace(pluginUserDataPath)
                ? Path.Combine(Path.GetTempPath(), "SwitchLocalMetadata")
                : pluginUserDataPath;
            Directory.CreateDirectory(safeDirectory);
            cachePath = Path.Combine(safeDirectory, "background-search-cache.json");
            entries = Load(cachePath);
        }

        // 读取缓存条目。
        public bool TryGet(string key, out SwitchBackgroundCacheEntry entry)
        {
            return entries.TryGetValue(NormalizeKey(key), out entry);
        }

        // 写入缓存条目并立刻落盘。
        public void Set(string key, SwitchBackgroundCacheEntry entry)
        {
            entries[NormalizeKey(key)] = entry;
            Save();
        }

        private void Save()
        {
            var lines = entries.Select(pair => string.Join("\t", new[]
            {
                Escape(pair.Key),
                Escape(pair.Value?.ImageUrl),
                Escape(pair.Value?.SourceUrl),
                pair.Value != null && pair.Value.NotFound ? "1" : "0",
                pair.Value == null ? string.Empty : pair.Value.CachedAt.Ticks.ToString()
            }));
            File.WriteAllLines(cachePath, lines);
        }

        private static Dictionary<string, SwitchBackgroundCacheEntry> Load(string path)
        {
            try
            {
                return File.Exists(path)
                    ? ReadRecords(path)
                    : new Dictionary<string, SwitchBackgroundCacheEntry>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, SwitchBackgroundCacheEntry>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToUpperInvariant();
        }

        private static Dictionary<string, SwitchBackgroundCacheEntry> ReadRecords(string path)
        {
            var result = new Dictionary<string, SwitchBackgroundCacheEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path))
            {
                var parts = line.Split(new[] { '\t' }, StringSplitOptions.None);
                if (parts.Length < 5)
                {
                    continue;
                }

                result[Unescape(parts[0])] = new SwitchBackgroundCacheEntry
                {
                    ImageUrl = Unescape(parts[1]),
                    SourceUrl = Unescape(parts[2]),
                    NotFound = parts[3] == "1",
                    CachedAt = long.TryParse(parts[4], out var ticks) ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue
                };
            }

            return result;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", string.Empty).Replace("\n", "\\n");
        }

        private static string Unescape(string value)
        {
            return (value ?? string.Empty).Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");
        }
    }

    // 单条缓存记录。
    public sealed class SwitchBackgroundCacheEntry
    {
        public string ImageUrl { get; set; }

        public string SourceUrl { get; set; }

        public bool NotFound { get; set; }

        public DateTime CachedAt { get; set; }
    }
}
