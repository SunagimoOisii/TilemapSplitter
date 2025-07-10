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
    private TileShapeSetting[] settings;
    private TileShapeResult result;

    /// <summary>
    /// 分割対象と設定を登録
    /// </summary>
    public void Setup(Tilemap tm, TileShapeSetting[] settings)
    {
        tilemap = tm;
        this.settings = settings;
    }

    public void SetShapeResult(TileShapeResult result) => this.result = result;

    public void Register()   => SceneView.duringSceneGui += OnSceneGUI;
    public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        if (tilemap == null || 
            result == null) return;

        //各設定の取得
        var v       = settings[(int)TileShapeType.VerticalEdge];
        var h       = settings[(int)TileShapeType.HorizontalEdge];
        var cross   = settings[(int)TileShapeType.Cross];
        var t       = settings[(int)TileShapeType.TJunction];
        var corner  = settings[(int)TileShapeType.Corner];
        var isolate = settings[(int)TileShapeType.Isolate];

        //設定色, プレビュー許可に応じて表示
        var previewSettings = new (List<Vector3Int> cells, Color c, bool canPreview)[]
        {
            (result.VerticalEdges,   v.previewColor,       v.canPreview),
            (result.HorizontalEdges, h.previewColor,       h.canPreview),
            (result.CrossTiles,      cross.previewColor,   cross.canPreview),
            (result.TJunctionTiles,  t.previewColor ,      t.canPreview),
            (result.CornerTiles,     corner.previewColor,  corner.canPreview),
            (result.IsolateTiles,    isolate.previewColor, isolate.canPreview)
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
        foreach (var pos in cells)
        {
            var worldPos = tilemap.CellToWorld(pos) + new Vector3(cellSize.x / 2f, cellSize.y / 2f);
            var rect = new Rect(
                worldPos.x - cellSize.x / 2f, worldPos.y - cellSize.y / 2f,
                cellSize.x, cellSize.y);
            Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
        }
    }
}
