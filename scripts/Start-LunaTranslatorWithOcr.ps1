# 文件用途：在 Playnite 启动游戏前重启 LunaTranslator，写入固定 OCR 区域并让配置生效。
# 相关模块：Playnite 启动前脚本、LunaTranslator userconfig/config.json。
param(
    [string]$LunaRoot = "D:\Program_Files\LunaTranslator_x64_win10",
    [int]$Left = 395,
    [int]$Top = 761,
    [int]$Right = 1254,
    [int]$Bottom = 993,
    [int]$ExitTimeoutSeconds = 5,
    [int]$LaunchWaitSeconds = 3,
    [string]$ShowRangeHotkey = "Alt+W",
    [switch]$MultiRegion,
    [switch]$SkipLaunch,
    [switch]$SkipShowRange
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 写入 UTF-8 JSON 文件，避免 PowerShell 默认编码影响配置。
function Save-Utf8JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($Path, $json, [System.Text.UTF8Encoding]::new($false))
}

# 验证 OCR 区域必须是有效矩形，避免写坏 LunaTranslator 配置。
function Assert-ValidOcrRegion {
    param(
        [int]$Left,
        [int]$Top,
        [int]$Right,
        [int]$Bottom
    )

    if ($Left -ge $Right -or $Top -ge $Bottom) {
        throw "Invalid OCR region. Left/Top must be smaller than Right/Bottom. Current: $Left,$Top,$Right,$Bottom"
    }
}

# 关闭已运行的 LunaTranslator；管理员进程无法关闭时不强杀，避免 Playnite 弹错。
function Stop-LunaTranslatorProcess {
    param([int]$TimeoutSeconds)

    $processes = @(Get-Process -Name "LunaTranslator", "LunaTranslator_admin" -ErrorAction SilentlyContinue)
    foreach ($process in $processes) {
        $process.CloseMainWindow() | Out-Null
    }

    $deadline = [DateTime]::Now.AddSeconds($TimeoutSeconds)
    while ([DateTime]::Now -lt $deadline) {
        $remaining = @(Get-Process -Name "LunaTranslator", "LunaTranslator_admin" -ErrorAction SilentlyContinue)
        if ($remaining.Count -eq 0) {
            return
        }

        Start-Sleep -Milliseconds 200
    }
}

# 强制开启 OCR 输入源和内置 OCR 引擎。
function Enable-LunaBuiltInOcr {
    param([Parameter(Mandatory = $true)]$Config)

    if (-not $Config.sourcestatus2) {
        $Config | Add-Member -MemberType NoteProperty -Name "sourcestatus2" -Value ([pscustomobject]@{})
    }

    if (-not $Config.sourcestatus2.ocr) {
        $Config.sourcestatus2 | Add-Member -MemberType NoteProperty -Name "ocr" -Value ([pscustomobject]@{})
    }

    $Config.sourcestatus2.ocr.use = $true

    foreach ($engine in $Config.ocr.PSObject.Properties) {
        $engine.Value.use = $false
    }

    $Config.ocr.local.use = $true
}
# 给 LunaTranslator 写入显示 OCR 范围框的热键，供无鼠标设备自动触发。
function Set-LunaShowRangeHotkey {
    param(
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Hotkey
    )

    if (-not $Config.quick_setting) {
        $Config | Add-Member -MemberType NoteProperty -Name "quick_setting" -Value ([pscustomobject]@{})
    }

    if (-not $Config.quick_setting.all) {
        $Config.quick_setting | Add-Member -MemberType NoteProperty -Name "all" -Value ([pscustomobject]@{})
    }

    if (-not $Config.quick_setting.all._14) {
        $Config.quick_setting.all | Add-Member -MemberType NoteProperty -Name "_14" -Value ([pscustomobject]@{})
    }

    $Config.quick_setting.all._14.use = $true
    $Config.quick_setting.all._14.keystring = $Hotkey
}

# 模拟热键，让 LunaTranslator 根据 ocrregions 显示范围框。
function Send-LunaShowRangeHotkey {
    param([string]$Hotkey)

    Add-Type -AssemblyName System.Windows.Forms
    $sendKeys = $Hotkey.ToLowerInvariant().Replace("alt+", "%").Replace("ctrl+", "^").Replace("shift+", "+").ToUpperInvariant()
    [System.Windows.Forms.SendKeys]::SendWait($sendKeys)
}

Assert-ValidOcrRegion -Left $Left -Top $Top -Right $Right -Bottom $Bottom

$configPath = Join-Path $LunaRoot "userconfig\config.json"
$exePath = Join-Path $LunaRoot "LunaTranslator.exe"

if (-not (Test-Path -LiteralPath $configPath)) {
    throw "LunaTranslator config file was not found: $configPath"
}

if (-not $SkipLaunch -and -not (Test-Path -LiteralPath $exePath)) {
    throw "LunaTranslator executable was not found: $exePath"
}

Stop-LunaTranslatorProcess -TimeoutSeconds $ExitTimeoutSeconds

$config = Get-Content -LiteralPath $configPath -Encoding UTF8 -Raw | ConvertFrom-Json
$config.multiregion = [bool]$MultiRegion
$config.showrangeafterrangeselect = $true
Enable-LunaBuiltInOcr -Config $config
$config.ocrregions = [object[]](,([object[]]@([object[]]@($Left, $Top), [object[]]@($Right, $Bottom))))
Set-LunaShowRangeHotkey -Config $config -Hotkey $ShowRangeHotkey

Save-Utf8JsonFile -Path $configPath -Value $config

if (-not $SkipLaunch) {
    Start-Process -FilePath $exePath -WorkingDirectory $LunaRoot
    Start-Sleep -Seconds $LaunchWaitSeconds

    if (-not $SkipShowRange) {
        Send-LunaShowRangeHotkey -Hotkey $ShowRangeHotkey
    }
}

# Keep stdout empty. Playnite may show script pipeline output as a script failure.
