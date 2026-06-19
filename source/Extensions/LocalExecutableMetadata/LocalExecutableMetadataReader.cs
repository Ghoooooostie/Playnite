using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace LocalExecutableMetadata
{
    // 读取 Windows exe 和同目录常见本地元数据文件。
    public static class LocalExecutableMetadataReader
    {
        public static LocalExecutableGameInfo TryRead(string executablePath)
        {
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                return null;
            }

            var installDirectory = Path.GetDirectoryName(executablePath);
            var version = FileVersionInfo.GetVersionInfo(executablePath);
            var appInfo = ReadUnityAppInfo(installDirectory, executablePath);
            var result = new LocalExecutableGameInfo
            {
                Name = FirstText(appInfo.GameName, version.ProductName, version.FileDescription, Path.GetFileNameWithoutExtension(executablePath)),
                Company = FirstText(appInfo.CompanyName, version.CompanyName),
                SteamAppId = ReadSteamAppId(installDirectory),
                ExecutablePath = executablePath,
                InstallDirectory = installDirectory,
                InstallSize = CalculateDirectorySize(installDirectory)
            };

            var icon = ExtractIcon(executablePath);
            result.IconFileName = icon.FileName;
            result.IconBytes = icon.Bytes;
            return string.IsNullOrWhiteSpace(result.Name) && result.IconBytes == null ? null : result;
        }

        // Unity app.info 第一行是厂商，第二行是游戏名。
        private static UnityAppInfo ReadUnityAppInfo(string installDirectory, string executablePath)
        {
            if (string.IsNullOrWhiteSpace(installDirectory))
            {
                return new UnityAppInfo();
            }

            var dataDir = Path.Combine(installDirectory, Path.GetFileNameWithoutExtension(executablePath) + "_Data");
            var appInfoPath = Path.Combine(dataDir, "app.info");
            if (!File.Exists(appInfoPath))
            {
                return new UnityAppInfo();
            }

            var lines = File.ReadAllLines(appInfoPath)
                .Select(line => line?.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            return lines.Length >= 2
                ? new UnityAppInfo { CompanyName = lines[0], GameName = lines[1] }
                : new UnityAppInfo();
        }

        // 读取 Steam 模拟器或本地配置里的 AppId。
        private static string ReadSteamAppId(string installDirectory)
        {
            if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory))
            {
                return null;
            }

            var steamAppIdPath = Path.Combine(installDirectory, "steam_appid.txt");
            if (File.Exists(steamAppIdPath))
            {
                var value = File.ReadAllLines(steamAppIdPath).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))?.Trim();
                if (IsNumeric(value))
                {
                    return value;
                }
            }

            foreach (var iniPath in Directory.GetFiles(installDirectory, "*.ini", SearchOption.AllDirectories))
            {
                foreach (var line in File.ReadLines(iniPath))
                {
                    var trimmed = line.Trim();
                    if (!trimmed.StartsWith("AppId=", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value = trimmed.Substring("AppId=".Length).Trim();
                    if (IsNumeric(value))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        // 从 exe 关联图标导出 PNG 字节。
        private static IconData ExtractIcon(string executablePath)
        {
            using (var icon = Icon.ExtractAssociatedIcon(executablePath))
            {
                if (icon == null)
                {
                    return new IconData();
                }

                using (var bitmap = icon.ToBitmap())
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return new IconData
                    {
                        FileName = Path.GetFileNameWithoutExtension(executablePath) + ".png",
                        Bytes = stream.ToArray()
                    };
                }
            }
        }

        private static ulong CalculateDirectorySize(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return 0;
            }

            ulong total = 0;
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                total += (ulong)new FileInfo(file).Length;
            }

            return total;
        }

        private static string FirstText(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
        }

        private static bool IsNumeric(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);
        }

        private sealed class UnityAppInfo
        {
            public string CompanyName { get; set; }

            public string GameName { get; set; }
        }

        private sealed class IconData
        {
            public string FileName { get; set; }

            public byte[] Bytes { get; set; }
        }
    }
}
