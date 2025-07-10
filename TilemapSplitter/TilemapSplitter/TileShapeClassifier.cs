using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Flags]
public enum TileShapeFlags
{
    VerticalEdge   = 1 << 0,
    HorizontalEdge = 1 << 1,
    Independent    = 1 << 2,
}

public enum TileShapeType
{
    VerticalEdge = 0,
    HorizontalEdge,
    Cross,
    TJunction,
    Corner,
    Isolate,
}

public class TileShapeSetting
{
    public TileShapeFlags flags;
    public int    layer;
    public string tag        = "Untagged";
    public bool   canPreview = true;
    public Color  previewColor;
}

/// <summary>
/// 分類ごとにセル座標を保持
/// </summary>
public class ShapeCells
{
    public readonly List<Vector3Int> VerticalEdgesCells   = new();
    public readonly List<Vector3Int> HorizontalEdgesCells = new();
    public readonly List<Vector3Int> CrossCells           = new();
    public readonly List<Vector3Int> TJunctionCells       = new();
    public readonly List<Vector3Int> CornerCells          = new();
    public readonly List<Vector3Int> IsolateCells         = new();
}

public static class TileShapeClassifier
{
    /// <summary>
    /// Tilemap のタイル配置を解析し分類結果を返す
    /// </summary>
    public static ShapeCells Classify(Tilemap original, TileShapeSetting[] settings)
    {
        var result = new ShapeCells();

        //空セル分だけ cellBounds 縮小
        original.CompressBounds();

        //セル座標空間における境界ボックスと、その中の全タイルを取得(空白セル分は null)
        var cellBounds    = original.cellBounds;
        var tilesInBounds = original.GetTilesBlock(cellBounds);

        //タイルが存在するセルのみコレクションに格納
        int width  = cellBounds.size.x;
        int height = cellBounds.size.y;
        var occupiedCells = new HashSet<Vector3Int>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;
                if (tilesInBounds[index] == null) continue;

                //Bounds の最小座標をオフセットとしてワールド座標へ変換
                var cell = new Vector3Int(cellBounds.xMin + x, cellBounds.yMin + y, cellBounds.zMin);
                occupiedCells.Add(cell);
            }
        }

        //各セルの近傍判定
        foreach (var cell in occupiedCells)
        {
            ClassifyCellNeighbors(cell, occupiedCells, settings, result);
        }

        return result;
    }

    /// <summary>
    /// 指定セルの4近傍から分類を行う
    /// </summary>
    private static void ClassifyCellNeighbors(Vector3Int cell, HashSet<Vector3Int> cells,
        TileShapeSetting[] settings, ShapeCells result)
    {
        //隣接タイルの有無を調査
        bool up    = cells.Contains(cell + Vector3Int.up);
        bool down  = cells.Contains(cell + Vector3Int.down);
        bool left  = cells.Contains(cell + Vector3Int.left);
        bool right = cells.Contains(cell + Vector3Int.right);
        bool anyV  = up   || down;
        bool anyH  = left || right;
        int count  = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

        //分類ごとにリストへ追加する
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
    /// 設定に従って各リストへ追加
    /// </summary>
    private static void ApplyShapeFlags(Vector3Int cell, TileShapeFlags flags,
        List<Vector3Int> indepCellList, List<Vector3Int> vCellList, List<Vector3Int> hCellList)
    {
        if (flags.HasFlag(TileShapeFlags.VerticalEdge))   vCellList?.Add(cell);
        if (flags.HasFlag(TileShapeFlags.HorizontalEdge)) hCellList?.Add(cell);
        if (flags.HasFlag(TileShapeFlags.Independent))    indepCellList?.Add(cell);
    }
}
