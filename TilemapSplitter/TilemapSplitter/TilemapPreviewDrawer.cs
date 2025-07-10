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
    private ClassificationSetting[] settings;
    private ClassificationResult result;

    /// <summary>
    /// 分割対象と設定を登録
    /// </summary>
    public void Initialize(Tilemap tm, ClassificationSetting[] settings)
    {
        tilemap = tm;
        this.settings = settings;
    }

    public void SetResult(ClassificationResult result) => this.result = result;

    public void Register()   => SceneView.duringSceneGui += OnSceneGUI;
    public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        if (tilemap == null || 
            result == null) return;

        //各設定の取得
        var v       = settings[(int)SettingType.VerticalEdge];
        var h       = settings[(int)SettingType.HorizontalEdge];
        var cross   = settings[(int)SettingType.Cross];
        var t       = settings[(int)SettingType.TJunction];
        var corner  = settings[(int)SettingType.Corner];
        var isolate = settings[(int)SettingType.Isolate];

        //設定色, プレビュー許可に応じて表示
        var previewSettings = new (List<Vector3Int> positions, Color c, bool canPreview)[]
        {
            (result.VerticalEdges,   v.color,       v.canPreview),
            (result.HorizontalEdges, h.color,       h.canPreview),
            (result.CrossTiles,      cross.color,   cross.canPreview),
            (result.TJunctionTiles,  t.color ,      t.canPreview),
            (result.CornerTiles,     corner.color,  corner.canPreview),
            (result.IsolateTiles,    isolate.color, isolate.canPreview)
        };
        foreach (var (positions, c, canPreview) in previewSettings)
        {
            if(canPreview) DrawList(positions, c);
        }
    }

    /// <summary>
    /// 各セルを指定色で描画
    /// </summary>
    private void DrawList(List<Vector3Int> positions, Color c)
    {
        Handles.color = new Color(c.r, c.g, c.b, 0.4f);
        var cellSize  = tilemap.cellSize;
        foreach (var pos in positions)
        {
            var worldPos = tilemap.CellToWorld(pos) + new Vector3(cellSize.x / 2f, cellSize.y / 2f);
            var rect = new Rect(
                worldPos.x - cellSize.x / 2f, worldPos.y - cellSize.y / 2f,
                cellSize.x, cellSize.y);
            Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
        }
    }
}
