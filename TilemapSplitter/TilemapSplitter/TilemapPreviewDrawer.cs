using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapPreviewDrawer
{
    private Tilemap tilemap;
    private ClassificationSetting[] settings;
    private ClassificationResult result;

    public void Initialize(Tilemap tilemap, ClassificationSetting[] settings)
    {
        this.tilemap  = tilemap;
        this.settings = settings;
    }

    public void SetResult(ClassificationResult result) => this.result = result;

    public void Register()  => SceneView.duringSceneGui += OnSceneGUI;
    public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        if (tilemap == null || result == null) return;

        if (settings[(int)SettingType.VerticalEdge].canPreview)
            DrawList(result.VerticalEdges, settings[(int)SettingType.VerticalEdge].color);
        if (settings[(int)SettingType.HorizontalEdge].canPreview)
            DrawList(result.HorizontalEdges, settings[(int)SettingType.HorizontalEdge].color);
        if (settings[(int)SettingType.Cross].canPreview)
            DrawList(result.CrossTiles, settings[(int)SettingType.Cross].color);
        if (settings[(int)SettingType.TJunction].canPreview)
            DrawList(result.TJunctionTiles, settings[(int)SettingType.TJunction].color);
        if (settings[(int)SettingType.Corner].canPreview)
            DrawList(result.CornerTiles, settings[(int)SettingType.Corner].color);
        if (settings[(int)SettingType.Isolate].canPreview)
            DrawList(result.IsolateTiles, settings[(int)SettingType.Isolate].color);
    }

    private void DrawList(System.Collections.Generic.List<Vector3Int> list, Color col)
    {
        Handles.color = new Color(col.r, col.g, col.b, 0.4f);
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
