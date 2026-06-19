# 文件用途：检查 LunaTranslator 启动前/停止后脚本是否满足 Playnite 调用要求。
# 相关模块：scripts/Start-LunaTranslatorWithOcr.ps1、scripts/Bind-LunaTranslatorWindow.ps1、scripts/Stop-LunaTranslator.ps1。
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
$bindScriptPath = Join-Path $repoRoot "scripts\Bind-LunaTranslatorWindow.ps1"
$startScript = Get-Content -LiteralPath $startScriptPath -Encoding UTF8 -Raw
$bindScript = if (Test-Path -LiteralPath $bindScriptPath) { Get-Content -LiteralPath $bindScriptPath -Encoding UTF8 -Raw } else { "" }
$docPath = Join-Path $repoRoot "docs\tools\LunaTranslator-OCR-Playnite.md"
$doc = Get-Content -LiteralPath $docPath -Encoding UTF8 -Raw
$playnitePreStartCommand = '& "D:\My_Project\Playnite\scripts\Start-LunaTranslatorWithOcr.ps1"'
$playniteGameStartedCommand = '& "D:\My_Project\Playnite\scripts\Bind-LunaTranslatorWindow.ps1" -StartedProcessId $StartedProcessId'
$playniteGameStoppedCommand = '& "D:\My_Project\Playnite\scripts\Stop-LunaTranslator.ps1"'

Assert-True -Condition ($startScript -notmatch "already running") -Message "Start script must not fail when LunaTranslator is already running."
Assert-True -Condition (Test-Path -LiteralPath $stopScriptPath) -Message "Stop script should exist for Playnite game stopped event."
Assert-True -Condition (Test-Path -LiteralPath $bindScriptPath) -Message "Bind script should exist for Playnite game started event."
Assert-True -Condition ($startScript -notmatch "Write-Output") -Message "Start script success path should stay silent."
Assert-True -Condition ($startScript -match "_14") -Message "Start script should configure LunaTranslator OCR range toggle hotkey."
Assert-True -Condition ($startScript -match "_15") -Message "Start script should configure LunaTranslator bind-window hotkey."
Assert-True -Condition ($startScript -match "SendWait") -Message "Start script should trigger the OCR range toggle without mouse input."
Assert-True -Condition ($startScript -match "sourcestatus2") -Message "Start script should enable OCR text source."
Assert-True -Condition ($startScript -match "ocr\.local") -Message "Start script should enable built-in OCR engine."
Assert-True -Condition ($startScript -match "PlatformNames") -Message "Start script should accept Playnite platform names."
Assert-True -Condition ($startScript -match "Nintendo Switch") -Message "Start script should only run for Switch games by default."
Assert-True -Condition ($bindScript -match "StartedProcessId") -Message "Bind script should bind the emulator process Playnite started."
Assert-True -Condition ($bindScript -match "SetCursorPos") -Message "Bind script should click the emulator window without mouse input."
Assert-True -Condition ($bindScript -match "StartedProcessId -le 0") -Message "Bind script should handle missing Playnite process id without throwing."
Assert-True -Condition ($doc -match '-StartedProcessId \$StartedProcessId') -Message "Playnite game-started script docs should pass StartedProcessId explicitly."
Assert-True -Condition ($doc.Contains($playnitePreStartCommand)) -Message "Playnite pre-start script docs should use the short direct call."
Assert-True -Condition ($doc.Contains($playniteGameStartedCommand)) -Message "Playnite game-started script docs should use the short direct call."
Assert-True -Condition ($doc.Contains($playniteGameStoppedCommand)) -Message "Playnite game-stopped script docs should use the short direct call."
Assert-True -Condition ($doc -notmatch 'powershell\s+-File\s+"D:\\My_Project\\Playnite\\scripts\\Bind-LunaTranslatorWindow\.ps1"') -Message "Playnite script docs should not start a separate PowerShell for game-started binding."

$null = [System.Management.Automation.PSParser]::Tokenize($playnitePreStartCommand, [ref]$null)
$null = [System.Management.Automation.PSParser]::Tokenize($playniteGameStartedCommand, [ref]$null)
$null = [System.Management.Automation.PSParser]::Tokenize($playniteGameStoppedCommand, [ref]$null)

$missingProcessOutput = & $bindScriptPath -PlatformNames @("Nintendo Switch") -StartedProcessId 0 -WindowWaitSeconds 1
Assert-True -Condition ($null -eq $missingProcessOutput) -Message "Bind script should silently skip when Playnite does not provide StartedProcessId."

$missingRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("missing-luna-" + [Guid]::NewGuid().ToString("N"))
$nonSwitchOutput = & $startScriptPath -LunaRoot $missingRoot -PlatformNames @("Windows") -SkipLaunch
Assert-True -Condition ($null -eq $nonSwitchOutput) -Message "Start script should silently skip non-Switch games before reading LunaTranslator config."

$parentScopeNonSwitchOutput = & {
    $Game = [pscustomobject]@{
        Platforms = @([pscustomobject]@{ Name = "Windows" })
    }

    & $startScriptPath -LunaRoot $missingRoot -SkipLaunch
}
Assert-True -Condition ($null -eq $parentScopeNonSwitchOutput) -Message "Start script should read Playnite Game from the caller scope."

$switchFailed = $false
try {
    & $startScriptPath -LunaRoot $missingRoot -PlatformNames @("Nintendo Switch") -SkipLaunch
}
catch {
    $switchFailed = $_.Exception.Message -match "config file was not found"
}
Assert-True -Condition $switchFailed -Message "Start script should continue to LunaTranslator setup for Switch games."
