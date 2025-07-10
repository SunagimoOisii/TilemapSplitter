using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapPreviewDrawer
{
    public static void DrawPreviewList(Tilemap tilemap, List<Vector3Int> list, Color color)
    {
        if (tilemap == null || list == null || list.Count == 0) return;

        Handles.color = new Color(color.r, color.g, color.b, 0.4f);
        var cellSize = tilemap.cellSize;
        foreach (var pos in list)
        {
            Vector3 worldPos = tilemap.CellToWorld(pos) + new Vector3(cellSize.x / 2f, cellSize.y / 2f);
            Rect rect = new(
                worldPos.x - cellSize.x / 2f,
                worldPos.y - cellSize.y / 2f,
                cellSize.x,
                cellSize.y);
            Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
        }
    }
}
