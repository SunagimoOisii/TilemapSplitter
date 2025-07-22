namespace TilemapSplitter
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    internal static class TilemapCreator
    {
        private const string VerticalObjName   = "VerticalEdge";
        private const string HorizontalObjName = "HorizontalEdge";
        private const string MergeObjName      = "EdgeTiles";
        private const string CrossObjName      = "CrossTiles";
        private const string TJunctionObjName  = "TJunctionTiles";
        private const string CornerObjName     = "CornerTiles";
        private const string IsolateObjName    = "IsolateTiles";

        public static void GenerateSplitTilemaps(Tilemap source, ShapeCells sc,
            Dictionary<ShapeType, ShapeSetting> settings, bool mergeEdges, bool canAttachCollider)
        {
            if (mergeEdges)
            {
                var mergedCells = new List<Vector3Int>(sc.VerticalCells);
                var v           = settings[ShapeType.VerticalEdge];
                v.flags         = ShapeFlags.Independent;
                mergedCells.AddRange(sc.HorizontalCells);
                CreateTilemapObjForCells(source, mergedCells, v, MergeObjName, canAttachCollider);
            }
            else
            {
                var v = settings[ShapeType.VerticalEdge];
                var h = settings[ShapeType.HorizontalEdge];
                CreateTilemapObjForCells(source, sc.VerticalCells,   v, VerticalObjName,   canAttachCollider);
                CreateTilemapObjForCells(source, sc.HorizontalCells, h, HorizontalObjName, canAttachCollider);
            }

            var cross   = settings[ShapeType.Cross];
            var t       = settings[ShapeType.TJunction];
            var corner  = settings[ShapeType.Corner];
            var isolate = settings[ShapeType.Isolate];
            CreateTilemapObjForCells(source, sc.CrossCells,     cross,   CrossObjName,     canAttachCollider);
            CreateTilemapObjForCells(source, sc.TJunctionCells, t,       TJunctionObjName, canAttachCollider);
            CreateTilemapObjForCells(source, sc.CornerCells,    corner,  CornerObjName,    canAttachCollider);
            CreateTilemapObjForCells(source, sc.IsolateCells,   isolate, IsolateObjName,   canAttachCollider);
        }

        private static void CreateTilemapObjForCells(Tilemap source,
            List<Vector3Int> cells, ShapeSetting setting, string name, bool canAttachCollider)
        {
            if (cells == null || cells.Count == 0) return;

            //Skip instantiating this tile collection when the Independent flag is not enabled in settings
            bool isRequiredIndependentFlag = name == CrossObjName  || name == TJunctionObjName ||
                                             name == CornerObjName || name == IsolateObjName;
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
                    "the TilemapRenderer of the generated object was generated with the default shapeSettings.");
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

            Undo.RegisterCreatedObjectUndo(obj, "GenerateSplitTilemaps " + name);
        }
    }
}
