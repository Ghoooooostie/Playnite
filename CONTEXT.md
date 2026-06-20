# CONTEXT

当前目标：修复 SwitchLocalMetadata 在部分 Switch 游戏上丢失图标和封面的问题。
当前进度：已确认根因是 SwitchSmartImport 会导入 `nsz/xcz`，而 SwitchLocalMetadata 之前只读取 `nsp/xci`；已补齐读取支持并生成 1.5 release 包。
下一步：安装 SwitchLocalMetadata 1.5 到 Playnite，重新刷新这些 `nsz/xcz` 游戏的元数据，确认图标和封面恢复。
注意事项：当前 TestRunner 运行时需设置 `SWITCHLOCAL_SKIP_ONLINE=1`，否则会被旧的联网背景测试挡住；本次图标封面问题与背景搜索逻辑无关。
最后更新时间：2026-06-20 14:20:00
