namespace TilemapSplitter
{
    using System;
    using System.Collections.Generic;
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
        public readonly List<Vector3Int> VerticalEdgesCells   = new();
        public readonly List<Vector3Int> HorizontalEdgesCells = new();
        public readonly List<Vector3Int> CrossCells           = new();
        public readonly List<Vector3Int> TJunctionCells       = new();
        public readonly List<Vector3Int> CornerCells          = new();
        public readonly List<Vector3Int> IsolateCells         = new();
    }

    internal static class TileShapeClassifier
    {
        /// <summary>
        /// Compress the tilemap bounds to exclude empty rows and columns
        /// </summary>
        public static ShapeCells Classify(Tilemap original, Dictionary<ShapeType, ShapeSetting> settings)
        {
            var result = new ShapeCells();

            //Compress the Tilemap’s cellBounds to skip empty rows and columns
            original.CompressBounds();

            //Get the bounding box in cell coordinates and retrieve all tiles inside it(empty slots = null)
            var cellBounds    = original.cellBounds;
            var tilesInBounds = original.GetTilesBlock(cellBounds);

            //Only cells containing tiles are stored in the collection
            int width         = cellBounds.size.x;
            int height        = cellBounds.size.y;
            var occupiedCells = new HashSet<Vector3Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    if (tilesInBounds[index] == null) continue;

                    //Calculate the world-space offset from the lower-left cell to origin
                    var cell = new Vector3Int(cellBounds.xMin + x, cellBounds.yMin + y, cellBounds.zMin);
                    occupiedCells.Add(cell);
                }
            }

            //Perform proximity determination for each cell
            foreach (var cell in occupiedCells)
            {
                ClassifyCellNeighbors(cell, occupiedCells, settings, result);
            }

            return result;
        }

        /// <summary>
        /// Classify the specified cell based on the four neighbouring cells
        /// </summary>
        private static void ClassifyCellNeighbors(Vector3Int cell, HashSet<Vector3Int> cells,
            Dictionary<ShapeType, ShapeSetting> settings, ShapeCells result)
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
                    ApplyShapeFlags(cell, settings[ShapeType.Cross].flags, result);
                    break;
                case 3: //TJunction
                    ApplyShapeFlags(cell, settings[ShapeType.TJunction].flags, result);
                    break;
                case 2 when anyV && anyH: //Corner
                    ApplyShapeFlags(cell, settings[ShapeType.Corner].flags, result);
                    break;
                default:
                    if (anyV && anyH == false) //Vertical
                    {
                        result.VerticalEdgesCells.Add(cell);
                    }
                    else if (anyH && anyV == false) //Horizontal
                    {
                        result.HorizontalEdgesCells.Add(cell);
                    }
                    else if (neighborCount == 0) //Isolate
                    {
                        ApplyShapeFlags(cell, settings[ShapeType.Isolate].flags, result);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add to each collection according to the settings
        /// </summary>
        private static void ApplyShapeFlags(Vector3Int cell, ShapeFlags flags, ShapeCells sc)
        {
            if (flags.HasFlag(ShapeFlags.VerticalEdge))   sc.VerticalEdgesCells?.Add(cell);
            if (flags.HasFlag(ShapeFlags.HorizontalEdge)) sc.HorizontalEdgesCells?.Add(cell);
            if (flags.HasFlag(ShapeFlags.Independent))    sc.IsolateCells?.Add(cell);
        }
    }
}
