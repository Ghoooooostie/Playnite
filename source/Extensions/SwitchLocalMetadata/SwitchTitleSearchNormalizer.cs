using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SwitchLocalMetadata
{
    // 生成更适合联网搜索的标题关键词。
    public static class SwitchTitleSearchNormalizer
    {
        private static readonly Regex MultiSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly string[] TrailingMarkers =
        {
            "for Nintendo Switch",
            "Nintendo Switch",
            "for Nintendo Switc",
            "通常版",
            "限定版"
        };

        // 生成搜索关键词，按从精确到宽松的顺序返回。
        public static string[] BuildSearchQueries(string title)
        {
            var result = new List<string>();
            var cleaned = CleanTitle(title);
            AddIfAny(result, cleaned);

            var noSubtitle = RemoveSubtitle(cleaned);
            AddIfAny(result, noSubtitle);

            var noBrackets = Regex.Replace(noSubtitle, @"[\[\(（【].*?[\]\)）】]", string.Empty).Trim();
            AddIfAny(result, noBrackets);

            return result
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        // 清洗标题中的平台尾巴和常见符号。
        public static string CleanTitle(string title)
        {
            var value = title ?? string.Empty;
            foreach (var marker in TrailingMarkers)
            {
                var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    value = value.Substring(0, index);
                }
            }

            value = value
                .Replace('×', 'x')
                .Replace('＊', '*')
                .Replace('〜', '～')
                .Replace('　', ' ');

            return MultiSpaceRegex.Replace(value, " ").Trim(' ', '-', '_', '～', '~');
        }

        private static string RemoveSubtitle(string title)
        {
            var separators = new[] { " ～", "〜", " -", " –", " —", ":" };
            foreach (var separator in separators)
            {
                var index = title.IndexOf(separator, StringComparison.Ordinal);
                if (index > 0)
                {
                    return title.Substring(0, index).Trim();
                }
            }

            return title;
        }

        private static void AddIfAny(List<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }
    }
}
