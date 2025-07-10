using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class TilemapCreator
{
    private const string VerticalEdgeName   = "VerticalEdge";
    private const string HorizontalEdgeName = "HorizontalEdge";
    private const string CrossTileName      = "CrossTiles";
    private const string TJunctionTileName  = "TJunctionTiles";
    private const string CornerTileName     = "CornerTiles";
    private const string IsolateTileName    = "IsolateTiles";

    public static void GenerateSplitTilemaps(Tilemap original, ShapeCells sCells,
        TileShapeSetting[] settings, bool mergeEdges)
    {
        if (mergeEdges)
        {
            var merged = new List<Vector3Int>(sCells.VerticalEdgesCells);
            merged.AddRange(sCells.HorizontalEdgesCells);
            var v = settings[(int)TileShapeType.VerticalEdge];
            CreateTilemapObjForCells(original, TileShapeFlags.Independent, "EdgeTiles", merged,
                v.layer, v.tag);
        }
        else
        {
            var v = settings[(int)TileShapeType.VerticalEdge];
            var h = settings[(int)TileShapeType.HorizontalEdge];
            CreateTilemapObjForCells(original, v.flags, VerticalEdgeName,   sCells.VerticalEdgesCells,
                v.layer, v.tag);
            CreateTilemapObjForCells(original, h.flags, HorizontalEdgeName, sCells.HorizontalEdgesCells,
                h.layer, h.tag);
        }

        var cross   = settings[(int)TileShapeType.Cross];
        var t       = settings[(int)TileShapeType.TJunction];
        var corner  = settings[(int)TileShapeType.Corner];
        var isolate = settings[(int)TileShapeType.Isolate];

        CreateTilemapObjForCells(original, cross.flags,   CrossTileName,     sCells.CrossCells,
            cross.layer,   cross.tag);
        CreateTilemapObjForCells(original, t.flags,       TJunctionTileName, sCells.TJunctionCells,
            t.layer,       t.tag);
        CreateTilemapObjForCells(original, corner.flags,  CornerTileName,    sCells.CornerCells,
            corner.layer,  corner.tag);
        CreateTilemapObjForCells(original, isolate.flags, IsolateTileName,   sCells.IsolateCells,
            isolate.layer, isolate.tag);
    }

    private static void CreateTilemapObjForCells(Tilemap original, TileShapeFlags flags, string name,
        List<Vector3Int> cells, int layer, string tag)
    {
        if (cells == null || 
            cells.Count == 0) return;

        //If Independent is required,
        //generation will be interrupted if it is not specified in the settings
        bool isRequiredIndependentFlag = name == CrossTileName  || name == TJunctionTileName ||
                                         name == CornerTileName || name == IsolateTileName;
        if (isRequiredIndependentFlag && 
            flags.HasFlag(TileShapeFlags.Independent) == false) return;

        //Create a GameObject with Tilemap and TilemapRenderer
        //Change layers, etc. to specified values while inheriting the Transform settings from the source
        var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(original.transform.parent, false);
        obj.transform.SetLocalPositionAndRotation(original.transform.localPosition, 
            original.transform.localRotation);
        obj.transform.localScale = original.transform.localScale;
        obj.layer = layer;
        obj.tag   = tag;

        //If there is a TilemapRenderer in the original, match its settings.
        var renderer = obj.GetComponent<TilemapRenderer>();
        if (original.TryGetComponent<TilemapRenderer>(out var oriRenderer))
        {
            renderer.sortingLayerID = oriRenderer.sortingLayerID;
            renderer.sortingOrder   = oriRenderer.sortingOrder;
        }
        else
        {
            Debug.LogWarning("Since TilemapRenderer is not attached to the split target, " +
                "the TilemapRenderer of the generated object was generated with the default shapeSettings.");
        }

        //Copy the information from the source tile to the corresponding generated tile
        var tm = obj.GetComponent<Tilemap>();
        foreach (var cell in cells)
        {
            tm.SetTile(cell, original.GetTile(cell));
            tm.SetColor(cell, original.GetColor(cell));
            tm.SetTransformMatrix(cell,original.GetTransformMatrix(cell));
        }

        Undo.RegisterCreatedObjectUndo(obj, "GenerateSplitTilemaps " + name);
    }
}
