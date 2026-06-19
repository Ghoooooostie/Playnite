# 文件用途：检查 LunaTranslator 启动前/停止后脚本是否满足 Playnite 调用要求。
# 相关模块：scripts/Start-LunaTranslatorWithOcr.ps1、scripts/Stop-LunaTranslator.ps1。
$ErrorActionPreference = "Stop"

# 检查条件，不满足时直接失败。
function Assert-True {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$startScriptPath = Join-Path $repoRoot "scripts\Start-LunaTranslatorWithOcr.ps1"
$stopScriptPath = Join-Path $repoRoot "scripts\Stop-LunaTranslator.ps1"
$startScript = Get-Content -LiteralPath $startScriptPath -Encoding UTF8 -Raw

Assert-True -Condition ($startScript -notmatch "already running") -Message "Start script must not fail when LunaTranslator is already running."
Assert-True -Condition (Test-Path -LiteralPath $stopScriptPath) -Message "Stop script should exist for Playnite game stopped event."
Assert-True -Condition ($startScript -notmatch "Write-Output") -Message "Start script success path should stay silent."
Assert-True -Condition ($startScript -match "_14") -Message "Start script should configure LunaTranslator OCR range toggle hotkey."
Assert-True -Condition ($startScript -match "SendWait") -Message "Start script should trigger the OCR range toggle without mouse input."
Assert-True -Condition ($startScript -match "sourcestatus2") -Message "Start script should enable OCR text source."
Assert-True -Condition ($startScript -match "ocr\.local") -Message "Start script should enable built-in OCR engine."
