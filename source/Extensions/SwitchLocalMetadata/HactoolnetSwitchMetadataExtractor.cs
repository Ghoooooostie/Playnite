using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SwitchLocalMetadata
{
    // 调用 hactoolnet 定位 control NCA，再读取 NACP 与图标。
    public sealed class HactoolnetSwitchMetadataExtractor
    {
        private const long MaxControlNcaSize = 64 * 1024 * 1024;
        private static readonly Regex XciNcaRegex = new Regex(@"secure:/([^\s]+\.nca)\s+([0-9A-Fa-f]+)-([0-9A-Fa-f]+)", RegexOptions.Compiled);
        private static readonly Regex NspNcaRegex = new Regex(@"pfs0:/([^\s]+?\.nca)([0-9A-Fa-f]+)-([0-9A-Fa-f]+)", RegexOptions.Compiled);
        private static readonly Regex RootPartitionOffsetRegex = new Regex(@"Root Partition:[\s\S]*?Offset:\s+([0-9A-Fa-f]+)", RegexOptions.Compiled);
        private static readonly Regex RootSecureRegex = new Regex(@"root:/secure\s+([0-9A-Fa-f]+)-([0-9A-Fa-f]+)", RegexOptions.Compiled);
        private readonly SwitchLocalMetadataSettings settings;

        public HactoolnetSwitchMetadataExtractor(SwitchLocalMetadataSettings settings)
        {
            this.settings = settings;
        }

        public SwitchLocalRomInfo TryRead(string path)
        {
            if (!IsValidInput(path) || !IsValidSettings())
            {
                return null;
            }

            var workDir = Path.Combine(Path.GetTempPath(), "SwitchLocalMetadata", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDir);
            try
            {
                var contentDir = ExtractSmallNcas(path, workDir);
                var control = FindControlNca(contentDir);
                if (control == null)
                {
                    return null;
                }

                var romfsDir = Path.Combine(workDir, "control");
                Directory.CreateDirectory(romfsDir);
                RunHactoolnet("-t nca --romfsdir " + Quote(romfsDir) + " " + Quote(control.Path));

                return ReadControlData(path, romfsDir, control.TitleId, new FileInfo(path).Length);
            }
            finally
            {
                TryDeleteDirectory(workDir);
            }
        }

        private string ExtractSmallNcas(string path, string workDir)
        {
            var extension = Path.GetExtension(path);
            if (extension.Equals(".xci", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractSmallXciNcas(path, workDir);
            }

            if (extension.Equals(".nsp", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractSmallNspNcas(path, workDir);
            }

            throw new InvalidDataException("不支持的 Switch 文件类型。" + extension);
        }

        private string ExtractSmallXciNcas(string path, string workDir)
        {
            var output = RunHactoolnet("-t xci " + Quote(path));
            var contentDir = Path.Combine(workDir, "content");
            Directory.CreateDirectory(contentDir);
            var fileBaseOffset = CalculateXciSecureDataOffset(path, output);

            foreach (var entry in ParseXciNcaEntries(output).Where(entry => entry.Length <= MaxControlNcaSize))
            {
                CopyRange(path, Path.Combine(contentDir, entry.Name), fileBaseOffset + entry.Start, entry.Length);
            }

            return contentDir;
        }

        private string ExtractSmallNspNcas(string path, string workDir)
        {
            var output = RunHactoolnet("-t pfs0 " + Quote(path));
            var contentDir = Path.Combine(workDir, "content");
            Directory.CreateDirectory(contentDir);
            var fileBaseOffset = ReadPfs0HeaderSize(path);

            foreach (var entry in ParseNspNcaEntries(output).Where(entry => entry.Length <= MaxControlNcaSize))
            {
                CopyRange(path, Path.Combine(contentDir, entry.Name), fileBaseOffset + entry.Start, entry.Length);
            }

            return contentDir;
        }

        private bool IsValidInput(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            var ext = Path.GetExtension(path);
            return ext.Equals(".xci", StringComparison.OrdinalIgnoreCase) || ext.Equals(".nsp", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsValidSettings()
        {
            return settings != null && File.Exists(settings.HactoolnetPath) && File.Exists(settings.ProdKeysPath);
        }

        private ControlNca FindControlNca(string contentDir)
        {
            foreach (var file in Directory.GetFiles(contentDir, "*.nca").Select(path => new FileInfo(path)).Where(file => file.Length <= MaxControlNcaSize).OrderBy(file => file.Length))
            {
                var output = RunHactoolnet("-t nca " + Quote(file.FullName));
                if (Regex.IsMatch(output, @"Content Type:\s+Control", RegexOptions.IgnoreCase))
                {
                    return new ControlNca(file.FullName, ExtractTitleIdFromHactoolOutput(output));
                }
            }

            return null;
        }

        private SwitchLocalRomInfo ReadControlData(string sourcePath, string romfsDir, string controlTitleId, long fileSize)
        {
            var nacpPath = Path.Combine(romfsDir, "control.nacp");
            if (!File.Exists(nacpPath))
            {
                return null;
            }

            var nacp = NacpReader.Read(nacpPath);
            var iconPath = Directory.GetFiles(romfsDir, "icon_*.dat").OrderByDescending(path => new FileInfo(path).Length).FirstOrDefault();
            if (iconPath == null)
            {
                return null;
            }

            var titleId = controlTitleId ?? SwitchLocalRomReader.ExtractTitleId(sourcePath);
            return new SwitchLocalRomInfo(
                sourcePath,
                titleId,
                nacp.Name ?? SwitchLocalRomReader.ExtractDisplayName(sourcePath, titleId),
                nacp.Publisher,
                Path.GetFileNameWithoutExtension(iconPath) + ".jpg",
                File.ReadAllBytes(iconPath),
                fileSize);
        }

        // 计算 XCI secure 分区内文件数据的起始偏移。
        internal static long CalculateXciSecureDataOffset(string path, string output)
        {
            var rootSecureOffset = ExtractRootSecureOffset(output);
            var rootPartitionOffset = ExtractRootPartitionOffset(output);
            var rootDataOffset = rootPartitionOffset + ReadHfs0HeaderSize(path, rootPartitionOffset);
            var securePartitionOffset = rootDataOffset + rootSecureOffset;
            return securePartitionOffset + ReadHfs0HeaderSize(path, securePartitionOffset);
        }

        private static IEnumerable<NcaContainerEntry> ParseXciNcaEntries(string output)
        {
            foreach (Match match in XciNcaRegex.Matches(output ?? string.Empty))
            {
                var start = long.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var end = long.Parse(match.Groups[3].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                yield return new NcaContainerEntry(match.Groups[1].Value, start, end - start);
            }
        }

        private static IEnumerable<NcaContainerEntry> ParseNspNcaEntries(string output)
        {
            foreach (Match match in NspNcaRegex.Matches(output ?? string.Empty))
            {
                var start = long.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var end = long.Parse(match.Groups[3].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                yield return new NcaContainerEntry(match.Groups[1].Value, start, end - start);
            }
        }

        private static long ExtractRootSecureOffset(string output)
        {
            var match = RootSecureRegex.Match(output ?? string.Empty);
            if (!match.Success)
            {
                throw new InvalidDataException("无法解析 XCI secure 分区偏移。");
            }

            return long.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        // 从 hactoolnet 输出中解析 XCI 根分区的起始偏移。
        private static long ExtractRootPartitionOffset(string output)
        {
            var match = RootPartitionOffsetRegex.Match(output ?? string.Empty);
            if (!match.Success)
            {
                throw new InvalidDataException("无法解析 XCI root 分区偏移。");
            }

            return long.Parse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private static long ReadPfs0HeaderSize(string sourcePath)
        {
            var header = new byte[16];
            using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var read = stream.Read(header, 0, header.Length);
                if (read != header.Length || Encoding.ASCII.GetString(header, 0, 4) != "PFS0")
                {
                    throw new InvalidDataException("无法读取 NSP PFS0 头。");
                }
            }

            var fileCount = BitConverter.ToUInt32(header, 4);
            var stringTableSize = BitConverter.ToUInt32(header, 8);
            return 0x10 + fileCount * 0x18 + stringTableSize;
        }

        private static long ReadHfs0HeaderSize(string sourcePath, long partitionOffset)
        {
            var header = new byte[16];
            using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Position = partitionOffset;
                var read = stream.Read(header, 0, header.Length);
                if (read != header.Length || Encoding.ASCII.GetString(header, 0, 4) != "HFS0")
                {
                    throw new InvalidDataException("无法读取 XCI secure HFS0 头。");
                }
            }

            var fileCount = BitConverter.ToUInt32(header, 4);
            var stringTableSize = BitConverter.ToUInt32(header, 8);
            return 0x10 + fileCount * 0x40 + stringTableSize;
        }

        private static void CopyRange(string sourcePath, string targetPath, long offset, long length)
        {
            var buffer = new byte[1024 * 1024];
            using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var output = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                input.Position = offset;
                var remaining = length;
                while (remaining > 0)
                {
                    var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read <= 0)
                    {
                        throw new EndOfStreamException("复制 NCA 时读到文件末尾。");
                    }

                    output.Write(buffer, 0, read);
                    remaining -= read;
                }
            }
        }

        private static string ExtractTitleIdFromHactoolOutput(string output)
        {
            var match = Regex.Match(output ?? string.Empty, @"TitleID:\s*([A-Fa-f0-9]{16})");
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
        }

        private string RunHactoolnet(string arguments)
        {
            var fullArguments = "-k " + Quote(settings.ProdKeysPath) + GetTitleKeysArgument() + " " + arguments;
            var startInfo = new ProcessStartInfo
            {
                FileName = settings.HactoolnetPath,
                Arguments = fullArguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = Process.Start(startInfo))
            {
                var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("hactoolnet 读取失败。" + Environment.NewLine + output);
                }

                return output;
            }
        }

        private string GetTitleKeysArgument()
        {
            return string.IsNullOrWhiteSpace(settings.TitleKeysPath) || !File.Exists(settings.TitleKeysPath)
                ? string.Empty
                : " --titlekeys " + Quote(settings.TitleKeysPath);
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }

        private static void TryDeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private sealed class ControlNca
        {
            public string Path { get; private set; }
            public string TitleId { get; private set; }

            public ControlNca(string path, string titleId)
            {
                Path = path;
                TitleId = titleId;
            }
        }

        private sealed class NcaContainerEntry
        {
            public string Name { get; private set; }
            public long Start { get; private set; }
            public long Length { get; private set; }

            public NcaContainerEntry(string name, long start, long length)
            {
                Name = name;
                Start = start;
                Length = length;
            }
        }
    }
}
