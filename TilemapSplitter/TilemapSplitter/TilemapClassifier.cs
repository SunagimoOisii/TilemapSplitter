using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapClassificationResult
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
    public static TilemapClassificationResult Classify(Tilemap tilemap)
    {
        var result = new TilemapClassificationResult();
        if (tilemap == null) return result;

        var positions = new List<Vector3Int>();
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(pos) != null) positions.Add(pos);
        }

        var set = new HashSet<Vector3Int>(positions);
        foreach (var pos in positions)
        {
            ClassifyNeighbors(pos, set, result);
        }

        return result;
    }

    private static void ClassifyNeighbors(Vector3Int pos, HashSet<Vector3Int> tiles, TilemapClassificationResult r)
    {
        bool up    = tiles.Contains(pos + Vector3Int.up);
        bool down  = tiles.Contains(pos + Vector3Int.down);
        bool left  = tiles.Contains(pos + Vector3Int.left);
        bool right = tiles.Contains(pos + Vector3Int.right);
        bool anyV  = up   || down;
        bool anyH  = left || right;
        int count  = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

        if (count == 4)
        {
            r.CrossTiles.Add(pos);
        }
        else if (count == 3)
        {
            r.TJunctionTiles.Add(pos);
        }
        else if (count == 2 && anyV && anyH)
        {
            r.CornerTiles.Add(pos);
        }
        else if (anyV && !anyH)
        {
            r.VerticalEdges.Add(pos);
        }
        else if (anyH && !anyV)
        {
            r.HorizontalEdges.Add(pos);
        }
        else if (count == 0)
        {
            r.IsolateTiles.Add(pos);
        }
    }
}
