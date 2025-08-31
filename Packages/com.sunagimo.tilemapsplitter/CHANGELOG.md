# Changelog

## [1.0.0]
- Initial Release

## [1.2.0]
- Support for Hexagon layouts with classification into Full, Junction5, Junction4, Junction3, Edge, Tip, and Isolate

## [1.2.1]
- Implement public API

## [1.2.2]
- Isometric layout note HelpBox explaining known draw‑order limitations in Unity
- Guard messages and validation in the Editor UI to prevent invalid execution states
  - Shows info/warning/error HelpBox when:
    - No Tilemap is assigned
    - The selected Tilemap is inactive in the hierarchy
    - The selected Tilemap is not under a Grid (no `layoutGrid`)
    - The selected Tilemap has no tiles
  - Disables the Execute button when guards are failing
  - Displays defensive dialogs on Execute as a final safety net
