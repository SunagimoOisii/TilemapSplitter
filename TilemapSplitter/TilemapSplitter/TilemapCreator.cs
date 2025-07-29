namespace TilemapSplitter
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    internal static class TilemapCreator
    {
        //For Rectangle or Isolate
        private const string VerticalObjName   = "VerticalEdge";
        private const string HorizontalObjName = "HorizontalEdge";
        private const string MergeObjName      = "EdgeTiles";
        private const string CrossObjName      = "CrossTiles";
        private const string TJunctionObjName  = "TJunctionTiles";
        private const string CornerObjName     = "CornerTiles";
        private const string IsolateObjName    = "IsolateTiles";
        //For Hexagon
        private const string FullObjName      = "FullTiles";
        private const string Junction5ObjName = "Junction5Tiles";
        private const string Junction4ObjName = "Junction4Tiles";
        private const string Junction3ObjName = "Junction3Tiles";
        private const string HexEdgeObjName   = "EdgeTiles";
        private const string TipObjName       = "TipTiles";

        public static void GenerateSplitTilemaps_Rect(Tilemap source, ShapeCells_Rect sc,
            Dictionary<ShapeType_Rect, ShapeSetting> settings, bool mergeEdges, bool canAttachCollider)
        {
            if (mergeEdges)
            {
                var mergedCells = new List<Vector3Int>(sc.Vertical);
                var v           = settings[ShapeType_Rect.VerticalEdge];
                v.flags         = ShapeFlags.Independent;
                mergedCells.AddRange(sc.Horizontal);
                CreateTilemapObjForCells(source, mergedCells, v, MergeObjName, canAttachCollider);
            }
            else
            {
                var v = settings[ShapeType_Rect.VerticalEdge];
                var h = settings[ShapeType_Rect.HorizontalEdge];
                CreateTilemapObjForCells(source, sc.Vertical,   v, VerticalObjName,   canAttachCollider);
                CreateTilemapObjForCells(source, sc.Horizontal, h, HorizontalObjName, canAttachCollider);
            }

            var cross   = settings[ShapeType_Rect.Cross];
            var t       = settings[ShapeType_Rect.TJunction];
            var corner  = settings[ShapeType_Rect.Corner];
            var isolate = settings[ShapeType_Rect.Isolate];
            CreateTilemapObjForCells(source, sc.Cross,     cross,   CrossObjName,     canAttachCollider);
            CreateTilemapObjForCells(source, sc.TJunction, t,       TJunctionObjName, canAttachCollider);
            CreateTilemapObjForCells(source, sc.Corner,    corner,  CornerObjName,    canAttachCollider);
            CreateTilemapObjForCells(source, sc.Isolate,   isolate, IsolateObjName,   canAttachCollider);
        }

        public static void GenerateSplitTilemaps_Hex(Tilemap source, ShapeCells_Hex sc,
            Dictionary<ShapeType_Hex, ShapeSetting> settings, bool canAttachCollider)
        {
            CreateTilemapObjForCells(source, sc.Full,      settings[ShapeType_Hex.Full],      FullObjName,      canAttachCollider);
            CreateTilemapObjForCells(source, sc.Junction5, settings[ShapeType_Hex.Junction5], Junction5ObjName, canAttachCollider);
            CreateTilemapObjForCells(source, sc.Junction4, settings[ShapeType_Hex.Junction4], Junction4ObjName, canAttachCollider);
            CreateTilemapObjForCells(source, sc.Junction3, settings[ShapeType_Hex.Junction3], Junction3ObjName, canAttachCollider);
            CreateTilemapObjForCells(source, sc.Edge,      settings[ShapeType_Hex.Edge],      HexEdgeObjName,   canAttachCollider);
            CreateTilemapObjForCells(source, sc.Tip,       settings[ShapeType_Hex.Tip],       TipObjName,       canAttachCollider);
            CreateTilemapObjForCells(source, sc.Isolate,   settings[ShapeType_Hex.Isolate],   IsolateObjName,   canAttachCollider);
        }

        private static void CreateTilemapObjForCells(Tilemap source,
            List<Vector3Int> cells, ShapeSetting setting, string name, bool canAttachCollider)
        {
            if (cells == null || cells.Count == 0) return;

            //Skip instantiating this tile collection when the Independent flag is not enabled in settings
            bool isRequiredIndependentFlag = name == CrossObjName     || name == TJunctionObjName ||
                                             name == CornerObjName    || name == IsolateObjName   ||
                                             name == FullObjName      || name == Junction5ObjName ||
                                             name == Junction4ObjName || name == Junction3ObjName ||
                                             name == HexEdgeObjName   || name == TipObjName;
            if (isRequiredIndependentFlag &&
                setting.flags.HasFlag(ShapeFlags.Independent) == false) return;

            //Instantiate a GameObject with Tilemap and TilemapRenderer components attached
            //Copy the source transform(position, rotation, scale) and apply the specified layer and tag
            var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            obj.layer                = setting.layer;
            obj.tag                  = setting.tag;
            obj.transform.localScale = source.transform.localScale;
            obj.transform.SetLocalPositionAndRotation(source.transform.localPosition,
                source.transform.localRotation);
            obj.transform.SetParent(source.transform.parent, false);

            //If there is a TilemapRenderer in the source, match its settings.
            var renderer = obj.GetComponent<TilemapRenderer>();
            if (source.TryGetComponent<TilemapRenderer>(out var oriRenderer))
            {
                renderer.sortingLayerID = oriRenderer.sortingLayerID;
                renderer.sortingOrder   = oriRenderer.sortingOrder;
                renderer.sortOrder      = oriRenderer.sortOrder;
                renderer.mode           = oriRenderer.mode;
            }
            else
            {
                Debug.LogWarning(
                    "Since TilemapRenderer is not attached to the split target, " +
                    "the TilemapRenderer of the generated object was generated with the default shapeSettings_Rect.");
            }

            //Transfer tile data(sprite, color, transform matrix) from the original to the new tile
            var tm = obj.GetComponent<Tilemap>();
            tm.tileAnchor = source.tileAnchor;
            foreach (var cell in cells)
            {
                tm.SetTile(cell,  source.GetTile(cell));
                tm.SetColor(cell, source.GetColor(cell));
                tm.SetTransformMatrix(cell, source.GetTransformMatrix(cell));
            }

            if (canAttachCollider)
            {
                var tmCol                = obj.AddComponent<TilemapCollider2D>();
                tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;

                var rb = obj.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;

                obj.AddComponent<CompositeCollider2D>();
            }

            Undo.RegisterCreatedObjectUndo(obj, "GenerateSplitTilemaps_Rect " + name);
        }
    }
}
