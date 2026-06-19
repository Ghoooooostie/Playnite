# Playnite 启动 LunaTranslator OCR 流程

## 目标

在掌机无鼠标场景下，通过 Playnite 自动打开 LunaTranslator，只对 Switch 游戏生效，并把 OCR 绑定到当前启动的模拟器窗口。

## 文件位置

| 用途 | 文件 |
|---|---|
| 游戏启动前脚本 | `D:\My_Project\Playnite\scripts\Start-LunaTranslatorWithOcr.ps1` |
| 游戏启动后脚本 | `D:\My_Project\Playnite\scripts\Bind-LunaTranslatorWindow.ps1` |
| 游戏关闭后脚本 | `D:\My_Project\Playnite\scripts\Stop-LunaTranslator.ps1` |
| LunaTranslator 程序目录 | `D:\Program_Files\LunaTranslator_x64_win10` |
| LunaTranslator 配置文件 | `D:\Program_Files\LunaTranslator_x64_win10\userconfig\config.json` |

## Playnite 里怎么填

建议填在 Playnite 全局脚本里，脚本会自己判断平台；只有平台名是 `Nintendo Switch` 或 `Switch` 的游戏才会执行。

| Playnite 位置 | 内容 |
|---|---|
| 游戏启动前脚本 | `& "D:\My_Project\Playnite\scripts\Start-LunaTranslatorWithOcr.ps1"` |
| 游戏启动后脚本 | `& "D:\My_Project\Playnite\scripts\Bind-LunaTranslatorWindow.ps1" -StartedProcessId $StartedProcessId` |
| 游戏关闭后脚本 | `& "D:\My_Project\Playnite\scripts\Stop-LunaTranslator.ps1"` |

注意不要在 Playnite 脚本里再写 `powershell -File`，否则新进程拿不到 `$Game` 和 `$StartedProcessId`，绑定窗口会失效。也不要把 `ForEach-Object { ... }` 填进一行脚本框，容易漏括号。

## 判断规则

| 游戏平台 | 模拟器 | 是否启动 LunaTranslator |
|---|---|---|
| Nintendo Switch / Switch | 任意模拟器 | 是 |
| 其他平台 | 任意模拟器 | 否 |

脚本不写死 Yuzu、Ryujinx、Eden 等模拟器名称，只看 Playnite 当前游戏的平台。

## 启动前脚本做什么

`Start-LunaTranslatorWithOcr.ps1` 会做这些事：

| 动作 | 说明 |
|---|---|
| 判断 Switch 平台 | 非 Switch 游戏静默退出 |
| 写入 OCR 区域 | 写入 `ocrregions`，当前区域是 `[[[395,761],[1254,993]]]` |
| 开启 OCR 输入源 | 设置 `sourcestatus2.ocr.use = true` |
| 开启内置 OCR | 只启用 `ocr.local.use = true`，关闭其他 OCR 引擎 |
| 设置范围框热键 | 设置“显示/隐藏范围框”为 `Alt+W` |
| 设置绑定窗口热键 | 设置“绑定窗口”为 `Alt+B` |
| 启动 LunaTranslator | 使用普通 `LunaTranslator.exe` 启动 |
| 自动触发范围框 | 启动后模拟 `Alt+W`，让 LunaTranslator 显示配置里的 OCR 区域 |

## 启动后脚本做什么

`Bind-LunaTranslatorWindow.ps1` 会在游戏启动后读取 Playnite 的 `StartedProcessId`，等待模拟器主窗口出现，然后触发 `Alt+B` 并自动点击模拟器窗口中心，完成 LunaTranslator 绑定窗口。若写法错误导致 `StartedProcessId=0`，脚本会静默跳过，避免 Playnite 弹失败框。

## LunaTranslator 要注意什么

| 项目 | 要求 |
|---|---|
| 启动程序 | 使用 `LunaTranslator.exe`，不要用 `LunaTranslator_admin.exe` |
| 运行权限 | Playnite 和 LunaTranslator 权限要一致，推荐都用普通权限 |
| 残留进程 | 如果 LunaTranslator 变成后台异常进程或管理员进程，需要手动退出一次 |
| OCR 区域 | 不需要每次框选；只有想换字幕位置时才改坐标 |
| 绑定窗口 | 需要把绑定脚本放在“游戏启动后脚本”，启动前还没有模拟器窗口 |

当前坐标在启动脚本参数里：

```powershell
[int]$Left = 395,
[int]$Top = 761,
[int]$Right = 1254,
[int]$Bottom = 993,
```

也可以在 Playnite 脚本里覆盖：

```powershell
powershell -ExecutionPolicy Bypass -File "D:\My_Project\Playnite\scripts\Start-LunaTranslatorWithOcr.ps1" -Left 395 -Top 761 -Right 1254 -Bottom 993
```

## 游戏关闭后脚本做什么

`Stop-LunaTranslator.ps1` 只会在 Switch 游戏关闭后尝试关闭 LunaTranslator 主窗口，避免下次启动时沿用旧内存状态。

它不会强杀管理员权限进程。如果关不掉，说明当前 LunaTranslator 权限比 Playnite 高，需要手动退出一次。

## 常见问题

| 现象 | 处理 |
|---|---|
| 非 Switch 游戏也启动了 LunaTranslator | 检查 Playnite 平台名是否被填成 `Switch` 或 `Nintendo Switch` |
| Switch 游戏没启动 LunaTranslator | 检查 Playnite 游戏的平台名是否是 `Switch` 或 `Nintendo Switch` |
| 配置里有坐标，但看不到框 | 先确认 LunaTranslator 是正常 UI 进程，不是 `MainWindowHandle=0` 的后台异常进程 |
| 没绑定到模拟器窗口 | 确认启动后脚本是 `& "D:\My_Project\Playnite\scripts\Bind-LunaTranslatorWindow.ps1" -StartedProcessId $StartedProcessId`，不要用 `powershell -File` |
| Playnite 弹脚本失败 | 检查是否有管理员版 LunaTranslator 残留，先手动退出 |
| 没开启内置 OCR | 启动前脚本会强制开启 `sourcestatus2.ocr` 和 `ocr.local` |
| 掌机没鼠标不能框选 | 不需要框选，固定坐标由脚本写入 |

## 验证命令

只验证非 Switch 会静默跳过，不读取 LunaTranslator 配置：

```powershell
powershell -ExecutionPolicy Bypass -File "D:\My_Project\Playnite\scripts\Start-LunaTranslatorWithOcr.ps1" -PlatformNames Windows -LunaRoot "D:\not-exist" -SkipLaunch
```

只验证 Switch 配置写入，不启动 LunaTranslator：

```powershell
powershell -ExecutionPolicy Bypass -File "D:\My_Project\Playnite\scripts\Start-LunaTranslatorWithOcr.ps1" -PlatformNames "Nintendo Switch" -SkipLaunch
```

检查配置结果：

```powershell
$json = Get-Content -Encoding UTF8 "D:\Program_Files\LunaTranslator_x64_win10\userconfig\config.json" -Raw | ConvertFrom-Json
$json.sourcestatus2.ocr.use
$json.ocr.local.use
$json.quick_setting.all._14.keystring
$json.quick_setting.all._15.keystring
$json.ocrregions | ConvertTo-Json -Depth 10
```
