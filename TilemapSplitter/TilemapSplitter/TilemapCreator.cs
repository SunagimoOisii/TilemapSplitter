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

            var rectShapes = new[]
            {
                (sc.Cross,     ShapeType_Rect.Cross,     CrossObjName),
                (sc.TJunction, ShapeType_Rect.TJunction, TJunctionObjName),
                (sc.Corner,    ShapeType_Rect.Corner,    CornerObjName),
                (sc.Isolate,   ShapeType_Rect.Isolate,   IsolateObjName)
            };

            foreach (var (cells, type, objName) in rectShapes)
            {
                CreateTilemapObjForCells(source, cells, settings[type], objName, canAttachCollider);
            }
        }

        public static void GenerateSplitTilemaps_Hex(Tilemap source, ShapeCells_Hex sc,
            Dictionary<ShapeType_Hex, ShapeSetting> settings, bool canAttachCollider)
        {
            var hexShapes = new[]
            {
                (sc.Full,      ShapeType_Hex.Full,      FullObjName),
                (sc.Junction5, ShapeType_Hex.Junction5, Junction5ObjName),
                (sc.Junction4, ShapeType_Hex.Junction4, Junction4ObjName),
                (sc.Junction3, ShapeType_Hex.Junction3, Junction3ObjName),
                (sc.Edge,      ShapeType_Hex.Edge,      HexEdgeObjName),
                (sc.Tip,       ShapeType_Hex.Tip,       TipObjName),
                (sc.Isolate,   ShapeType_Hex.Isolate,   IsolateObjName)
            };

            foreach (var (cells, type, objName) in hexShapes)
            {
                CreateTilemapObjForCells(source, cells, settings[type], objName, canAttachCollider);
            }
        }

        private static void CreateTilemapObjForCells(Tilemap source,
            List<Vector3Int> cells, ShapeSetting setting, string name, bool canAttachCollider)
        {
            if (cells.Count == 0) return;

            //Skip instantiating this tile collection when the Independent flag is not enabled in settings
            if (RequiresIndependent(name) &&
                setting.flags.HasFlag(ShapeFlags.Independent) == false) return;

            //Instantiate a GameObject with Tilemap and TilemapRenderer components attached
            //Copy the source transform(position, rotation, scale) and apply the specified layer and tag
            var obj = CreateTilemapObject(name, setting, source);

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
                var tmCol = obj.AddComponent<TilemapCollider2D>();
                tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;

                var rb = obj.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;

                obj.AddComponent<CompositeCollider2D>();
            }

            Undo.RegisterCreatedObjectUndo(obj, "GenerateSplitTilemaps_Rect " + name);
        }

        private static GameObject CreateTilemapObject(string name, ShapeSetting setting, Tilemap source)
        {
            var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer))
            {
                layer = setting.layer,
                tag   = setting.tag
            };

            obj.transform.localScale = source.transform.localScale;
            obj.transform.SetLocalPositionAndRotation(source.transform.localPosition,
                source.transform.localRotation);
            obj.transform.SetParent(source.transform.parent, false);

            return obj;
        }

        private static bool RequiresIndependent(string name) =>
            name is CrossObjName or TJunctionObjName or CornerObjName or IsolateObjName
                or FullObjName or Junction5ObjName or Junction4ObjName
                or Junction3ObjName or HexEdgeObjName or TipObjName;
    }
}
