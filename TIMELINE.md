# TIMELINE
2026-06-19：新增 GameScreenshots 截图插件，支持快捷键截图、游戏菜单查看和侧边栏画廊
2026-06-19：GameScreenshots 设置页改为点击后自动录入快捷键，并隐藏热键消息窗口
2026-06-19：给游戏时光回顾插件增加全屏主菜单入口并打包 1.4。
2026-06-19：GameScreenshots 1.3 修复截图页面刷新和缩略图显示，新增截图目录设置并改用非阻塞通知。
2026-06-19：GameScreenshots 1.4 增加全屏首页截图区域紧凑内容，并在默认全屏主题游戏列表下方加入插件插槽。
2026-06-19：GameActivityReview 1.5 去掉全屏弹窗入口，改为插入全屏 Recently Played 后面。
2026-06-19：GameActivityReview 1.6 改用默认全屏主题插槽显示在 Recently Played 后。
2026-06-19：新增 Playnite 启动前脚本，自动写入 LunaTranslator OCR 区域并启动程序
2026-06-19：GameActivityReview 1.7 将全屏入口改为 Recently Played 同类栏目按钮，并新增可按键滚动的全屏回顾主面板。
2026-06-19：修复 LunaTranslator OCR 启动前脚本成功路径输出导致 Playnite 误报失败
2026-06-19：GameScreenshots 1.5 修复截图页和画廊页缩略图被 ListView 主题吞掉的问题。
2026-06-19：LunaTranslator 启动前脚本增加固定 OCR 区域热键触发，并新增游戏关闭后停止脚本
2026-06-19：GameActivityReview 全屏入口改为顶部同排栏目并重做回顾页滚动与榜单视觉。
2026-06-19：LunaTranslator 启动前脚本强制开启 OCR 输入源和内置 OCR 引擎
2026-06-19：记录 Playnite 自动启动 LunaTranslator OCR 的脚本用法和配置要求
2026-06-19：GameActivityReview 全屏顶部入口改名为 Play Time，并修正回顾页下拉框边框显示。
2026-06-20：记录 GameActivityReview release 版只需同步插件 DLL 和全屏主题 Main.xaml。
2026-06-20：GameScreenshots 1.6 将截图画廊改为按游戏分组展示。
2026-06-20：GameScreenshots 1.7 新增截图管理模式，支持多选并删除截图文件。
2026-06-20：GameScreenshots 新增设为背景按钮，可把单张选中截图写入对应游戏背景图。
2026-06-20：GameScreenshots 升级到 1.9 并生成新的 pext 发布包。
2026-06-20：GameActivityReview 1.8 恢复桌面主菜单入口，全屏仍使用顶部 Play Time 入口，并生成 1.8 release 包。
2026-06-20：LunaTranslator 脚本改为仅 Switch 平台生效，并新增启动后自动绑定模拟器窗口。
2026-06-20：GameActivityReview 1.9 修复桌面版插件加载失败并把桌面入口改为时长，GameScreenshots 1.8 将截图画廊显示名改为画廊。
2026-06-20：统一 5 个插件最新发布包命名，补齐标准格式的 pext 产物记录。
2026-06-20：新增 SwitchSmartImport 插件，提供 Switch 智能导入、可配置多目录扫描、待确认列表和 Switch Local Metadata 全量刷新。
2026-06-20：SwitchSmartImport 升级到 1.1，补上默认平台/模拟器/配置设置，并修复待确认导入缺配置时崩溃。
2026-06-20：SwitchSmartImport 升级到 1.2，确认导入改为后台执行并使用通知提示开始、完成和失败。
2026-06-20：SwitchSmartImport 升级到 1.3，修复扫描目录编号递增异常，并减少重复本体与补丁误入候选。
2026-06-20：SwitchSmartImport 升级到 1.4，导入时如果库里已存在同路径或同目录同名游戏则改为更新，不再重复新建。
2026-06-20：SwitchSmartImport 升级到 1.5，补上跨目录本体别名判重，减少 Switch Local Metadata 刷新后暴露出的重复游戏。
2026-06-20：SwitchSmartImport 1.5 修复导入后元数据刷新未显式写回数据库的问题，并补上背景图写回。
2026-06-20：SwitchSmartImport 1.5 修复单条元数据失败中断整批刷新，并改正图片优先取内存内容的落库顺序。
2026-06-20：SwitchSmartImport 升级到 1.6，修复定时扫描不通知、不自动导入、扫描间隔不即时生效的问题，并补齐 1.6 安装包。
2026-06-20：SwitchLocalMetadata 1.4 增加自动背景搜索链，本地横图缺失时尝试官网 og:image 和 SteamGridDB 页面图，并生成新 pext 包。
2026-06-20：SwitchLocalMetadata 升级到 1.5，补齐 `nsz/xcz` 读取支持，修复部分 Switch 游戏图标和封面丢失问题。
