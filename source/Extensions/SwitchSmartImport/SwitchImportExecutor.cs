// 文件用途：把待确认的 Switch 候选导入到 Playnite 数据库。
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SwitchSmartImport
{
    // Switch 导入执行器接口。
    public interface ISwitchImportExecutor
    {
        List<Game> Import(IEnumerable<SwitchImportCandidate> candidates, SwitchSmartImportSettings settings);
    }

    // Switch 导入执行器。
    public class SwitchImportExecutor : ISwitchImportExecutor
    {
        private readonly IGameDatabaseAPI database;

        public SwitchImportExecutor(IGameDatabaseAPI database)
        {
            this.database = database ?? throw new ArgumentNullException("database");
        }

        // 导入选中的候选列表。
        public List<Game> Import(IEnumerable<SwitchImportCandidate> candidates, SwitchSmartImportSettings settings)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException("candidates");
            }

            ValidateSettings(settings);
            var importedGames = new List<Game>();
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.Import)
                {
                    continue;
                }

                var metadata = new GameMetadata
                {
                    Name = candidate.GameName,
                    InstallDirectory = Path.GetDirectoryName(candidate.BasePath),
                    Version = candidate.HighestPatchVersion,
                    IsInstalled = true,
                    GameActions = new List<GameAction>
                    {
                        new GameAction
                        {
                            Name = "Play",
                            Type = GameActionType.Emulator,
                            IsPlayAction = true,
                            EmulatorId = settings.DefaultEmulatorId,
                            EmulatorProfileId = settings.DefaultEmulatorProfileId
                        }
                    },
                    Roms = new List<GameRom>
                    {
                        new GameRom(Path.GetFileName(candidate.BasePath), candidate.BasePath)
                    },
                    Platforms = new HashSet<MetadataProperty>
                    {
                        new MetadataIdProperty(candidate.SelectedPlatformId == Guid.Empty ? settings.DefaultPlatformId : candidate.SelectedPlatformId)
                    }
                };

                var existingGame = FindExistingGame(candidate, settings);
                if (existingGame != null)
                {
                    UpdateExistingGame(existingGame, metadata);
                    database.Games.Update(existingGame);
                    importedGames.Add(existingGame);
                    continue;
                }

                importedGames.Add(database.ImportGame(metadata));
            }

            return importedGames;
        }

        // 校验最小导入配置。
        private static void ValidateSettings(SwitchSmartImportSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (settings.DefaultEmulatorId == Guid.Empty)
            {
                throw new InvalidOperationException("默认模拟器未配置。");
            }

            if (string.IsNullOrWhiteSpace(settings.DefaultEmulatorProfileId))
            {
                throw new InvalidOperationException("默认模拟器配置未配置。");
            }

            if (settings.DefaultPlatformId == Guid.Empty)
            {
                throw new InvalidOperationException("默认平台未配置。");
            }
        }

        // 查找库里已存在的同一 Switch 游戏，避免同一本体重复导入。
        private Game FindExistingGame(SwitchImportCandidate candidate, SwitchSmartImportSettings settings)
        {
            var platformId = candidate.SelectedPlatformId == Guid.Empty ? settings.DefaultPlatformId : candidate.SelectedPlatformId;
            var candidatePath = candidate.BasePath ?? string.Empty;
            var candidateDirectory = Path.GetDirectoryName(candidatePath) ?? string.Empty;
            var candidateName = SwitchTitleAliasHelper.Normalize(candidate.GameName);
            var candidateBaseTitleId = SwitchTitleAliasHelper.NormalizeBaseTitleId(SwitchPackageClassifier.ExtractTitleId(candidatePath));
            var candidateAliases = SwitchTitleAliasHelper.ExtractAliasesFromPath(candidatePath, candidate.GameName);

            foreach (var game in database.Games)
            {
                if (game == null)
                {
                    continue;
                }

                if (game.PlatformIds != null && game.PlatformIds.Count > 0 && !game.PlatformIds.Contains(platformId))
                {
                    continue;
                }

                if (game.Roms != null && game.Roms.Any(a => a != null && string.Equals(a.Path, candidatePath, StringComparison.OrdinalIgnoreCase)))
                {
                    return game;
                }

                if (!string.IsNullOrWhiteSpace(candidateBaseTitleId) && GameHasBaseTitleId(game, candidateBaseTitleId))
                {
                    return game;
                }

                var existingDirectory = game.InstallDirectory ?? string.Empty;
                if (string.Equals(existingDirectory, candidateDirectory, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(SwitchTitleAliasHelper.Normalize(game.Name), candidateName, StringComparison.Ordinal))
                {
                    return game;
                }

                if (SwitchTitleAliasHelper.HasCommonAlias(candidateAliases, GetGameAliases(game)))
                {
                    return game;
                }
            }

            return null;
        }

        // 把新的导入信息回写到已存在游戏。
        private static void UpdateExistingGame(Game game, GameMetadata metadata)
        {
            game.Name = metadata.Name;
            game.InstallDirectory = metadata.InstallDirectory;
            game.Version = metadata.Version;
            game.IsInstalled = metadata.IsInstalled;
            game.GameActions = metadata.GameActions == null ? null : new System.Collections.ObjectModel.ObservableCollection<GameAction>(metadata.GameActions);
            game.Roms = metadata.Roms == null ? null : new System.Collections.ObjectModel.ObservableCollection<GameRom>(metadata.Roms);
            game.PlatformIds = metadata.Platforms?.OfType<MetadataIdProperty>().Select(a => a.Id).ToList() ?? new List<Guid>();
        }

        // 判断现有游戏是否带有相同本体 Title ID。
        private static bool GameHasBaseTitleId(Game game, string candidateBaseTitleId)
        {
            if (game?.Roms == null || string.IsNullOrWhiteSpace(candidateBaseTitleId))
            {
                return false;
            }

            return game.Roms
                .Where(rom => rom != null && !string.IsNullOrWhiteSpace(rom.Path))
                .Select(rom => SwitchTitleAliasHelper.NormalizeBaseTitleId(SwitchPackageClassifier.ExtractTitleId(rom.Path)))
                .Any(baseTitleId => string.Equals(baseTitleId, candidateBaseTitleId, StringComparison.OrdinalIgnoreCase));
        }

        // 提取现有游戏可用于判重的标题别名。
        private static IReadOnlyCollection<string> GetGameAliases(Game game)
        {
            var aliases = new HashSet<string>(StringComparer.Ordinal);
            if (game == null)
            {
                return aliases.ToList();
            }

            foreach (var alias in SwitchTitleAliasHelper.ExtractAliasesFromPath(game.InstallDirectory, game.Name))
            {
                aliases.Add(alias);
            }

            if (game.Roms != null)
            {
                foreach (var rom in game.Roms.Where(a => a != null && !string.IsNullOrWhiteSpace(a.Path)))
                {
                    foreach (var alias in SwitchTitleAliasHelper.ExtractAliasesFromPath(rom.Path, game.Name))
                    {
                        aliases.Add(alias);
                    }
                }
            }

            return aliases.ToList();
        }
    }
}
