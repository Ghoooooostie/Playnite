# 文件用途：在 Playnite 的 Switch 游戏关闭后关闭 LunaTranslator，避免下次启动时沿用旧内存区域。
# 相关模块：Playnite 游戏关闭后脚本、LunaTranslator 进程管理。
param(
    [int]$ExitTimeoutSeconds = 5,
    [string[]]$PlatformNames,
    [string[]]$SwitchPlatformNames = @("Nintendo Switch", "Switch")
)

$ErrorActionPreference = "Stop"

# 读取 Playnite 当前游戏的平台名称，用于只让 Switch 游戏关闭露娜。
function Get-PlaynitePlatformNames {
    if ($PlatformNames -and $PlatformNames.Count -gt 0) {
        return @($PlatformNames)
    }

    $gameVariable = Get-Variable -Name "Game" -ErrorAction SilentlyContinue
    if ($gameVariable) {
        $gameValue = $gameVariable.Value
        if ($gameValue -and $gameValue.Platforms) {
            $names = @()
            foreach ($platform in $gameValue.Platforms) {
                if ($platform -and -not [string]::IsNullOrWhiteSpace($platform.Name)) {
                    $names += $platform.Name
                }
            }
            return $names
        }
    }

    return @()
}

# 判断当前游戏是否是 Switch；没有平台信息时继续执行，便于单游戏脚本手动调用。
function Test-SwitchGame {
    $names = @(Get-PlaynitePlatformNames)
    if ($names.Count -eq 0) {
        return $true
    }

    foreach ($name in $names) {
        foreach ($switchName in $SwitchPlatformNames) {
            if ([string]::Equals($name, $switchName, [System.StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }

    return $false
}

if (-not (Test-SwitchGame)) {
    return
}

$processes = @(Get-Process -Name "LunaTranslator", "LunaTranslator_admin" -ErrorAction SilentlyContinue)
foreach ($process in $processes) {
    $process.CloseMainWindow() | Out-Null
}

$deadline = [DateTime]::Now.AddSeconds($ExitTimeoutSeconds)
while ([DateTime]::Now -lt $deadline) {
    $remaining = @(Get-Process -Name "LunaTranslator", "LunaTranslator_admin" -ErrorAction SilentlyContinue)
    if ($remaining.Count -eq 0) {
        break
    }

    Start-Sleep -Milliseconds 200
}

# Keep stdout empty. Playnite may show script pipeline output as a script failure.
