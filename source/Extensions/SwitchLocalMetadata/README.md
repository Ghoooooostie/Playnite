# Switch Local Metadata

Playnite metadata extension for reading Nintendo Switch metadata from local `.xci` and `.nsp` files.

## Install on Another Computer

1. Install Playnite.
2. Copy `Switch_Local_Metadata_B49F4F91-73E2-46E8-B66E-3D09BD6BE2FA_1_5.pext` to the computer.
3. Double-click the `.pext` file to install the extension.
4. Restart Playnite.
5. Open `Add-ons` -> `Extension settings` -> `Switch Local Metadata`.
6. Set these paths:
   - `hactoolnet.exe`
   - `prod.keys`
   - `title.keys` if available
7. Make sure each Playnite game points to the base `.xci` or `.nsp` file in its ROM or play action path.
8. Right-click the game, choose metadata download, and select `Switch Local Metadata`.

## Required Files

| File | Required | Purpose |
|---|---|---|
| `hactoolnet.exe` | Yes | Reads Switch XCI, NSP, and NCA data. |
| `prod.keys` | Yes | Decrypts Switch content metadata. |
| `title.keys` | Recommended | Helps with some eShop NSP content. |
| Base `.xci` / `.nsp` | Yes | Source game package. Update and DLC packages may not contain cover metadata. |

## Notes

- Version 1.5 adds direct metadata reading for `.nsz` and `.xcz`, so compressed Switch packages can restore icon and cover data again.
- 联网搜索背景图可在插件设置里关闭。
- The extension still uses `hactoolnet.exe`; the `.pext` package alone is not enough on a new computer.
- If a game path points to an `[UPD]` update package or DLC package, cover metadata may be missing.
