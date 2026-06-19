// 文件用途：从 Playnite 游戏和模拟器配置生成隐藏启动播放动作。
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Linq;

namespace EmulatorQuickLaunchHide
{
    // 从游戏的模拟器动作生成隐藏启动动作。
    public static class HiddenEmulatorActionBuilder
    {
        // 生成当前游戏可用的隐藏模拟器启动动作。
        public static IEnumerable<HiddenEmulatorLaunchAction> GetLaunchActions(
            Game game,
            IEnumerable<Emulator> emulators,
            IEnumerable<EmulatorDefinition> emulatorDefinitions)
        {
            if (game?.GameActions == null)
            {
                yield break;
            }

            var emulatorList = emulators?.ToList() ?? new List<Emulator>();
            var definitionList = emulatorDefinitions?.ToList() ?? new List<EmulatorDefinition>();
            var roms = game.Roms?.ToList();
            if (roms == null || roms.Count == 0)
            {
                roms = new List<GameRom> { new GameRom() };
            }

            foreach (var action in game.GameActions.Where(a => a.IsPlayAction && a.Type == GameActionType.Emulator))
            {
                foreach (var emulator in GetEmulators(action, game, emulatorList, definitionList))
                {
                    var profiles = GetProfiles(action, game, emulator, definitionList).ToList();
                    foreach (var profile in profiles)
                    {
                        var definition = GetDefinition(profile, emulator, definitionList);
                        foreach (var rom in roms)
                        {
                            yield return new HiddenEmulatorLaunchAction
                            {
                                Name = BuildName(action, emulator, profile, rom, roms.Count > 1),
                                Game = game,
                                SourceAction = action,
                                Emulator = emulator,
                                Profile = profile,
                                EmulatorDefinition = definition,
                                RomPath = rom.Path
                            };
                        }
                    }
                }
            }
        }

        // 选择动作未指定模拟器时，按平台找可用模拟器。
        private static IEnumerable<Emulator> GetEmulators(
            GameAction action,
            Game game,
            IEnumerable<Emulator> emulators,
            IEnumerable<EmulatorDefinition> definitions)
        {
            if (action.EmulatorId != System.Guid.Empty)
            {
                var emulator = emulators.FirstOrDefault(e => e.Id == action.EmulatorId);
                if (emulator != null)
                {
                    yield return emulator;
                }

                yield break;
            }

            foreach (var emulator in emulators)
            {
                if (GetProfiles(action, game, emulator, definitions).Any())
                {
                    yield return emulator;
                }
            }
        }

        // 按游戏动作指定的配置选出模拟器配置。
        private static IEnumerable<EmulatorProfile> GetProfiles(
            GameAction action,
            Game game,
            Emulator emulator,
            IEnumerable<EmulatorDefinition> definitions)
        {
            if (!string.IsNullOrEmpty(action.EmulatorProfileId))
            {
                var profile = emulator.AllProfiles.FirstOrDefault(p => p.Id == action.EmulatorProfileId);
                if (profile != null)
                {
                    yield return profile;
                }

                yield break;
            }

            foreach (var profile in emulator.AllProfiles)
            {
                if (action.EmulatorId != System.Guid.Empty || IsProfileCompatible(game, emulator, profile, definitions))
                {
                    yield return profile;
                }
            }
        }

        // 判断模拟器配置是否匹配当前游戏平台。
        private static bool IsProfileCompatible(
            Game game,
            Emulator emulator,
            EmulatorProfile profile,
            IEnumerable<EmulatorDefinition> definitions)
        {
            if (profile is CustomEmulatorProfile customProfile)
            {
                return customProfile.Platforms?.Intersect(game.PlatformIds ?? new List<System.Guid>()).Any() == true;
            }

            if (profile is BuiltInEmulatorProfile)
            {
                var definition = GetDefinition(profile, emulator, definitions);
                var platformSpecs = game.Platforms?.Where(p => !string.IsNullOrEmpty(p.SpecificationId)).Select(p => p.SpecificationId).ToList();
                return definition?.Platforms?.Intersect(platformSpecs ?? new List<string>()).Any() == true;
            }

            return false;
        }

        // 为内置模拟器配置找到 Playnite 自带定义。
        private static EmulatorDefinitionProfile GetDefinition(
            EmulatorProfile profile,
            Emulator emulator,
            IEnumerable<EmulatorDefinition> definitions)
        {
            if (profile is BuiltInEmulatorProfile builtIn)
            {
                return definitions.FirstOrDefault(d => d.Id == emulator.BuiltInConfigId)
                    ?.Profiles
                    ?.FirstOrDefault(p => p.Name == builtIn.BuiltInProfileName);
            }

            return null;
        }

        // 生成 Playnite 播放动作列表里显示的名称。
        private static string BuildName(GameAction action, Emulator emulator, EmulatorProfile profile, GameRom rom, bool hasMultipleRoms)
        {
            var name = string.IsNullOrWhiteSpace(action.Name) ? profile?.Name : action.Name;
            if (action.EmulatorId == System.Guid.Empty)
            {
                name = string.IsNullOrWhiteSpace(profile?.Name) ? emulator.Name : $"{emulator.Name}: {profile.Name}";
            }

            if (hasMultipleRoms && !string.IsNullOrWhiteSpace(rom?.Name))
            {
                name = $"{name}: {rom.Name}";
            }

            return $"隐藏启动模拟器: {name}";
        }
    }
}
