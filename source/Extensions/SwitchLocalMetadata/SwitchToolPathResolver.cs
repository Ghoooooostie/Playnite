using System.IO;

namespace SwitchLocalMetadata
{
    // 自动寻找本机常见的 Switch 工具路径。
    public static class SwitchToolPathResolver
    {
        private static readonly string[] HactoolnetCandidates =
        {
            @"H:\galgame\SAK_64bit\bin\hactoolnet.exe",
            @"H:\galgame\NSCBx1.0.1b Keys20.4.0\ztools\hactoolnet.exe"
        };

        private static readonly string[] ProdKeysCandidates =
        {
            @"H:\galgame\SAK_64bit\bin\prod.keys",
            @"H:\galgame\SAK_64bit\ProdKeys.NET-21.1.0\ProdKeys.NET-21.1.0\prod.keys"
        };

        private static readonly string[] TitleKeysCandidates =
        {
            @"H:\galgame\SAK_64bit\bin\title.keys",
            @"H:\galgame\SAK_64bit\ProdKeys.NET-21.1.0\ProdKeys.NET-21.1.0\title.keys"
        };

        public static SwitchLocalMetadataSettings ResolveDefaults()
        {
            return new SwitchLocalMetadataSettings
            {
                HactoolnetPath = FirstExisting(HactoolnetCandidates),
                ProdKeysPath = FirstExisting(ProdKeysCandidates),
                TitleKeysPath = FirstExisting(TitleKeysCandidates)
            };
        }

        private static string FirstExisting(string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }
    }
}
