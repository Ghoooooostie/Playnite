// 文件用途：负责把截图文件按游戏 ID 写入和读取。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GameScreenshots
{
    // 以文件系统为唯一索引，避免维护额外数据库。
    public class ScreenshotStore : IScreenshotStore
    {
        private const string MetadataFileName = "game.json";
        private readonly string screenshotsRoot;

        public ScreenshotStore(string screenshotsRoot)
        {
            if (string.IsNullOrWhiteSpace(screenshotsRoot))
            {
                throw new ArgumentException("截图目录不能为空。", "screenshotsRoot");
            }

            this.screenshotsRoot = screenshotsRoot;
        }

        // 保存一张截图并更新游戏元数据。
        public ScreenshotItem SaveScreenshot(Guid gameId, string gameName, byte[] pngBytes, DateTime capturedAt)
        {
            if (gameId == Guid.Empty)
            {
                throw new ArgumentException("游戏 ID 不能为空。", "gameId");
            }

            if (pngBytes == null || pngBytes.Length == 0)
            {
                throw new ArgumentException("截图内容不能为空。", "pngBytes");
            }

            var directory = GetGameDirectory(gameId);
            Directory.CreateDirectory(directory);
            SaveMetadata(directory, gameId, gameName);

            var fileName = BuildAvailableFileName(directory, capturedAt);
            var filePath = Path.Combine(directory, fileName);
            File.WriteAllBytes(filePath, pngBytes);

            return CreateItem(gameId, gameName, filePath, capturedAt);
        }

        // 读取某个游戏的截图。
        public IEnumerable<ScreenshotItem> LoadGameScreenshots(Guid gameId)
        {
            var directory = GetGameDirectory(gameId);
            if (!Directory.Exists(directory))
            {
                return new List<ScreenshotItem>();
            }

            var metadata = LoadMetadata(directory, gameId);
            return LoadItemsFromDirectory(directory, metadata)
                .OrderByDescending(a => a.CapturedAt)
                .ToList();
        }

        // 读取所有游戏的截图。
        public IEnumerable<ScreenshotItem> LoadAllScreenshots()
        {
            if (!Directory.Exists(screenshotsRoot))
            {
                return new List<ScreenshotItem>();
            }

            var items = new List<ScreenshotItem>();
            foreach (var directory in Directory.GetDirectories(screenshotsRoot))
            {
                Guid gameId;
                if (!Guid.TryParse(Path.GetFileName(directory), out gameId))
                {
                    throw new InvalidDataException("截图目录名不是有效游戏 ID：" + directory);
                }

                var metadata = LoadMetadata(directory, gameId);
                items.AddRange(LoadItemsFromDirectory(directory, metadata));
            }

            return items.OrderByDescending(a => a.CapturedAt).ToList();
        }

        // 生成游戏截图目录。
        private string GetGameDirectory(Guid gameId)
        {
            return Path.Combine(screenshotsRoot, gameId.ToString());
        }

        // 写入游戏元数据。
        private void SaveMetadata(string directory, Guid gameId, string gameName)
        {
            var metadata = new ScreenshotGameMetadata
            {
                GameId = gameId,
                GameName = string.IsNullOrWhiteSpace(gameName) ? gameId.ToString() : gameName
            };

            var serializer = new DataContractJsonSerializer(typeof(ScreenshotGameMetadata));
            using (var stream = File.Create(Path.Combine(directory, MetadataFileName)))
            {
                serializer.WriteObject(stream, metadata);
            }
        }

        // 读取游戏元数据。
        private ScreenshotGameMetadata LoadMetadata(string directory, Guid gameId)
        {
            var metadataPath = Path.Combine(directory, MetadataFileName);
            if (!File.Exists(metadataPath))
            {
                throw new InvalidDataException("截图目录缺少游戏元数据：" + metadataPath);
            }

            var serializer = new DataContractJsonSerializer(typeof(ScreenshotGameMetadata));
            ScreenshotGameMetadata metadata;
            using (var stream = File.OpenRead(metadataPath))
            {
                metadata = serializer.ReadObject(stream) as ScreenshotGameMetadata;
            }

            if (metadata == null || metadata.GameId != gameId)
            {
                throw new InvalidDataException("截图游戏元数据与目录不匹配：" + metadataPath);
            }

            return metadata;
        }

        // 从游戏目录加载截图项。
        private IEnumerable<ScreenshotItem> LoadItemsFromDirectory(string directory, ScreenshotGameMetadata metadata)
        {
            foreach (var filePath in Directory.GetFiles(directory, "*.png"))
            {
                yield return CreateItem(metadata.GameId, metadata.GameName, filePath, ParseCapturedAt(filePath));
            }
        }

        // 生成没有冲突的截图文件名。
        private string BuildAvailableFileName(string directory, DateTime capturedAt)
        {
            var stamp = capturedAt.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var fileName = stamp + ".png";
            if (!File.Exists(Path.Combine(directory, fileName)))
            {
                return fileName;
            }

            for (var index = 1; index < 1000; index++)
            {
                fileName = stamp + "-" + index.ToString("000", CultureInfo.InvariantCulture) + ".png";
                if (!File.Exists(Path.Combine(directory, fileName)))
                {
                    return fileName;
                }
            }

            throw new IOException("同一秒内截图数量过多，无法生成文件名。");
        }

        // 从文件名解析截图时间。
        private static DateTime ParseCapturedAt(string filePath)
        {
            var name = Path.GetFileNameWithoutExtension(filePath);
            if (name.Length > 15)
            {
                name = name.Substring(0, 15);
            }

            DateTime capturedAt;
            if (!DateTime.TryParseExact(name, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out capturedAt))
            {
                throw new InvalidDataException("截图文件名不符合时间格式：" + filePath);
            }

            return capturedAt;
        }

        // 创建截图项。
        private static ScreenshotItem CreateItem(Guid gameId, string gameName, string filePath, DateTime capturedAt)
        {
            return new ScreenshotItem
            {
                GameId = gameId,
                GameName = gameName,
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                CapturedAt = capturedAt
            };
        }
    }
}
