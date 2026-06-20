# Switch 智能导入 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 实现一个 Switch 专用 Playnite 智能导入插件，支持多目录扫描、待确认列表、定期扫描、补丁/DLC 过滤，以及可选的 `Switch Local Metadata` 全量资料刷新。

**Architecture:** 采用独立 GenericPlugin，不修改 Playnite 核心导入窗口。扫描流程拆成“文件发现 -> 包分类 -> 候选归并 -> 待确认缓存 -> 导入执行 -> 可选元数据刷新”六段，插件入口只负责菜单、设置和调度。识别规则优先依赖文件名里的 `[Base]/[Update]/[DLC]`、Title ID 和版本号，目录结构只做辅助。

**Tech Stack:** C# / .NET Framework 4.6.2、Playnite SDK、NUnit、现有扩展测试运行器模式

---

### Task 1: 更新任务现场文档

**Files:**
- Modify: `D:\My_Project\Playnite\task_plan.md`
- Modify: `D:\My_Project\Playnite\progress.md`
- Modify: `D:\My_Project\Playnite\findings.md`

- [ ] **Step 1: 更新任务计划摘要**

把 `task_plan.md` 改成当前任务内容，记录目标、步骤、边界。关键内容：

```md
# 任务计划

目标：新增 Switch 智能导入插件，解决 Switch 本体、补丁、DLC 和重复包误导入问题，并支持待确认列表与可选全量元数据刷新。

## 步骤
| 步骤 | 状态 |
|---|---|
| 梳理 H:\乙女 的真实目录与命名模式 | 已完成 |
| 写入设计和实现计划 | 已完成 |
| 新增插件骨架与红灯测试 | 进行中 |
| 实现扫描、分类、归并和待确认缓存 | 未开始 |
| 实现导入与元数据全量刷新 | 未开始 |
| 运行测试、构建和打包 | 未开始 |
```

- [ ] **Step 2: 追加进度记录**

在 `progress.md` 末尾追加一行：

```md
- 2026-06-20：确认 Switch 智能导入要优先按文件名标记与 Title ID 识别，不能依赖固定本体/补丁/DLC 分目录。
```

- [ ] **Step 3: 追加发现记录**

在 `findings.md` 末尾追加 2-3 条结论：

```md
- `H:\乙女` 中 Switch 包既有分目录存放，也有同目录混放 `[Base]` / `[Update]` 的情况。
- 同一游戏可能同时存在本体、副本、整合包、补丁和 DLC，去重不能只按文件路径。
- `Switch Local Metadata` 目前可直接按游戏路径返回本地元数据，适合作为导入后可选的全量刷新来源。
```

- [ ] **Step 4: 不执行测试**

本任务只改文档，不运行命令。

- [ ] **Step 5: Commit**

```bash
git add task_plan.md progress.md findings.md docs/superpowers/specs/2026-06-20-switch-smart-import-design.md docs/superpowers/plans/2026-06-20-switch-smart-import.md
git commit -m "docs: add switch smart import design and plan"
```

### Task 2: 搭建插件项目骨架

**Files:**
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchSmartImport.csproj`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\extension.yaml`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\Properties\AssemblyInfo.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchSmartImportPlugin.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchSmartImportSettings.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchSmartImportSettingsView.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchSmartImport.Tests.csproj`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchSmartImportPluginTests.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.TestRunner\SwitchSmartImport.TestRunner.csproj`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.TestRunner\Program.cs`

- [ ] **Step 1: 先写插件红灯测试**

在 `SwitchSmartImportPluginTests.cs` 写最小失败测试，覆盖：

```csharp
[Test]
public void Plugin_adds_main_menu_entries()
{
    var plugin = new SwitchSmartImportPlugin(new FakePlayniteApi());
    var items = plugin.GetMainMenuItems(new GetMainMenuItemsArgs()).ToList();

    Assert.AreEqual(3, items.Count);
    Assert.AreEqual("Switch 智能导入", items[0].Description);
    Assert.AreEqual("立即扫描 Switch 智能导入", items[1].Description);
    Assert.AreEqual("Switch 智能导入设置", items[2].Description);
}

