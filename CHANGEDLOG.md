# Changelog

All notable changes to this project will be documented in this file.

## [1.2.2] - 2025-08-31

### Added
- Guard messages and validation in the Editor UI to prevent invalid execution states.
  - Shows info/warning/error HelpBox when:
    - No Tilemap is assigned
    - The selected Tilemap is inactive in the hierarchy
    - The selected Tilemap is not under a Grid (no `layoutGrid`)
    - The selected Tilemap has no tiles
  - Disables the Execute button when guards are failing.
  - Displays defensive dialogs on Execute as a final safety net.
- Isometric layout note HelpBox explaining known draw‑order limitations in Unity.

### Affected files
- `Packages/com.sunagimo.tilemapsplitter/Editor/UI/TilemapSplitterUI.cs`
- `TilemapSplitter/TilemapSplitter/src/UI/TilemapSplitterUI.cs`

### Notes
- Messages are currently in English. If you prefer Japanese UI strings, please open an issue or request localization and we will update the text.

