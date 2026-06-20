// 文件用途：把识别后的 Switch 包归并成待导入候选，并过滤补丁和 DLC。
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwitchSmartImport
{
    // Switch 候选归并器。
    public static class SwitchCandidateMerger
    {
        // 归并分类结果。
        public static SwitchCandidateMergeResult Merge(IEnumerable<SwitchPackageInfo> packages)
        {
            if (packages == null)
            {
                throw new ArgumentNullException("packages");
            }

            var result = new SwitchCandidateMergeResult();
            foreach (var group in GroupPackages(packages))
            {
                MergeGroup(group.ToList(), result);
            }

            return result;
        }

        private static void MergeGroup(List<SwitchPackageInfo> packages, SwitchCandidateMergeResult result)
        {
            foreach (var dlc in packages.Where(a => a.PackageType == SwitchPackageType.Dlc))
            {
                result.SkippedItems.Add(new SwitchSkippedItem
                {
                    Path = dlc.FilePath,
                    Reason = "跳过DLC"
                });
            }

            var basePackage = packages
                .Where(a => a.PackageType == SwitchPackageType.Base || a.PackageType == SwitchPackageType.Unknown)
                .OrderByDescending(GetBasePackageScore)
                .ThenByDescending(a => a.VersionRank)
                .ThenBy(a => a.FilePath, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            var updates = packages
                .Where(a => a.PackageType == SwitchPackageType.Update)
                .OrderByDescending(a => a.VersionRank)
                .ThenBy(a => a.FilePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (basePackage == null)
            {
                foreach (var item in updates)
                {
                    result.SkippedItems.Add(new SwitchSkippedItem
                    {
                        Path = item.FilePath,
                        Reason = "缺少本体"
                    });
                }

                return;
            }

            if (updates.Count > 1)
            {
                foreach (var item in updates.Skip(1))
                {
                    result.SkippedItems.Add(new SwitchSkippedItem
                    {
                        Path = item.FilePath,
                        Reason = "跳过低版本补丁"
                    });
                }
            }

            result.Candidates.Add(new SwitchImportCandidate
            {
                GameName = basePackage.DisplayName,
                BasePath = basePackage.FilePath,
                HighestPatchVersion = updates.FirstOrDefault()?.Version
            });
        }

        private static IEnumerable<List<SwitchPackageInfo>> GroupPackages(IEnumerable<SwitchPackageInfo> packages)
        {
            var groups = new List<List<SwitchPackageInfo>>();

            foreach (var package in packages)
            {
                var existing = groups.FirstOrDefault(group => group.Any(item => IsSameGroup(item, package)));
                if (existing == null)
                {
                    groups.Add(new List<SwitchPackageInfo> { package });
                }
                else
                {
                    existing.Add(package);
                }
            }

            return groups;
        }

        // 判断两个包是否应归并到同一游戏。
        private static bool IsSameGroup(SwitchPackageInfo left, SwitchPackageInfo right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(left.BaseTitleId) &&
                !string.IsNullOrWhiteSpace(right.BaseTitleId) &&
                string.Equals(left.BaseTitleId, right.BaseTitleId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(left.NormalizedName) &&
                !string.IsNullOrWhiteSpace(right.NormalizedName) &&
                string.Equals(left.NormalizedName, right.NormalizedName, StringComparison.Ordinal))
            {
                return true;
            }

            return SwitchTitleAliasHelper.HasCommonAlias(left.Aliases, right.Aliases);
        }

        // 对重复本体做稳定排序，优先保留更像正式本体的文件。
        private static int GetBasePackageScore(SwitchPackageInfo package)
        {
            var score = 0;
            var path = (package?.FilePath ?? string.Empty).ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(package?.TitleId))
            {
                score += 100;
            }

            if (!string.IsNullOrWhiteSpace(package?.BaseTitleId))
            {
                score += 20;
            }

            if (path.Contains("本体"))
            {
                score += 20;
            }

            if (path.Contains("升级档") || path.Contains("补丁") || path.Contains(@"\update\") || path.Contains(@"\patch\"))
            {
                score -= 80;
            }

            return score;
        }
    }
}
