using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// タイル分割結果を Scene 上でプレビュー表示するクラス
/// </summary>
public class TilemapPreviewDrawer
{
    private Tilemap tilemap;
    private TileShapeSetting[] shapeSettings;
    private ShapeCells shapeCells;

    public void Setup(Tilemap original, TileShapeSetting[] settings)
    {
        tilemap       = original;
        shapeSettings = settings;
    }

    public void SetShapeResult(ShapeCells sc) => shapeCells = sc;

    public void Register()   => SceneView.duringSceneGui += OnSceneGUI;
    public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        if (tilemap == null || 
            shapeCells == null) return;

        //各設定の取得
        var v       = shapeSettings[(int)TileShapeType.VerticalEdge];
        var h       = shapeSettings[(int)TileShapeType.HorizontalEdge];
        var cross   = shapeSettings[(int)TileShapeType.Cross];
        var t       = shapeSettings[(int)TileShapeType.TJunction];
        var corner  = shapeSettings[(int)TileShapeType.Corner];
        var isolate = shapeSettings[(int)TileShapeType.Isolate];

        //設定色, プレビュー許可に応じて表示
        var previewSettings = new (List<Vector3Int> cells, Color c, bool canPreview)[]
        {
            (shapeCells.VerticalEdgesCells,   v.previewColor,       v.canPreview),
            (shapeCells.HorizontalEdgesCells, h.previewColor,       h.canPreview),
            (shapeCells.CrossCells,           cross.previewColor,   cross.canPreview),
            (shapeCells.TJunctionCells,       t.previewColor ,      t.canPreview),
            (shapeCells.CornerCells,          corner.previewColor,  corner.canPreview),
            (shapeCells.IsolateCells,         isolate.previewColor, isolate.canPreview)
        };
        foreach (var (cells, c, canPreview) in previewSettings)
        {
            if(canPreview) DrawCellPreviews(cells, c);
        }
    }

    private void DrawCellPreviews(List<Vector3Int> cells, Color c)
    {
        Handles.color = new Color(c.r, c.g, c.b, 0.4f);
        var cellSize  = tilemap.cellSize;
        foreach (var cell in cells)
        {
            var worldPos = tilemap.CellToWorld(cell) + new Vector3(cellSize.x / 2f, cellSize.y / 2f);
            var rect = new Rect(
                worldPos.x - cellSize.x / 2f, worldPos.y - cellSize.y / 2f,
                cellSize.x, cellSize.y);
            Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
        }
    }
}
