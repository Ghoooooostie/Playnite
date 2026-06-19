// 文件用途：把 Playnite 模拟器动作解析成隐藏启动请求。
using Playnite.SDK.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace EmulatorQuickLaunchHide
{
    // 将 Playnite 模拟器动作解析成可直接启动的进程参数。
    public static class HiddenEmulatorLaunchResolver
    {
        // 按模拟器配置类型解析启动请求。
        public static HiddenEmulatorLaunchRequest Resolve(
            HiddenEmulatorLaunchAction action,
            Func<Game, string, string, string, string> expandVariables,
            Func<string, bool> directoryExists,
            Func<string, string, string> findExecutable)
        {
            EnsureNoProfileScripts(action?.Profile);

            if (action?.Profile is CustomEmulatorProfile customProfile)
            {
                return ResolveCustom(action, customProfile, expandVariables);
            }

            if (action?.Profile is BuiltInEmulatorProfile builtInProfile)
            {
                return ResolveBuiltIn(action, builtInProfile, expandVariables, directoryExists, findExecutable);
            }

            throw new InvalidOperationException("不支持的模拟器配置。");
        }

        // 拒绝带脚本的配置，避免隐藏启动时跳过用户脚本。
        private static void EnsureNoProfileScripts(EmulatorProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(profile.PreScript) ||
                !string.IsNullOrWhiteSpace(profile.PostScript) ||
                !string.IsNullOrWhiteSpace(profile.ExitScript))
            {
                throw new InvalidOperationException("带脚本的模拟器配置无法隐藏，请移除预执行、后执行和退出脚本。");
            }
        }

        // 解析用户自定义模拟器配置。
        private static HiddenEmulatorLaunchRequest ResolveCustom(
            HiddenEmulatorLaunchAction action,
            CustomEmulatorProfile profile,
            Func<Game, string, string, string, string> expandVariables)
        {
            if (!string.IsNullOrWhiteSpace(profile.StartupScript))
            {
                throw new InvalidOperationException("脚本型模拟器启动无法隐藏，请改用可执行文件启动配置。");
            }

            var emulatorDir = action.Emulator.InstallDir;
            var arguments = action.SourceAction.OverrideDefaultArgs
                ? action.SourceAction.Arguments
                : profile.Arguments;
            if (!action.SourceAction.OverrideDefaultArgs && !string.IsNullOrWhiteSpace(action.SourceAction.AdditionalArguments))
            {
                arguments = string.IsNullOrWhiteSpace(arguments)
                    ? action.SourceAction.AdditionalArguments
                    : arguments + " " + action.SourceAction.AdditionalArguments;
            }

            return new HiddenEmulatorLaunchRequest
            {
                ExecutablePath = expandVariables(action.Game, profile.Executable, emulatorDir, action.RomPath),
                Arguments = expandVariables(action.Game, arguments, emulatorDir, action.RomPath),
                WorkingDirectory = expandVariables(action.Game, profile.WorkingDirectory, emulatorDir, action.RomPath),
                TrackingMode = profile.TrackingMode,
                TrackingPath = expandVariables(action.Game, profile.TrackingPath, emulatorDir, action.RomPath)
            };
        }

        // 解析 Playnite 内置模拟器配置。
        private static HiddenEmulatorLaunchRequest ResolveBuiltIn(
            HiddenEmulatorLaunchAction action,
            BuiltInEmulatorProfile profile,
            Func<Game, string, string, string, string> expandVariables,
            Func<string, bool> directoryExists,
            Func<string, string, string> findExecutable)
        {
            var definition = action.EmulatorDefinition;
            if (definition == null)
            {
                throw new InvalidOperationException("未找到内置模拟器定义。");
            }

            if (definition.ScriptStartup)
            {
                throw new InvalidOperationException("脚本型模拟器启动无法隐藏，请改用可执行文件启动配置。");
            }

            var emulatorDir = action.Emulator.InstallDir;
            if (!directoryExists(emulatorDir))
            {
                throw new DirectoryNotFoundException(emulatorDir);
            }

            var executable = findExecutable(emulatorDir, definition.StartupExecutable);
            if (string.IsNullOrWhiteSpace(executable))
            {
                throw new FileNotFoundException("找不到模拟器可执行文件。", definition.StartupExecutable);
            }

            var arguments = action.SourceAction.OverrideDefaultArgs
                ? action.SourceAction.Arguments
                : profile.OverrideDefaultArgs ? profile.CustomArguments : definition.StartupArguments;
            if (!action.SourceAction.OverrideDefaultArgs && !string.IsNullOrWhiteSpace(action.SourceAction.AdditionalArguments))
            {
                arguments = string.IsNullOrWhiteSpace(arguments)
                    ? action.SourceAction.AdditionalArguments
                    : arguments + " " + action.SourceAction.AdditionalArguments;
            }

            return new HiddenEmulatorLaunchRequest
            {
                ExecutablePath = executable,
                Arguments = expandVariables(action.Game, arguments, emulatorDir, action.RomPath),
                WorkingDirectory = emulatorDir,
                TrackingMode = TrackingMode.Process
            };
        }

        // 在模拟器目录中按 Playnite 内置规则查找可执行文件。
        public static string FindExecutable(string directory, string executablePattern)
        {
            var regex = new Regex(executablePattern, RegexOptions.IgnoreCase);
            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var relativePath = file.Replace(Path.GetDirectoryName(file), string.Empty).Trim(Path.DirectorySeparatorChar);
                if (regex.IsMatch(relativePath))
                {
                    return file;
                }
            }

            return null;
        }
    }
}
