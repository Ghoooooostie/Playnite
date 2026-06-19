using System;
using System.IO;
using System.Text;

namespace SwitchLocalMetadata
{
    // 解析 Nintendo Switch control.nacp 中的标题、厂商和 Title ID。
    public static class NacpReader
    {
        private const int LanguageEntrySize = 0x300;
        private const int NameSize = 0x200;
        private const int PublisherSize = 0x100;
        private const int LanguageCount = 16;
        private const int TitleIdOffset = 0x3070;
        private static readonly int[] PreferredLanguages = { 2, 1, 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

        public static NacpInfo Read(string path)
        {
            var data = File.ReadAllBytes(path);
            if (data.Length < LanguageEntrySize * LanguageCount)
            {
                throw new InvalidDataException("control.nacp 文件太小。");
            }

            foreach (var language in PreferredLanguages)
            {
                var baseOffset = language * LanguageEntrySize;
                var name = ReadNullTerminatedUtf8(data, baseOffset, NameSize);
                var publisher = ReadNullTerminatedUtf8(data, baseOffset + NameSize, PublisherSize);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return new NacpInfo(name, publisher, ReadTitleId(data));
                }
            }

            return new NacpInfo(null, null, ReadTitleId(data));
        }

        private static string ReadTitleId(byte[] data)
        {
            if (data.Length < TitleIdOffset + 8)
            {
                return null;
            }

            var value = BitConverter.ToUInt64(data, TitleIdOffset);
            return value == 0 ? null : value.ToString("X16");
        }

        private static string ReadNullTerminatedUtf8(byte[] data, int offset, int length)
        {
            var end = offset;
            var max = Math.Min(offset + length, data.Length);
            while (end < max && data[end] != 0)
            {
                end++;
            }

            return end == offset ? null : Encoding.UTF8.GetString(data, offset, end - offset).Trim();
        }
    }

    // NACP 解析结果。
    public sealed class NacpInfo
    {
        public string Name { get; private set; }
        public string Publisher { get; private set; }
        public string TitleId { get; private set; }

        public NacpInfo(string name, string publisher, string titleId)
        {
            Name = name;
            Publisher = publisher;
            TitleId = titleId;
        }
    }
}
