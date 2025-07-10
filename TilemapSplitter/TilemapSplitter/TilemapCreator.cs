using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapCreator
{
    private const string VerticalEdgeName   = "VerticalEdge";
    private const string HorizontalEdgeName = "HorizontalEdge";
    private const string CrossTileName      = "CrossTiles";
    private const string TJunctionTileName  = "TJunctionTiles";
    private const string CornerTileName     = "CornerTiles";
    private const string IsolateTileName    = "IsolateTiles";

    public static void GenerateSplitTilemaps(Tilemap original, TileShapeResult result,
        TileShapeSetting[] settings, bool mergeEdges)
    {
        if (mergeEdges)
        {
            var merged = new List<Vector3Int>(result.VerticalEdges);
            merged.AddRange(result.HorizontalEdges);
            var v = settings[(int)TileShapeType.VerticalEdge];
            PopulateTilemapForShape(original, TileShapeFlags.Independent, "EdgeTiles", merged, v.layer, v.tag);
        }
        else
        {
            var v = settings[(int)TileShapeType.VerticalEdge];
            var h = settings[(int)TileShapeType.HorizontalEdge];
            PopulateTilemapForShape(original, v.flags, VerticalEdgeName, result.VerticalEdges, v.layer, v.tag);
            PopulateTilemapForShape(original, h.flags, HorizontalEdgeName, result.HorizontalEdges, h.layer, h.tag);
        }

        var cross   = settings[(int)TileShapeType.Cross];
        var t       = settings[(int)TileShapeType.TJunction];
        var corner  = settings[(int)TileShapeType.Corner];
        var isolate = settings[(int)TileShapeType.Isolate];

        PopulateTilemapForShape(original, cross.flags,   CrossTileName,     result.CrossTiles,   cross.layer,   cross.tag);
        PopulateTilemapForShape(original, t.flags,       TJunctionTileName, result.TJunctionTiles, t.layer,       t.tag);
        PopulateTilemapForShape(original, corner.flags,  CornerTileName,    result.CornerTiles,  corner.layer,  corner.tag);
        PopulateTilemapForShape(original, isolate.flags, IsolateTileName,   result.IsolateTiles, isolate.layer, isolate.tag);
    }

    /// <summary>
    /// タイル座標リスト通りの Tilemap を持つ GameObject を生成
    /// </summary>
    private static void PopulateTilemapForShape(Tilemap original, TileShapeFlags opt, string name,
        List<Vector3Int> tilePositions, int layer, string tag)
    {
        if (tilePositions == null || 
            tilePositions.Count == 0) return;

        //Independent が必要な場合、設定になければ生成中断
        bool isRequiredIndependentOption = name == CrossTileName  || name == TJunctionTileName ||
                                           name == CornerTileName || name == IsolateTileName;
        if (isRequiredIndependentOption && 
            opt.HasFlag(TileShapeFlags.Independent) == false) return;

        //Tilemap, TilemapRenderer を持つ GameObject 生成
        //生成元の Transform 設定を引継ぎつつ、レイヤー等を指定のものに変更
        var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(original.transform.parent, false);
        obj.transform.SetLocalPositionAndRotation(original.transform.localPosition, 
            original.transform.localRotation);
        obj.transform.localScale = original.transform.localScale;
        obj.layer = layer;
        obj.tag   = tag;

        //分割元に TilemapRenderer があればその設定を一致させる
        var renderer = obj.GetComponent<TilemapRenderer>();
        if (original.TryGetComponent<TilemapRenderer>(out var oriRenderer))
        {
            renderer.sortingLayerID = oriRenderer.sortingLayerID;
            renderer.sortingOrder   = oriRenderer.sortingOrder;
        }
        else
        {
            Debug.LogWarning("Since TilemapRenderer is not attached to the split target, " +
                "the TilemapRenderer of the generated object was generated with the default settings.");
        }

        //生成元タイルの情報を、対応する生成タイルへコピー
        var tm = obj.GetComponent<Tilemap>();
        foreach (var p in tilePositions)
        {
            tm.SetTile(p, original.GetTile(p));
            tm.SetColor(p, original.GetColor(p));
            tm.SetTransformMatrix(p,original.GetTransformMatrix(p));
        }

        Undo.RegisterCreatedObjectUndo(obj, "GenerateSplitTilemaps " + name);
    }
}
