# DEPLOYMENT

## GameActivityReview release 版使用

release 版不需要重新编译 Playnite 主程序，只需要同步以下两个文件：

| 文件 | 目标位置 |
|---|---|
| `GameActivityReview.dll` | `C:\Users\Administrator\AppData\Roaming\Playnite\Extensions\Game_Activity_Review_7E2B780F-51D2-4BC5-9D80-91DDAA64DF88\` |
| `Main.xaml` | `D:\Program_Files\Playnite\Themes\Fullscreen\Default\Views\Main.xaml` |

同步后重启 `D:\Program_Files\Playnite\Playnite.FullscreenApp.exe` 验证全屏顶部是否出现 `Play Time`。

注意：不要为这个插件重新编译或替换 Playnite 主程序；当前方案依赖插件 DLL 和默认全屏主题插槽。
