# Local Executable Metadata Design

## Goal

Add a separate Playnite metadata provider for Windows PC games that reads useful data directly from local executable folders.

## Scope

The plugin handles local `.exe` games only. It does not replace or modify the existing Switch metadata plugin, and it does not download online metadata.

## Design

- `LocalExecutablePathResolver` finds a game executable from Playnite file actions first, then from the install directory first level.
- `LocalExecutableMetadataReader` reads Windows version info, Unity `*_Data/app.info`, Steam AppId values from `steam_appid.txt` or local `.ini` files, associated exe icon, and install folder size.
- `LocalExecutableMetadataProvider` exposes Playnite fields: name, developers, publishers, platform, links, icon, and install size.
- The platform is reported as `pc_windows`.

## Validation

The sample `HouseFlipper2.exe` must return:

- Name: `House Flipper 2`
- Company: `Frozen District`
- Steam AppId: `1190970`
- Icon file: `HouseFlipper2.png`
