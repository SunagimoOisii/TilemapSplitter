using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Flags]
public enum ClassificationOption
{
    VerticalEdge   = 1 << 0,
    HorizontalEdge = 1 << 1,
    Independent    = 1 << 2,
}

public enum SettingType
{
    VerticalEdge = 0,
    HorizontalEdge,
    Cross,
    TJunction,
    Corner,
    Isolate,
}

public class ClassificationSetting
{
    public ClassificationOption option;
    public int    layer;
    public string tag        = "Untagged";
    public bool   canPreview = true;
    public Color  color;
}

/// <summary>
/// 各分類に沿ったリストへタイルが格納される
/// </summary>
public class ClassificationResult
{
    public readonly List<Vector3Int> VerticalEdges   = new();
    public readonly List<Vector3Int> HorizontalEdges = new();
    public readonly List<Vector3Int> CrossTiles      = new();
    public readonly List<Vector3Int> TJunctionTiles  = new();
    public readonly List<Vector3Int> CornerTiles     = new();
    public readonly List<Vector3Int> IsolateTiles    = new();
}

public static class TilemapClassifier
{
    /// <summary>
    /// Tilemap のタイル配置を解析し分類結果を返す
    /// </summary>
    public static ClassificationResult Classify(Tilemap tilemap, ClassificationSetting[] settings)
    {
        var result = new ClassificationResult();

        //空セル分だけ cellBounds 縮小
        tilemap.CompressBounds();

        //セル座標空間における境界ボックスと、その中の全タイルを取得(空白セル分は null)
        var cellBounds    = tilemap.cellBounds;
        var tilesInBounds = tilemap.GetTilesBlock(cellBounds);

        //タイルが存在するセルのみコレクションに格納
        int width  = cellBounds.size.x;
        int height = cellBounds.size.y;
        var occupiedCellPositions = new HashSet<Vector3Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;
                if (tilesInBounds[index] == null) continue;

                //Bounds の最小座標をオフセットとしてワールド座標へ変換
                var pos = new Vector3Int(cellBounds.xMin + x, cellBounds.yMin + y, cellBounds.zMin);
                occupiedCellPositions.Add(pos);
            }
        }

        //各タイルの近傍判定
        foreach (var pos in occupiedCellPositions)
        {
            ClassifyTileNeighbors(pos, occupiedCellPositions, settings, result);
        }

        return result;
    }

    /// <summary>
    /// 指定セルの4近傍から分類を行う
    /// </summary>
    private static void ClassifyTileNeighbors(Vector3Int pos, HashSet<Vector3Int> tiles,
        ClassificationSetting[] settings, ClassificationResult result)
    {
        //隣接タイルの有無を調査
        bool up    = tiles.Contains(pos + Vector3Int.up);
        bool down  = tiles.Contains(pos + Vector3Int.down);
        bool left  = tiles.Contains(pos + Vector3Int.left);
        bool right = tiles.Contains(pos + Vector3Int.right);
        bool anyV  = up   || down;
        bool anyH  = left || right;
        int count  = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

        //分類ごとにリストへ追加する
        if (count == 4) //Cross
        {
            ApplyClassification(pos, settings[(int)SettingType.Cross].option,
                result.CrossTiles, result.VerticalEdges, result.HorizontalEdges);
        }
        else if (count == 3) //TJunction
        {
            ApplyClassification(pos, settings[(int)SettingType.TJunction].option,
                result.TJunctionTiles, result.VerticalEdges, result.HorizontalEdges);
        }
        else if (count == 2 && //Corner
                 anyV &&
                 anyH)
        {
            ApplyClassification(pos, settings[(int)SettingType.Corner].option,
                result.CornerTiles, result.VerticalEdges, result.HorizontalEdges);
        }
        else if (anyV && //VerticalEdge
                 anyH == false)
        {
            result.VerticalEdges.Add(pos);
        }
        else if (anyH && //HorizontalEdge
                 anyV == false)
        {
            result.HorizontalEdges.Add(pos);
        }
        else if (count == 0) //Isolate
        {
            ApplyClassification(pos, settings[(int)SettingType.Isolate].option,
                result.IsolateTiles, result.VerticalEdges, result.HorizontalEdges);
        }
    }

    /// <summary>
    /// 設定に従って各リストへ追加
    /// </summary>
    private static void ApplyClassification(Vector3Int pos, ClassificationOption opt,
        List<Vector3Int> indep, List<Vector3Int> vList, List<Vector3Int> hList)
    {
        if (opt.HasFlag(ClassificationOption.VerticalEdge))   vList?.Add(pos);
        if (opt.HasFlag(ClassificationOption.HorizontalEdge)) hList?.Add(pos);
        if (opt.HasFlag(ClassificationOption.Independent))    indep?.Add(pos);
    }
}
