# ONI-Access

An accessibility mod for Oxygen Not Included that enables blind and visually impaired players to play the game using NVDA screen reader.

## Features

- **Virtual Menu Navigation**: Navigate menus using arrow keys instead of mouse
- **NVDA Integration**: Menu items and UI elements are spoken via NVDA
- **Keyboard Controls**:
  - Up/Down Arrow: Navigate menu items
  - Enter: Activate selected item
  - Backspace: Close menu / Go back

## Installation

1. Download the latest release
2. Copy `ONIAccessibilityMod.dll` to: `Documents\Klei\OxygenNotIncluded\mods\Local\ONIAccessibilityMod\`
3. Copy `mod_info.yaml` and `mod.yaml` to the same folder
4. Copy `nvdaControllerClient64.dll` to the ONI game folder (where OxygenNotIncluded.exe is located)
5. Enable the mod in-game or via mods.json

## Requirements

- Oxygen Not Included (Steam version)
- NVDA Screen Reader
- Windows 64-bit

## Development Status

Currently in Phase 1 development:
- [x] NVDA speech integration
- [x] Main Menu navigation
- [ ] Colony Setup screens
- [ ] In-game navigation
- [ ] Pause menu

## Building

1. Open `ONIAccessibilityMod.csproj` in Visual Studio or use `dotnet build`
2. The DLL will be automatically copied to the mods folder

## License

MIT License - Feel free to use, modify, and distribute.

## Credits

Inspired by Hearthstone Access and other game accessibility mods.
