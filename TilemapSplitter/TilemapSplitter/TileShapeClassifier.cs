namespace TilemapSplitter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    [Flags]
    internal enum ShapeFlags
    {
        VerticalEdge   = 1 << 0,
        HorizontalEdge = 1 << 1,
        Independent    = 1 << 2,
    }

    internal enum ShapeType_Rect
    {
        VerticalEdge = 0,
        HorizontalEdge,
        Cross,
        TJunction,
        Corner,
        Isolate,
    }

    internal enum ShapeType_Hex
    {
        Isolate = 0,
        Tip,
        Edge,
        Junction3,
        Junction4,
        Junction5,
        Full,
    }

    internal class ShapeSetting
    {
        public ShapeFlags flags;
        public int        layer;
        public string     tag        = "Untagged";
        public bool       canPreview = true;
        public Color      previewColor;
    }

    /// <summary>
    /// Store cell coordinates for each tile classification
    /// </summary>
    internal class ShapeCells_Rect
    {
        public readonly List<Vector3Int> Vertical   = new();
        public readonly List<Vector3Int> Horizontal = new();
        public readonly List<Vector3Int> Cross      = new();
        public readonly List<Vector3Int> TJunction  = new();
        public readonly List<Vector3Int> Corner     = new();
        public readonly List<Vector3Int> Isolate    = new();
    }

    internal class ShapeCells_Hex
    {
        public readonly List<Vector3Int> Isolate   = new();
        public readonly List<Vector3Int> Tip       = new();
        public readonly List<Vector3Int> Edge      = new();
        public readonly List<Vector3Int> Junction3 = new();
        public readonly List<Vector3Int> Junction4 = new();
        public readonly List<Vector3Int> Junction5 = new();
        public readonly List<Vector3Int> Full      = new();
    }

    internal static class TileShapeClassifier
    {
        private static readonly Vector3Int[] neighbors_Rect =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right,
        };

        private static readonly Vector3Int[] neighbors_PointTop_Even =
        {
            new( 1, 0, 0), new( 0, -1, 0), new(-1, -1, 0),
            new(-1, 0, 0), new(-1,  1, 0), new( 0,  1, 0)
        };
        private static readonly Vector3Int[] neighbors_PointTop_Odd =
        {
            new( 1, 0, 0), new(1, -1, 0), new(0, -1, 0),
            new(-1, 0, 0), new(0,  1, 0), new(1,  1, 0)
        };

        private static readonly Vector3Int[] neighbors_FlatTop_Even =
        {
            new( 1, 0, 0), new(1,  1, 0), new(0,  1, 0),
            new(-1, 0, 0), new(0, -1, 0), new(1, -1, 0)
        };
        private static readonly Vector3Int[] neighbors_FlatTop_Odd =
        {
            new( 1, 0, 0), new( 0,  1, 0), new(-1,  1, 0),
            new(-1, 0, 0), new(-1, -1, 0), new( 0, -1, 0)
        };

        private static bool IsPointTopLayout(GridLayout grid)
        {
            var c0  = grid.CellToWorld(Vector3Int.zero);
            var cUp = grid.CellToWorld(Vector3Int.up);
            return !Mathf.Approximately(cUp.x, c0.x);
        }

        private static IReadOnlyList<Vector3Int> GetNeighborOffsets_Rect() => neighbors_Rect;

        private static IReadOnlyList<Vector3Int> GetNeighborOffsets_Hex(Vector3Int cell, bool isPointTop)
        {
            if (isPointTop) return (cell.y & 1) == 0 ? neighbors_PointTop_Even : neighbors_PointTop_Odd;
            else            return (cell.x & 1) == 0 ? neighbors_FlatTop_Even : neighbors_FlatTop_Odd;
        }

        /// <summary>
        /// Collect all non-empty cells from the Tilemap.
        /// </summary>
        private static HashSet<Vector3Int> CollectOccupiedCells(Tilemap source)
        {
            source.CompressBounds();
            var cellBounds    = source.cellBounds;
            var tilesInBounds = source.GetTilesBlock(cellBounds);

            int width  = cellBounds.size.x;
            int height = cellBounds.size.y;
            var occupiedCells = new HashSet<Vector3Int>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    if (tilesInBounds[index] != null)
                    {
                        var cell = new Vector3Int(cellBounds.xMin + x,
                                                  cellBounds.yMin + y,
                                                  cellBounds.zMin);
                        occupiedCells.Add(cell);
                    }
                }
            }
            return occupiedCells;
        }

        /// <summary>
        /// Compress the tilemap bounds to exclude empty rows and columns
        /// </summary>
        public static IEnumerator ClassifyCoroutine_Rect(Tilemap source,
            Dictionary<ShapeType_Rect, ShapeSetting> settings, ShapeCells_Rect sc, int batch = 100)
        {
            sc.Vertical.Clear();
            sc.Horizontal.Clear();
            sc.Cross.Clear();
            sc.TJunction.Clear();
            sc.Corner.Clear();
            sc.Isolate.Clear();

            var occupiedCells = CollectOccupiedCells(source);
            bool isCancelled  = false;
            try
            {
                int total     = occupiedCells.Count;
                int processed = 0;
                foreach (var cell in occupiedCells)
                {
                    Classify_Rect(cell, occupiedCells, settings, sc);

                    processed++;
                    if (processed % batch == 0)
                    {
                        float progress = (float)processed / total;
                        isCancelled = EditorUtility.DisplayCancelableProgressBar("Classify_Rect",
                            $"Classifying... {processed}/{total}", progress);
                        if (isCancelled) break;

                        yield return null;
                    }
                }
                if (isCancelled) yield break;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static IEnumerator ClassifyCoroutine_Hex(Tilemap source,
            Dictionary<ShapeType_Hex, ShapeSetting> settings, ShapeCells_Hex sc, int batch = 100)
        {
            sc.Isolate.Clear();
            sc.Tip.Clear();
            sc.Edge.Clear();
            sc.Junction3.Clear();
            sc.Junction4.Clear();
            sc.Junction5.Clear();
            sc.Full.Clear();

            var occupiedCells = CollectOccupiedCells(source);
            bool isCancelled = false;
            try
            {
                bool isPointTop = IsPointTopLayout(source.layoutGrid);
                int  total      = occupiedCells.Count;
                int  processed  = 0;
                foreach (var cell in occupiedCells)
                {
                    var offsets = GetNeighborOffsets_Hex(cell, isPointTop);
                    int count   = 0;
                    foreach (var offset in offsets)
                    {
                        if (occupiedCells.Contains(cell + offset))
                            count++;
                    }
                    Classify_Hex(cell, count, settings, sc);

                    processed++;
                    if (processed % batch == 0)
                    {
                        float progress = (float)processed / total;
                        isCancelled = EditorUtility.DisplayCancelableProgressBar("Classify_Hex",
                            $"Classifying... {processed}/{total}", progress);
                        if (isCancelled) break;

                        yield return null;
                    }
                }
                if (isCancelled) yield break;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Classify the specified cell based on neighbouring cells
        /// </summary>
        private static void Classify_Rect(Vector3Int cell, HashSet<Vector3Int> cells,
            Dictionary<ShapeType_Rect, ShapeSetting> settings, ShapeCells_Rect sc)
        {
            var offsets = GetNeighborOffsets_Rect();
            bool up    = cells.Contains(cell + offsets[0]);
            bool down  = cells.Contains(cell + offsets[1]);
            bool left  = cells.Contains(cell + offsets[2]);
            bool right = cells.Contains(cell + offsets[3]);
            bool anyV  = up   || down;
            bool anyH  = left || right;

            int neighborCount = (up?1:0) + (down?1:0) + (left?1:0) + (right?1:0);
            switch (neighborCount)
            {
                case 4: //Cross
                    ApplyShapeFlags_Rect(cell, settings[ShapeType_Rect.Cross].flags, sc, sc.Cross);
                    break;

                case 3: //T-Junction
                    ApplyShapeFlags_Rect(cell, settings[ShapeType_Rect.TJunction].flags, sc, sc.TJunction);
                    break;

                case 2 when anyV && anyH: //Corner
                    ApplyShapeFlags_Rect(cell, settings[ShapeType_Rect.Corner].flags, sc, sc.Corner);
                    break;

                default: //Vertical, Horizontal, Isolate
                    if      (anyV && !anyH) sc.Vertical.Add(cell);
                    else if (anyH && !anyV) sc.Horizontal.Add(cell);
                    else if (neighborCount == 0)
                        ApplyShapeFlags_Rect(cell, settings[ShapeType_Rect.Isolate].flags, sc, sc.Isolate);
                    break;
            }
        }

        private static void Classify_Hex(Vector3Int cell, int neighborCount,
            Dictionary<ShapeType_Hex, ShapeSetting> settings, ShapeCells_Hex sc)
        {
            switch (neighborCount)
            {
                case 6:  //Full
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Full].flags, sc.Full);
                    break;
                case 5:  //Junction5
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Junction5].flags, sc.Junction5);
                    break;
                case 4:  //Junction4
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Junction4].flags, sc.Junction4);
                    break;
                case 3:  //Junction3
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Junction3].flags, sc.Junction3);
                    break;
                case 2:  //Edge
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Edge].flags, sc.Edge);
                    break;
                case 1:  //Tip
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Tip].flags, sc.Tip);
                    break;
                default: //Isolate
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Isolate].flags, sc.Isolate);
                    break;
            }
        }

        /// <summary>
        /// Add to each collection according to the settings
        /// </summary>
        private static void ApplyShapeFlags_Hex(Vector3Int cell, ShapeFlags flags,
            List<Vector3Int> indepCells)
        {
            if (flags.HasFlag(ShapeFlags.Independent)) indepCells?.Add(cell);
        }

        /// <summary>
        /// Add to each collection according to the settings
        /// </summary>
        private static void ApplyShapeFlags_Rect(Vector3Int cell, ShapeFlags flags,
            ShapeCells_Rect sc, List<Vector3Int> indepCells)
        {
            if (flags.HasFlag(ShapeFlags.VerticalEdge))   sc.Vertical?.Add(cell);
            if (flags.HasFlag(ShapeFlags.HorizontalEdge)) sc.Horizontal?.Add(cell);
            if (flags.HasFlag(ShapeFlags.Independent))    indepCells?.Add(cell);
        }
    }
}
