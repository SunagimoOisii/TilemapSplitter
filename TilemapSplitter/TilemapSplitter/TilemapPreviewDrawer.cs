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

        var list = new (List<Vector3Int> positions, Color c)[]
        {
            (result.VerticalEdges,   settings[(int)SettingType.VerticalEdge].color),
            (result.HorizontalEdges, settings[(int)SettingType.HorizontalEdge].color),
            (result.CrossTiles,      settings[(int)SettingType.Cross].color),
            (result.TJunctionTiles,  settings[(int)SettingType.TJunction].color),
            (result.CornerTiles,     settings[(int)SettingType.Corner].color),
            (result.IsolateTiles,    settings[(int)SettingType.Isolate].color)
        };
        foreach (var (positions, c) in list) DrawList(positions, c);
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
