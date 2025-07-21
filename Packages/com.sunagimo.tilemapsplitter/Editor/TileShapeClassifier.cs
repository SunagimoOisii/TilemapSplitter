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

    internal enum ShapeType
    {
        VerticalEdge = 0,
        HorizontalEdge,
        Cross,
        TJunction,
        Corner,
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
    internal class ShapeCells
    {
        public readonly List<Vector3Int> VerticalCells   = new();
        public readonly List<Vector3Int> HorizontalCells = new();
        public readonly List<Vector3Int> CrossCells      = new();
        public readonly List<Vector3Int> TJunctionCells  = new();
        public readonly List<Vector3Int> CornerCells     = new();
        public readonly List<Vector3Int> IsolateCells    = new();
    }

    internal static class TileShapeClassifier
    {
        /// <summary>
        /// Compress the tilemap bounds to exclude empty rows and columns
        /// </summary>
        public static IEnumerator ClassifyCoroutine(Tilemap source, 
            Dictionary<ShapeType, ShapeSetting> settings, ShapeCells sc, int batch = 100)
        {
            sc.VerticalCells.Clear();
            sc.HorizontalCells.Clear();
            sc.CrossCells.Clear();
            sc.TJunctionCells.Clear();
            sc.CornerCells.Clear();
            sc.IsolateCells.Clear();

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

                        isCancelled = EditorUtility.DisplayCancelableProgressBar("Classify",
                            "Collecting cells...", progress);
                        if (isCancelled) break;

                        yield return null;
                    }
                }
                if (isCancelled) yield break;

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

                        isCancelled = EditorUtility.DisplayCancelableProgressBar("Classify",
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

        /// <summary>
        /// Classify the specified cell based on the four neighbouring cells
        /// </summary>
        private static void ClassifyCellNeighbors(Vector3Int cell, HashSet<Vector3Int> cells,
            Dictionary<ShapeType, ShapeSetting> settings, ShapeCells sc)
        {
            //Determine whether adjacent cells exist
            bool up    = cells.Contains(cell + Vector3Int.up);
            bool down  = cells.Contains(cell + Vector3Int.down);
            bool left  = cells.Contains(cell + Vector3Int.left);
            bool right = cells.Contains(cell + Vector3Int.right);
            bool anyV  = up || down;
            bool anyH  = left || right;

            //Add to collection by Classification
            int neighborCount  = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);
            switch (neighborCount)
            {
                case 4: //Cross
                    ApplyShapeFlags(cell, settings[ShapeType.Cross].flags, sc, sc.CrossCells);
                    break;
                case 3: //TJunction
                    ApplyShapeFlags(cell, settings[ShapeType.TJunction].flags, sc, sc.TJunctionCells);
                    break;
                case 2 when anyV && anyH: //Corner
                    ApplyShapeFlags(cell, settings[ShapeType.Corner].flags, sc, sc.CornerCells);
                    break;
                default:
                    if (anyV && anyH == false) //Vertical
                    {
                        sc.VerticalCells.Add(cell);
                    }
                    else if (anyH && anyV == false) //Horizontal
                    {
                        sc.HorizontalCells.Add(cell);
                    }
                    else if (neighborCount == 0) //Isolate
                    {
                        ApplyShapeFlags(cell, settings[ShapeType.Isolate].flags, sc, sc.IsolateCells);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add to each collection according to the settings
        /// </summary>
        private static void ApplyShapeFlags(Vector3Int cell, ShapeFlags flags,
            ShapeCells sc, List<Vector3Int> indepCells)
        {
            if (flags.HasFlag(ShapeFlags.VerticalEdge))   sc.VerticalCells?.Add(cell);
            if (flags.HasFlag(ShapeFlags.HorizontalEdge)) sc.HorizontalCells?.Add(cell);
            if (flags.HasFlag(ShapeFlags.Independent))    indepCells?.Add(cell);
        }
    }
}
