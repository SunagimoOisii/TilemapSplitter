namespace TilemapSplitter
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    [Flags]
    internal enum TileShapeFlags
    {
        VerticalEdge   = 1 << 0,
        HorizontalEdge = 1 << 1,
        Independent    = 1 << 2,
    }

    internal enum TileShapeType
    {
        VerticalEdge = 0,
        HorizontalEdge,
        Cross,
        TJunction,
        Corner,
        Isolate,
    }

    internal class TileShapeSetting
    {
        public TileShapeFlags flags;
        public int    layer;
        public string tag        = "Untagged";
        public bool   canPreview = true;
        public Color  previewColor;
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
        public static ShapeCells Classify(Tilemap original, TileShapeSetting[] settings)
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
            TileShapeSetting[] settings, ShapeCells result)
        {
            //Determine whether adjacent cells exist
            bool up    = cells.Contains(cell + Vector3Int.up);
            bool down  = cells.Contains(cell + Vector3Int.down);
            bool left  = cells.Contains(cell + Vector3Int.left);
            bool right = cells.Contains(cell + Vector3Int.right);
            bool anyV  = up || down;
            bool anyH  = left || right;
            int count  = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

            //Add to collection by Classification
            if (count == 4) //Cross
            {
                ApplyShapeFlags(cell, settings[(int)TileShapeType.Cross].flags,
                    result.CrossCells, result.VerticalEdgesCells, result.HorizontalEdgesCells);
            }
            else if (count == 3) //TJunction
            {
                ApplyShapeFlags(cell, settings[(int)TileShapeType.TJunction].flags,
                    result.TJunctionCells, result.VerticalEdgesCells, result.HorizontalEdgesCells);
            }
            else if (count == 2 && //Corner
                     anyV &&
                     anyH)
            {
                ApplyShapeFlags(cell, settings[(int)TileShapeType.Corner].flags,
                    result.CornerCells, result.VerticalEdgesCells, result.HorizontalEdgesCells);
            }
            else if (anyV && //VerticalEdge
                     anyH == false)
            {
                result.VerticalEdgesCells.Add(cell);
            }
            else if (anyH && //HorizontalEdge
                     anyV == false)
            {
                result.HorizontalEdgesCells.Add(cell);
            }
            else if (count == 0) //Isolate
            {
                ApplyShapeFlags(cell, settings[(int)TileShapeType.Isolate].flags,
                    result.IsolateCells, result.VerticalEdgesCells, result.HorizontalEdgesCells);
            }
        }

        /// <summary>
        /// Add to each collection according to the settings
        /// </summary>
        private static void ApplyShapeFlags(Vector3Int cell, TileShapeFlags flags,
            List<Vector3Int> indepCellList, List<Vector3Int> vCellList, List<Vector3Int> hCellList)
        {
            if (flags.HasFlag(TileShapeFlags.VerticalEdge)) vCellList?.Add(cell);
            if (flags.HasFlag(TileShapeFlags.HorizontalEdge)) hCellList?.Add(cell);
            if (flags.HasFlag(TileShapeFlags.Independent)) indepCellList?.Add(cell);
        }
    }
}
