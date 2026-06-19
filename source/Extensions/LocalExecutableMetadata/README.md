# Local Executable Metadata

Playnite metadata extension for reading Windows PC game metadata from local executable folders.

## Install

1. Build or copy `Local_Executable_Metadata_2B76AB8B-5E61-4B92-84CB-23F3739346CB_1_1.pext`.
2. Double-click the `.pext` file to install it.
3. Restart Playnite.
4. Make sure the Playnite game has a File play action pointing to the game `.exe`, or has its install directory set.
5. Right-click the game, choose metadata download, and select `Local Executable Metadata`.

## What It Reads

| Source | Data |
|---|---|
| Windows `.exe` version info | Game name, company, associated icon when available |
| Unity `*_Data/app.info` | Company and game name |
| `steam_appid.txt` or `AppId=` in local `.ini` files | Steam AppId |
| Local `cover` / `poster` image or Steam AppId | Cover image |
| Game install folder | Install size |

## Notes

- This extension does not use `hactoolnet.exe`.
- If no local cover exists, Playnite can download the Steam cover image by using the locally detected Steam AppId.
- Some games store little or no metadata in the exe, so the result depends on what the game folder contains.
