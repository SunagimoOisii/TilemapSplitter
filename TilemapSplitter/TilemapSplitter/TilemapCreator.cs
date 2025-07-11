namespace TilemapSplitter
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    internal static class TilemapCreator
    {
        private const string VerticalEdgeName   = "VerticalEdge";
        private const string HorizontalEdgeName = "HorizontalEdge";
        private const string MergeTileName      = "EdgeTiles";
        private const string CrossTileName      = "CrossTiles";
        private const string TJunctionTileName  = "TJunctionTiles";
        private const string CornerTileName     = "CornerTiles";
        private const string IsolateTileName    = "IsolateTiles";

        public static void GenerateSplitTilemaps(Tilemap original, ShapeCells sCells,
            Dictionary<ShapeType, ShapeSetting> settings, bool mergeEdges)
        {
            if (mergeEdges)
            {
                var mergedCells = new List<Vector3Int>(sCells.VerticalEdgesCells);
                var v           = settings[ShapeType.VerticalEdge];
                mergedCells.AddRange(sCells.HorizontalEdgesCells);
                CreateTilemapObjForCells(original, ShapeFlags.Independent, MergeTileName,
                    mergedCells, v.layer, v.tag);
            }
            else
            {
                var v = settings[ShapeType.VerticalEdge];
                var h = settings[ShapeType.HorizontalEdge];
                CreateTilemapObjForCells(original, v.flags, VerticalEdgeName,
                    sCells.VerticalEdgesCells, v.layer, v.tag);
                CreateTilemapObjForCells(original, h.flags, HorizontalEdgeName,
                    sCells.HorizontalEdgesCells, h.layer, h.tag);
            }

            var cross   = settings[ShapeType.Cross];
            var t       = settings[ShapeType.TJunction];
            var corner  = settings[ShapeType.Corner];
            var isolate = settings[ShapeType.Isolate];

            CreateTilemapObjForCells(original, cross.flags, CrossTileName,
                sCells.CrossCells, cross.layer, cross.tag);
            CreateTilemapObjForCells(original, t.flags, TJunctionTileName,
                sCells.TJunctionCells, t.layer, t.tag);
            CreateTilemapObjForCells(original, corner.flags, CornerTileName,
                sCells.CornerCells, corner.layer, corner.tag);
            CreateTilemapObjForCells(original, isolate.flags, IsolateTileName,
                sCells.IsolateCells, isolate.layer, isolate.tag);
        }

        private static void CreateTilemapObjForCells(Tilemap original, ShapeFlags flags,
            string name, List<Vector3Int> cells, int layer, string tag)
        {
            if (cells == null ||
                cells.Count == 0) return;

            //Skip instantiating this tile collection when the Independent flag is not enabled in settings
            bool isRequiredIndependentFlag = name == CrossTileName  || name == TJunctionTileName ||
                                             name == CornerTileName || name == IsolateTileName;
            if (isRequiredIndependentFlag &&
                flags.HasFlag(ShapeFlags.Independent) == false) return;

            //Instantiate a GameObject with Tilemap and TilemapRenderer components attached
            //Copy the original transform(position, rotation, scale) and apply the specified layer and tag
            var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            obj.layer                = layer;
            obj.tag                  = tag;
            obj.transform.localScale = original.transform.localScale;
            obj.transform.SetLocalPositionAndRotation(original.transform.localPosition,
                original.transform.localRotation);
            obj.transform.SetParent(original.transform.parent, false);

            //If there is a TilemapRenderer in the original, match its settings.
            var renderer = obj.GetComponent<TilemapRenderer>();
            if (original.TryGetComponent<TilemapRenderer>(out var oriRenderer))
            {
                renderer.sortingLayerID = oriRenderer.sortingLayerID;
                renderer.sortingOrder = oriRenderer.sortingOrder;
            }
            else
            {
                Debug.LogWarning("Since TilemapRenderer is not attached to the split target, " +
                    "the TilemapRenderer of the generated object was generated with the default shapeSettings.");
            }

            //Transfer tile data(sprite, color, transform matrix) from the original to the new tile
            var tm = obj.GetComponent<Tilemap>();
            foreach (var cell in cells)
            {
                tm.SetTile(cell,  original.GetTile(cell));
                tm.SetColor(cell, original.GetColor(cell));
                tm.SetTransformMatrix(cell, original.GetTransformMatrix(cell));
            }

            Undo.RegisterCreatedObjectUndo(obj, "GenerateSplitTilemaps " + name);
        }
    }
}
