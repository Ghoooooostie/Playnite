# Game Screenshots Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增独立 Playnite 截图插件，支持全局快捷键保存当前游戏截图、游戏菜单截图入口、侧边栏画廊。

**Architecture:** 插件只依赖 Playnite SDK 和 .NET Framework 自带 WPF/Win32 API。截图文件写入插件用户目录 `screenshots/<gameId>/`，索引从文件系统实时读取，避免维护二份数据。

**Tech Stack:** C#、.NET Framework 4.6.2、WPF、NUnit、Playnite GenericPlugin。

---

## 文件结构
| 文件 | 动作 | 职责 |
|---|---|---|
| `source/Extensions/GameScreenshots/GameScreenshotsPlugin.cs` | 新增 | 插件入口、菜单、侧边栏、详情页控件、热键生命周期 |
| `source/Extensions/GameScreenshots/ScreenshotStore.cs` | 新增 | 按游戏保存和读取截图文件 |
| `source/Extensions/GameScreenshots/ScreenshotCaptureService.cs` | 新增 | 捕获屏幕并保存 PNG |
| `source/Extensions/GameScreenshots/ScreenshotHotkeyService.cs` | 新增 | 注册/注销全局热键并触发截图 |
| `source/Extensions/GameScreenshots/GameScreenshotsSettings.cs` | 新增 | 保存热键开关、修饰键、按键 |
| `source/Extensions/GameScreenshots/GameScreenshotsSettingsView.cs` | 新增 | 简单设置页 |
| `source/Extensions/GameScreenshots/GameScreenshotsViewModel.cs` | 新增 | 画廊和游戏详情数据模型 |
| `source/Extensions/GameScreenshots/GameScreenshotsGalleryView.cs` | 新增 | 侧边栏画廊 UI |
| `source/Extensions/GameScreenshots/GameScreenshotsGameView.cs` | 新增 | 单个游戏截图窗口 UI |
| `source/Extensions/GameScreenshots/extension.yaml` | 新增 | Playnite 扩展清单 |
| `source/Extensions/GameScreenshots/GameScreenshots.csproj` | 新增 | 插件项目 |
| `source/Extensions/GameScreenshots.Tests/*` | 新增 | 单元测试 |

## 执行步骤
| 步骤 | 验证 |
|---|---|
| 先新增测试项目和截图存储测试 | 测试应先因缺少类型失败 |
| 实现 ScreenshotStore 和模型 | 存储测试通过 |
| 新增插件热键选择逻辑测试 | 测试应先失败 |
| 实现插件触发逻辑和热键服务接口 | 插件逻辑测试通过 |
| 新增 UI/ViewModel 基础测试 | 测试应先失败 |
| 实现详情页控件和画廊控件 | UI/ViewModel 测试通过 |
| 加入解决方案和打包验证 | 构建插件并生成 `.pext` |

## 完成标准
| 项目 | 标准 |
|---|---|
| 截图保存 | 每个游戏保存到独立目录，文件名包含时间 |
| 游戏菜单 | 右键游戏可保存截图，也可打开该游戏截图窗口 |
| 画廊 | 侧边栏能看到所有游戏截图，保留游戏名 |
| 快捷键 | 默认 `Ctrl+Shift+F12`，触发当前选中游戏截图 |
| 验证 | 新测试通过；能构建插件包，不能完整构建时说明环境原因 |
