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
        Full = 0,
        Junction,
        Corner,
        Edge,
        Tip,
        Isolate,
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
        public readonly List<Vector3Int> Full     = new();
        public readonly List<Vector3Int> Junction = new();
        public readonly List<Vector3Int> Corner   = new();
        public readonly List<Vector3Int> Edge     = new();
        public readonly List<Vector3Int> Tip      = new();
        public readonly List<Vector3Int> Isolate  = new();
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
            new(1, 0, 0), new(0, -1, 0), new(-1, -1, 0),
            new(-1, 0, 0), new(-1, 1, 0), new(0, 1, 0)
        };

        private static readonly Vector3Int[] neighbors_PointTop_Odd =
        {
            new(1, 0, 0), new(1, -1, 0), new(0, -1, 0),
            new(-1, 0, 0), new(0, 1, 0), new(1, 1, 0)
        };

        private static readonly Vector3Int[] neighbors_FlatTop_Even =
        {
            new(1, 0, 0), new(1, 1, 0), new(0, 1, 0),
            new(-1, 0, 0), new(0, -1, 0), new(1, -1, 0)
        };

        private static readonly Vector3Int[] neighbors_FlatTop_Odd =
        {
            new(1, 0, 0), new(0, 1, 0), new(-1, 1, 0),
            new(-1, 0, 0), new(-1, -1, 0), new(0, -1, 0)
        };

        private static bool IsPointTopLayout(GridLayout grid)
        {
            var c0 = grid.CellToWorld(Vector3Int.zero);
            var cUp = grid.CellToWorld(Vector3Int.up);
            var cRight = grid.CellToWorld(Vector3Int.right);
            float dxUp = Mathf.Abs(cUp.x - c0.x);
            float dxRight = Mathf.Abs(cRight.x - c0.x);
            return dxUp > dxRight;
        }

        private static IReadOnlyList<Vector3Int> GetNeighborOffsets_Hex(Vector3Int cell, bool isPointTop)
        {
            if (isPointTop)
            {
                return (cell.y & 1) == 0 ? neighbors_PointTop_Even : neighbors_PointTop_Odd;
            }
            else
            {
                return (cell.x & 1) == 0 ? neighbors_FlatTop_Even : neighbors_FlatTop_Odd;
            }
        }

        internal static IReadOnlyList<Vector3Int> GetNeighborOffsets_Rect() => neighbors_Rect;

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

            //Compress the Tilemap’s cellBounds to skip empty rows and columns
            source.CompressBounds();

            //Get the bounding box in cell coordinates and retrieve all tiles inside it(empty slots = null)
            var cellBounds    = source.cellBounds;
            var tilesInBounds = source.GetTilesBlock(cellBounds);

            //Only cells containing tiles are stored in the collection
            int width  = cellBounds.size.x;
            int height = cellBounds.size.y;
            var occupiedCells = new HashSet<Vector3Int>();

            bool isCancelled = false;
            try
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = x + y * width;
                        if (tilesInBounds[index] == null) continue;

                        //Calculate the world-space offset from the lower-left cell to origin
                        var cell = new Vector3Int(cellBounds.xMin + x,
                                                  cellBounds.yMin + y,
                                                  cellBounds.zMin);
                        occupiedCells.Add(cell);
                    }

                    if (y % batch == 0)
                    {
                        float progress = (float)(y * width) / (width * height);

                        isCancelled = EditorUtility.DisplayCancelableProgressBar("Classify_Rect",
                            "Collecting cells...", progress);
                        if (isCancelled) break;

                        yield return null;
                    }
                }
                if (isCancelled) yield break;

                var layout    = source.layoutGrid.cellLayout;
                int total     = occupiedCells.Count;
                int processed = 0;
                foreach (var cell in occupiedCells)
                {
                    //Perform proximity determination for each cell
                    ClassifyCellNeighbors(cell, occupiedCells, settings, sc);

                    processed++;
                    if (processed % batch == 0)
                    {
                        float progress = (float)processed / total;

                        isCancelled = EditorUtility.DisplayCancelableProgressBar("Classify_Rect",
                            $"Classifying... {processed}/{total}", progress);
                        if(isCancelled) break;

                        yield return null;
                    }
                }
                if(isCancelled) yield break;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static IEnumerator ClassifyCoroutine_Hex(Tilemap source,
            Dictionary<ShapeType_Hex, ShapeSetting> settings, ShapeCells_Hex sc, int batch = 100)
        {
            sc.Full.Clear();
            sc.Junction.Clear();
            sc.Corner.Clear();
            sc.Edge.Clear();
            sc.Tip.Clear();
            sc.Isolate.Clear();

            source.CompressBounds();

            var cellBounds    = source.cellBounds;
            var tilesInBounds = source.GetTilesBlock(cellBounds);

            int width  = cellBounds.size.x;
            int height = cellBounds.size.y;
            var occupiedCells = new HashSet<Vector3Int>();

            bool isCancelled = false;
            try
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = x + y * width;
                        if (tilesInBounds[index] == null) continue;

                        var cell = new Vector3Int(cellBounds.xMin + x,
                                                  cellBounds.yMin + y,
                                                  cellBounds.zMin);
                        occupiedCells.Add(cell);
                    }

                    if (y % batch == 0)
                    {
                        float progress = (float)(y * width) / (width * height);

                        isCancelled = EditorUtility.DisplayCancelableProgressBar(
                            "Classify_Rect", "Collecting cells...", progress);
                        if (isCancelled) break;

                        yield return null;
                    }
                }
                if (isCancelled) yield break;

                bool isPointTop = IsPointTopLayout(source.layoutGrid);
                int total     = occupiedCells.Count;
                int processed = 0;
                foreach (var cell in occupiedCells)
                {
                    var offsets = GetNeighborOffsets_Hex(cell, isPointTop);
                    var exist   = new bool[offsets.Count];
                    int count   = 0;
                    for (int i = 0; i < offsets.Count; i++)
                    {
                        bool e    = occupiedCells.Contains(cell + offsets[i]);
                        exist[i] = e;
                        if (e) count++;
                    }
                    Classify_Hex(cell, exist, count, settings, sc);

                    processed++;
                    if (processed % batch == 0)
                    {
                        float progress = (float)processed / total;

                        isCancelled = EditorUtility.DisplayCancelableProgressBar(
                            "Classify_Rect", $"Classifying... {processed}/{total}", progress);
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
        private static void ClassifyCellNeighbors(Vector3Int cell, HashSet<Vector3Int> cells,
            Dictionary<ShapeType_Rect, ShapeSetting> settings, ShapeCells_Rect sc)
        {
            var offsets = GetNeighborOffsets_Rect();
            var exist = new bool[offsets.Count];
            int count = 0;
            for (int i = 0; i < offsets.Count; i++)
            {
                exist[i] = cells.Contains(cell + offsets[i]);
                if (exist[i]) count++;
            }

            Classify_Rect(cell, exist, count, settings, sc);
        }

        private static void Classify_Rect(Vector3Int cell, bool[] exist,
            int neighborCount, Dictionary<ShapeType_Rect, ShapeSetting> settings, ShapeCells_Rect sc)
        {
            bool up    = exist[0];
            bool down  = exist[1];
            bool left  = exist[2];
            bool right = exist[3];
            bool anyV  = up || down;
            bool anyH  = left || right;

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


        private static void Classify_Hex(Vector3Int cell, bool[] exist, int neighborCount,
            Dictionary<ShapeType_Hex, ShapeSetting> settings, ShapeCells_Hex sc)
        {
            int mask = 0;
            for (int i = 0; i < exist.Length; i++)
            {
                if (exist[i]) mask |= 1 << i;
            }

            switch (neighborCount)
            {
                case 6:
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Full].flags, sc, sc.Full);
                    break;

                case 5:
                case 4:
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Junction].flags, sc, sc.Junction);
                    break;

                case 3:
                    if (HasConsecutiveBits(mask, 3))
                        ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Corner].flags, sc, sc.Corner);
                    else
                        ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Junction].flags, sc, sc.Junction);
                    break;

                case 2:
                    if (IsOpposite(mask))
                        ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Edge].flags, sc, sc.Edge);
                    else
                        ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Corner].flags, sc, sc.Corner);
                    break;

                case 1:
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Tip].flags, sc, sc.Tip);
                    break;

                default:
                    ApplyShapeFlags_Hex(cell, settings[ShapeType_Hex.Isolate].flags, sc, sc.Isolate);
                    break;
            }
        }

        private static bool HasConsecutiveBits(int mask, int length)
        {
            int bits = mask | (mask << 6);
            int seq  = (1 << length) - 1;
            for (int i = 0; i < 6; i++)
            {
                if ((bits & (seq << i)) == (seq << i)) return true;
            }
            return false;
        }

        private static bool IsOpposite(int mask)
        {
            const int pair0 = (1 << 0) | (1 << 3);
            const int pair1 = (1 << 1) | (1 << 4);
            const int pair2 = (1 << 2) | (1 << 5);
            return mask == pair0 || mask == pair1 || mask == pair2;
        }

        private static void ApplyShapeFlags_Hex(Vector3Int cell, ShapeFlags flags,
            ShapeCells_Hex sc, List<Vector3Int> indepCells)
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
