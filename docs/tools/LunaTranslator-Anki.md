# LunaTranslator Anki 自定义脚本

## 目标

给 LunaTranslator 的 Anki 导出增加截图字段。脚本会在制卡时自动截取当前屏幕，并把图片写入 Anki 的 `screenshot` 字段。

## 文件位置

| 用途 | 文件 |
|---|---|
| 项目备份 | `D:\My_Project\Playnite\scripts\myanki_v2.py` |
| LunaTranslator 使用位置 | `D:\Program_Files\LunaTranslator_x64_win10\userconfig\myanki_v2.py` |

## 脚本做什么

| 函数 | 作用 |
|---|---|
| `AnkiFields` | 给 LunaTranslator 默认字段追加 `screenshot` |
| `ParseFieldsData` | 用 Qt 截取当前屏幕，保存为 PNG，再作为 Anki 图片字段传出 |

## LunaTranslator 里怎么用

把 `myanki_v2.py` 放在 LunaTranslator 的 `userconfig` 目录下，也就是：

```text
D:\Program_Files\LunaTranslator_x64_win10\userconfig\myanki_v2.py
```

然后在 Anki 模板里准备一个字段：

```text
screenshot
```

脚本会自己截图，不需要 Playnite 启动脚本再帮它截图。
