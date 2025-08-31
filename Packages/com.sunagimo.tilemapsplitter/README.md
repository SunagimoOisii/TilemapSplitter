# Tilemap Splitter
An Editor extension that categorises Tilemaps by connection relationships and splits them by role.

## Installation
Add the following from the Git URL in the Package Manager:
```
https://github.com/SunagimoOisii/TilemapSplitter.git?path=/Packages/com.sunagimo.tilemapsplitter
```

## Usage
- Editor Window: `Tools/TilemapSplitter`
  - Select a `Tilemap` to split
  - Configure per-shape settings and execute

- Script API:
```csharp
using TilemapSplitter;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Returns a list of generated GameObjects
List<GameObject> created = TilemapSplitterApi.Split(
    source: someTilemap,
    canMergeEdges: false,      // Rect layout only: merge vertical/horizontal edges into one
    canAttachCollider: false,  // Attach TilemapCollider2D + Rigidbody2D(Static) + CompositeCollider2D
    progress: null,            // Optional IProgress<float> (0..1)
    isCancelled: null          // Optional Func<bool> to cancel classification
);
```

Notes
- Settings (layers, tags, preview colors, shape flags) are loaded from EditorPrefs and applied before splitting.
- Layout is detected automatically (Rect or Hex) and the corresponding strategy is used.

