# DEPLOYMENT

## Playnite 插件 release 版使用

release 版不需要重新编译 Playnite 主程序；插件升级要使用新的插件版本号，不能在同一个版本号下只换 DLL。

## GameActivityReview

当前 release 版本：`1.9`。

| 文件 | 目标位置 |
|---|---|
| `GameActivityReview.dll` | `C:\Users\Administrator\AppData\Roaming\Playnite\Extensions\Game_Activity_Review_7E2B780F-51D2-4BC5-9D80-91DDAA64DF88\` |
| `extension.yaml` | `C:\Users\Administrator\AppData\Roaming\Playnite\Extensions\Game_Activity_Review_7E2B780F-51D2-4BC5-9D80-91DDAA64DF88\` |
| `Main.xaml` | `D:\Program_Files\Playnite\Themes\Fullscreen\Default\Views\Main.xaml` |

发布包：`artifacts\Game_Activity_Review_7E2B780F-51D2-4BC5-9D80-91DDAA64DF88_1_9.pext`。
同步后重启 Playnite，桌面侧边栏和扩展菜单显示 `时长`，全屏顶部仍显示 `Play Time`。

## GameScreenshots

当前 release 版本：`1.8`。

| 文件 | 目标位置 |
|---|---|
| `GameScreenshots.dll` | `C:\Users\Administrator\AppData\Roaming\Playnite\Extensions\Game_Screenshots_5139A212-C04C-419F-A534-71DA19581A63\` |
| `extension.yaml` | `C:\Users\Administrator\AppData\Roaming\Playnite\Extensions\Game_Screenshots_5139A212-C04C-419F-A534-71DA19581A63\` |

发布包：`artifacts\Game_Screenshots_5139A212-C04C-419F-A534-71DA19581A63_1_8.pext`。
同步后重启 Playnite，截图侧边栏显示 `画廊`。

## 统一发布包

| 插件 | 当前 release 版本 | 发布包 |
|---|---|---|
| EmulatorQuickLaunchHide | `1.14` | `artifacts\Emulator_Quick_Launch_Hide_941A6E02-3F88-483D-829A-8B1F7797C681_1_14.pext` |
| GameActivityReview | `1.9` | `artifacts\Game_Activity_Review_7E2B780F-51D2-4BC5-9D80-91DDAA64DF88_1_9.pext` |
| GameScreenshots | `1.8` | `artifacts\Game_Screenshots_5139A212-C04C-419F-A534-71DA19581A63_1_8.pext` |
| LocalExecutableMetadata | `1.2` | `artifacts\Local_Executable_Metadata_2B76AB8B-5E61-4B92-84CB-23F3739346CB_1_2.pext` |
| SwitchLocalMetadata | `1.2` | `artifacts\Switch_Local_Metadata_B49F4F91-73E2-46E8-B66E-3D09BD6BE2FA_1_2.pext` |

注意：不要为这些插件重新编译或替换 Playnite 主程序；GameActivityReview 全屏入口依赖插件 DLL、插件清单版本和默认全屏主题插槽。