[Test]
public void Plugin_exposes_settings()
{
    var plugin = new SwitchSmartImportPlugin(new FakePlayniteApi());

    Assert.IsNotNull(plugin.GetSettings(false));
    Assert.IsNotNull(plugin.GetSettingsView(false));
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; msbuild source/Extensions/SwitchSmartImport.Tests/SwitchSmartImport.Tests.csproj /t:Build /p:Configuration=Debug
```

Expected: FAIL，提示缺少 `SwitchSmartImportPlugin` 或相关项目文件。

- [ ] **Step 3: 写最小插件骨架**

创建 `SwitchSmartImportPlugin.cs` 最小实现：

```csharp
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SwitchSmartImport
{
    // 文件用途：Switch 智能导入插件入口，负责主菜单入口和设置页。
    public class SwitchSmartImportPlugin : GenericPlugin
    {
        private readonly SwitchSmartImportSettingsViewModel settingsViewModel;

        public override Guid Id => Guid.Parse("5E230E44-29A3-4A76-AF78-C71A6B6C5D54");

        public SwitchSmartImportPlugin(IPlayniteAPI api) : base(api)
        {
            settingsViewModel = new SwitchSmartImportSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new SwitchSmartImportSettingsView();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem { Description = "Switch 智能导入", MenuSection = "@", Action = () => { } };
            yield return new MainMenuItem { Description = "立即扫描 Switch 智能导入", MenuSection = "@", Action = () => { } };
            yield return new MainMenuItem { Description = "Switch 智能导入设置", MenuSection = "@", Action = () => OpenSettingsView() };
        }
    }
}
```

`SwitchSmartImportSettings.cs` 最小实现：

```csharp
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SwitchSmartImport
{
    // 文件用途：保存 Switch 智能导入插件设置，并提供 Playnite 设置页模型。
    public class SwitchSmartImportSettings : ObservableObject
    {
    }

    public class SwitchSmartImportSettingsViewModel : ObservableObject, ISettings
    {
        private readonly SwitchSmartImportPlugin plugin;
        private SwitchSmartImportSettings editingClone;
        private SwitchSmartImportSettings settings;

        public SwitchSmartImportSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public SwitchSmartImportSettingsViewModel(SwitchSmartImportPlugin plugin)
        {
            this.plugin = plugin;
            Settings = plugin.LoadPluginSettings<SwitchSmartImportSettings>() ?? new SwitchSmartImportSettings();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
```

- [ ] **Step 4: 补最小设置视图和项目文件**

`SwitchSmartImportSettingsView.cs`：

```csharp
using System.Windows.Controls;

namespace SwitchSmartImport
{
    // 文件用途：承载 Switch 智能导入设置页。
    public class SwitchSmartImportSettingsView : UserControl
    {
        public SwitchSmartImportSettingsView()
        {
            Content = new TextBlock { Text = "Switch 智能导入设置" };
        }
    }
}
```

`extension.yaml`：

```yaml
Id: Switch_Smart_Import_5E230E44-29A3-4A76-AF78-C71A6B6C5D54
Name: Switch 智能导入
Author: Codex
Version: 1.0
Module: SwitchSmartImport.dll
Type: GenericPlugin
```

- [ ] **Step 5: 运行测试确认通过**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; msbuild source/Extensions/SwitchSmartImport.TestRunner/SwitchSmartImport.TestRunner.csproj /t:Build /p:Configuration=Debug
```

Then:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: PASS 2，FAIL 0。

- [ ] **Step 6: Commit**

```bash
git add source/Extensions/SwitchSmartImport source/Extensions/SwitchSmartImport.Tests source/Extensions/SwitchSmartImport.TestRunner
git commit -m "feat: scaffold switch smart import plugin"
```

### Task 3: 实现扫描配置模型和持久化

**Files:**
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchSmartImportSettings.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchScanPathConfig.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchPendingImportStore.cs`
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchSmartImportPluginTests.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchPendingImportStoreTests.cs`
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.TestRunner\Program.cs`

- [ ] **Step 1: 写失败测试，覆盖设置默认值和缓存读写**

```csharp
[Test]
public void Settings_default_to_no_metadata_source_and_manual_confirmation()
{
    var settings = new SwitchSmartImportSettings();

    Assert.AreEqual(SwitchMetadataSource.None, settings.MetadataSource);
    Assert.IsTrue(settings.RequireManualConfirmation);
}

[Test]
public void Pending_store_round_trips_candidates()
{
    var root = Path.Combine(Path.GetTempPath(), "SwitchSmartImportTests", Guid.NewGuid().ToString("N"));
    var store = new SwitchPendingImportStore(root);
    var items = new List<SwitchImportCandidate>
    {
        new SwitchImportCandidate { GameName = "测试游戏", BasePath = @"H:\乙女\测试游戏\base.nsp" }
    };

    store.Save(items, DateTime.Parse("2026-06-20T12:00:00"));
    var loaded = store.Load();

    Assert.AreEqual(1, loaded.Candidates.Count);
    Assert.AreEqual("测试游戏", loaded.Candidates[0].GameName);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: FAIL，提示缺少 `SwitchMetadataSource`、`SwitchPendingImportStore`、`SwitchImportCandidate`。

- [ ] **Step 3: 实现设置模型、扫描目录模型和缓存存储**

要求：

- `SwitchSmartImportSettings` 增加：
  - `List<SwitchScanPathConfig> ScanPaths`
  - `bool EnableScheduledScan`
  - `int ScheduledScanMinutes`
  - `bool ScanOnStartup`
  - `bool IncludeSubdirectories`
  - `bool ShowDlcInPendingList`
  - `bool RecordHighestPatchVersion`
  - `bool RequireManualConfirmation`
  - `bool PreferMergedPackage`
  - `bool ImportWithRelativePaths`
  - `Guid DefaultEmulatorId`
  - `string DefaultEmulatorProfileId`
  - `Guid DefaultPlatformId`
  - `SwitchMetadataSource MetadataSource`
- `SwitchScanPathConfig` 增加：
  - `string Name`
  - `string Path`
  - `bool Enabled`
  - `int Priority`
  - `SwitchScanPathType TypeHint`
- `SwitchPendingImportStore` 用插件数据目录下 `pending-imports.json` 保存：
  - `SavedAt`
  - `Candidates`
  - `SkippedItems`

- [ ] **Step 4: 运行测试确认通过**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: 新增测试通过。

- [ ] **Step 5: Commit**

```bash
git add source/Extensions/SwitchSmartImport
git commit -m "feat: add switch smart import settings and cache store"
```

### Task 4: 实现包分类与候选归并

**Files:**
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchPackageInfo.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchPackageClassifier.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchImportCandidate.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchCandidateMerger.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchPackageClassifierTests.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchCandidateMergerTests.cs`
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.TestRunner\Program.cs`

- [ ] **Step 1: 先写失败测试，覆盖真实样例**

测试至少覆盖这些样例：

```csharp
[Test]
public void Classifier_recognizes_base_and_update_in_same_directory()
{
    var baseInfo = SwitchPackageClassifier.Classify(@"H:\乙女\PanicPalette [010063C0212BE000][v0][Base].nsp");
    var updateInfo = SwitchPackageClassifier.Classify(@"H:\乙女\PanicPalette [010063C0212BE800][v65536][Update].nsp");

    Assert.AreEqual(SwitchPackageType.Base, baseInfo.PackageType);
    Assert.AreEqual(SwitchPackageType.Update, updateInfo.PackageType);
}

[Test]
public void Classifier_recognizes_dlc_from_file_name()
{
    var info = SwitchPackageClassifier.Classify(@"H:\乙女\結合男子 [0100DA2019045001][DLC 1].nsp");

    Assert.AreEqual(SwitchPackageType.Dlc, info.PackageType);
}

[Test]
public void Merger_keeps_single_candidate_and_highest_patch()
{
    var result = SwitchCandidateMerger.Merge(new[]
    {
        new SwitchPackageInfo { DisplayName = "PanicPalette", NormalizedName = "panicpalette", PackageType = SwitchPackageType.Base, TitleId = "010063C0212BE000", FilePath = @"H:\base.nsp" },
        new SwitchPackageInfo { DisplayName = "PanicPalette", NormalizedName = "panicpalette", PackageType = SwitchPackageType.Update, TitleId = "010063C0212BE800", Version = "1.0.1", FilePath = @"H:\u101.nsp" },
        new SwitchPackageInfo { DisplayName = "PanicPalette", NormalizedName = "panicpalette", PackageType = SwitchPackageType.Update, TitleId = "010063C0212BE800", Version = "1.0.3", FilePath = @"H:\u103.nsp" }
    });

    Assert.AreEqual(1, result.Candidates.Count);
    Assert.AreEqual("1.0.3", result.Candidates[0].HighestPatchVersion);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: FAIL，提示缺少分类器和归并器。

- [ ] **Step 3: 实现最小分类器与归并器**

要求：

- `SwitchPackageClassifier`：
  - 只接受 `.nsp/.nsz/.xci/.xcz`
  - 优先识别 `[Base]`、`[Update]`、`[DLC]`、`[UPD]`、`[APP]`
  - 提取 `TitleId`
  - 提取版本号：支持 `v65536`、`1.0.1`
  - 去掉常见垃圾后缀，如站点名和 `(1)`
- `SwitchCandidateMerger`：
  - 归组键优先 `NormalizedName`
  - 一个候选只保留一个本体
  - 多个补丁只保留最高版本信息
  - DLC 默认放进 `SkippedItems`
  - 没有本体但有补丁时生成 `缺少本体` 跳过项

- [ ] **Step 4: 运行测试确认通过**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: 分类和归并测试通过。

- [ ] **Step 5: Commit**

```bash
git add source/Extensions/SwitchSmartImport
git commit -m "feat: classify and merge switch packages"
```

### Task 5: 实现目录扫描和定时扫描

**Files:**
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchImportScanner.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchScheduledScanService.cs`
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchSmartImportPlugin.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchImportScannerTests.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchScheduledScanServiceTests.cs`
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.TestRunner\Program.cs`

- [ ] **Step 1: 写失败测试**

```csharp
[Test]
public void Scanner_ignores_non_switch_files_and_collects_switch_packages()
{
    // 创建 .nsp、.rar、.txt 混合目录，断言只返回 Switch 包
}

[Test]
public void Scheduled_scan_only_updates_pending_store()
{
    // 断言扫描后更新缓存，不触发导入执行
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: FAIL，提示缺少扫描器和定时器。

- [ ] **Step 3: 实现扫描器和调度器**

要求：

- `SwitchImportScanner`：
  - 按设置扫描多个目录
  - 只读支持扩展名
  - 扫描结果交给 `SwitchPackageClassifier` + `SwitchCandidateMerger`
- `SwitchScheduledScanService`：
  - 按分钟间隔触发
  - 只调用扫描并保存到 `SwitchPendingImportStore`
  - 不调用导入执行
- `SwitchSmartImportPlugin`：
  - `OnApplicationStarted` 时按设置启动定时器
  - `立即扫描` 菜单实际触发一次扫描

- [ ] **Step 4: 运行测试确认通过**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: 扫描和定时器测试通过。

- [ ] **Step 5: Commit**

```bash
git add source/Extensions/SwitchSmartImport
git commit -m "feat: add switch smart import scanning"
```

### Task 6: 实现导入执行与可选全量资料刷新

**Files:**
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchImportExecutor.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchMetadataRefreshService.cs`
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport\SwitchSmartImportPlugin.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchImportExecutorTests.cs`
- Create: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.Tests\SwitchMetadataRefreshServiceTests.cs`
- Modify: `D:\My_Project\Playnite\source\Extensions\SwitchSmartImport.TestRunner\Program.cs`

- [ ] **Step 1: 先写失败测试**

```csharp
[Test]
public void Import_executor_creates_one_game_from_candidate()
{
    // 断言只导入本体路径
}

[Test]
public void Metadata_refresh_is_skipped_when_source_is_none()
{
    // 断言不调用任何元数据插件
}

[Test]
public void Metadata_refresh_uses_switch_local_metadata_and_overwrites_existing_values()
{
    // 预先给游戏旧名称，再断言被新来源值覆盖
}
```

- [ ] **Step 2: 运行测试确认失败**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: FAIL，提示缺少导入执行器和元数据刷新服务。

- [ ] **Step 3: 实现导入执行器**

要求：

- 每个候选生成一个 `Game`
- `GameAction` 使用设置中的模拟器和配置
- `Roms` 指向最终本体文件
- `InstallDirectory` 指向本体所在目录
- 如果缺默认模拟器配置，直接抛明确错误

- [ ] **Step 4: 实现元数据刷新服务**

要求：

- `MetadataSource == None` 时直接返回，不处理
- `MetadataSource == SwitchLocalMetadata` 时：
  - 从 `PlayniteApi.Addons.Plugins` 找 `SwitchLocalMetadataPlugin`
  - 如果没找到，抛明确错误
  - 对每个游戏创建 `MetadataRequestOptions`
  - 按支持字段逐个读取
  - 只要来源返回值，就覆盖到游戏上
  - 不做“仅空字段更新”

- [ ] **Step 5: 运行测试确认通过**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: 导入和元数据刷新测试通过。

- [ ] **Step 6: Commit**

```bash
git add source/Extensions/SwitchSmartImport
git commit -m "feat: import switch candidates and refresh metadata"
```

### Task 7: 完成验证、构建和打包

**Files:**
- Modify: `D:\My_Project\Playnite\progress.md`
- Modify: `D:\My_Project\Playnite\TIMELINE.md`

- [ ] **Step 1: 运行测试总集**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; .\\source\\Extensions\\SwitchSmartImport.TestRunner\\bin\\Debug\\SwitchSmartImport.TestRunner.exe
```

Expected: 全部通过，失败 0。

- [ ] **Step 2: 构建 Release**

Run:

```bash
chcp 65001 > $null; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; msbuild source/Extensions/SwitchSmartImport/SwitchSmartImport.csproj /t:Build /p:Configuration=Release
```

Expected: Build succeeded.

- [ ] **Step 3: 打包 pext**

使用现有打包方式，生成：

```text
artifacts/SwitchSmartImport-1.0.pext
```

包内容至少包含：

- `extension.yaml`
- `SwitchSmartImport.dll`

- [ ] **Step 4: 更新进度和时间线**

在 `progress.md` 追加：

```md
- 2026-06-20：SwitchSmartImport 1.0 完成，支持多目录扫描、待确认列表、补丁/DLC 过滤和可选全量元数据刷新。
```

在 `TIMELINE.md` 追加：

```md
2026-06-20：新增 SwitchSmartImport 插件，提供 Switch 智能导入、待确认列表和 Switch Local Metadata 全量刷新。
```

- [ ] **Step 5: Commit**

```bash
git add source/Extensions/SwitchSmartImport source/Extensions/SwitchSmartImport.Tests source/Extensions/SwitchSmartImport.TestRunner progress.md TIMELINE.md
git commit -m "feat: add switch smart import plugin"
```
