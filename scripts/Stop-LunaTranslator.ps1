# 文件用途：在 Playnite 游戏关闭后关闭 LunaTranslator，避免下次启动时沿用旧内存区域。
# 相关模块：Playnite 游戏关闭后脚本、LunaTranslator 进程管理。
param([int]$ExitTimeoutSeconds = 5)

$ErrorActionPreference = "Stop"

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
