param(
    [int]$StartedProcessId = 0,
    [string[]]$PlatformNames,
    [string[]]$SwitchPlatformNames = @("Nintendo Switch", "Switch"),
    [string]$BindWindowHotkey = "Alt+B",
    [int]$WindowWaitSeconds = 10,
    [int]$LunaReadyWaitSeconds = 2
)

# 文件用途：在 Playnite 游戏启动后，把 LunaTranslator OCR 绑定到刚启动的模拟器窗口。
# 相关模块：Playnite 游戏启动后脚本、LunaTranslator 绑定窗口热键。
$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 读取 Playnite 当前游戏的平台名称，用于只让 Switch 游戏绑定窗口。
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

# 把 Alt+X 这种写法转换成 SendKeys 格式。
function ConvertTo-SendKeys {
    param([Parameter(Mandatory = $true)][string]$Hotkey)

    return $Hotkey.ToLowerInvariant().Replace("alt+", "%").Replace("ctrl+", "^").Replace("shift+", "+").ToUpperInvariant()
}

# 等待模拟器窗口出现，优先使用 Playnite 传入的 StartedProcessId。
function Wait-StartedProcessWindow {
    param(
        [int]$ProcessId,
        [int]$TimeoutSeconds
    )

    if ($ProcessId -le 0) {
        return $null
    }

    $deadline = [DateTime]::Now.AddSeconds($TimeoutSeconds)
    while ([DateTime]::Now -lt $deadline) {
        $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($process -and $process.MainWindowHandle -ne 0) {
            return $process
        }

        Start-Sleep -Milliseconds 200
    }

    return $null
}

# 注册窗口和鼠标 API，重复执行时跳过已存在类型。
function Ensure-NativeMethods {
    if ("LunaBindWindowNativeMethods" -as [type]) {
        return
    }

    $source = @"
using System;
using System.Runtime.InteropServices;

public static class LunaBindWindowNativeMethods
{
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
"@

    Add-Type -TypeDefinition $source
}

if (-not (Test-SwitchGame)) {
    return
}

$startedProcessVariable = Get-Variable -Name "StartedProcessId" -ErrorAction SilentlyContinue
if ($StartedProcessId -le 0 -and $startedProcessVariable) {
    $StartedProcessId = [int]$startedProcessVariable.Value
}

# Playnite 某些全局启动后脚本不会提供进程 ID；这种情况不能弹失败框。
if ($StartedProcessId -le 0) {
    return
}

$targetProcess = Wait-StartedProcessWindow -ProcessId $StartedProcessId -TimeoutSeconds $WindowWaitSeconds
if (-not $targetProcess) {
    return
}

Add-Type -AssemblyName System.Windows.Forms
Ensure-NativeMethods

$rect = New-Object LunaBindWindowNativeMethods+RECT
if (-not [LunaBindWindowNativeMethods]::GetWindowRect($targetProcess.MainWindowHandle, [ref]$rect)) {
    throw "Started emulator window rectangle was not found. StartedProcessId=$StartedProcessId"
}

$centerX = [int](($rect.Left + $rect.Right) / 2)
$centerY = [int](($rect.Top + $rect.Bottom) / 2)

[LunaBindWindowNativeMethods]::SetForegroundWindow($targetProcess.MainWindowHandle) | Out-Null
Start-Sleep -Seconds $LunaReadyWaitSeconds

[System.Windows.Forms.SendKeys]::SendWait((ConvertTo-SendKeys -Hotkey $BindWindowHotkey))
Start-Sleep -Milliseconds 300

[LunaBindWindowNativeMethods]::SetCursorPos($centerX, $centerY) | Out-Null
[LunaBindWindowNativeMethods]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[LunaBindWindowNativeMethods]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)

# Keep stdout empty. Playnite may show script pipeline output as a script failure.
