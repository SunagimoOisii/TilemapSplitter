namespace TilemapSplitter
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// Draws color-coded previews of classified tiles on the SceneView
    /// </summary>
    internal class TilemapPreviewDrawer
    {
        private Tilemap tilemap;
        private Dictionary<ShapeType_Rect, ShapeSetting> shapeSettings_Rect;
        private Dictionary<ShapeType_Hex, ShapeSetting> shapeSettings_Hex;
        private ShapeCells_Rect shapeCells;
        private ShapeCells_Hex hexShapeCells;

        public void Setup_Rect(Tilemap source, Dictionary<ShapeType_Rect, ShapeSetting> settings)
        {
            tilemap            = source;
            shapeSettings_Rect = settings;
            shapeSettings_Hex  = null;
        }

        public void Setup_Hex(Tilemap source, Dictionary<ShapeType_Hex, ShapeSetting> settings)
        {
            tilemap            = source;
            shapeSettings_Hex  = settings;
            shapeSettings_Rect = null;
        }

        public void SetShapeCells(ShapeCells_Rect sc) => shapeCells = sc;
        public void SetShapeCells(ShapeCells_Hex sc) => hexShapeCells = sc;

        public void Register() =>   SceneView.duringSceneGui += OnSceneGUI;
        public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnSceneGUI(SceneView sv)
        {
            if (tilemap == null) return;

            if (shapeSettings_Hex != null && hexShapeCells != null)
            {
                var full     = shapeSettings_Hex[ShapeType_Hex.Full];
                var junction = shapeSettings_Hex[ShapeType_Hex.Junction];
                var co       = shapeSettings_Hex[ShapeType_Hex.Corner];
                var edge     = shapeSettings_Hex[ShapeType_Hex.Edge];
                var tip      = shapeSettings_Hex[ShapeType_Hex.Tip];
                var i        = shapeSettings_Hex[ShapeType_Hex.Isolate];

                var previewSettings = new (List<Vector3Int> cells, Color c, bool canPreview)[]
                {
                    (hexShapeCells.Full,     full.previewColor,     full.canPreview),
                    (hexShapeCells.Junction, junction.previewColor, junction.canPreview),
                    (hexShapeCells.Corner,   co.previewColor,   co.canPreview),
                    (hexShapeCells.Edge,     edge.previewColor,     edge.canPreview),
                    (hexShapeCells.Tip,      tip.previewColor,      tip.canPreview),
                    (hexShapeCells.Isolate,  i.previewColor,  i.canPreview)
                };
                foreach (var (cells, c, canPreview) in previewSettings)
                {
                    if (canPreview) DrawCellPreviews(cells, c);
                }
                return;
            }

            if (shapeSettings_Rect == null || shapeCells == null) return;

            var v       = shapeSettings_Rect[ShapeType_Rect.VerticalEdge];
            var h       = shapeSettings_Rect[ShapeType_Rect.HorizontalEdge];
            var cross   = shapeSettings_Rect[ShapeType_Rect.Cross];
            var t       = shapeSettings_Rect[ShapeType_Rect.TJunction];
            var corner  = shapeSettings_Rect[ShapeType_Rect.Corner];
            var isolate = shapeSettings_Rect[ShapeType_Rect.Isolate];

            var previewSettingsRect = new (List<Vector3Int> cells, Color c, bool canPreview)[]
            {
                (shapeCells.Vertical,   v.previewColor,       v.canPreview),
                (shapeCells.Horizontal, h.previewColor,       h.canPreview),
                (shapeCells.Cross,      cross.previewColor,   cross.canPreview),
                (shapeCells.TJunction,  t.previewColor ,      t.canPreview),
                (shapeCells.Corner,     corner.previewColor,  corner.canPreview),
                (shapeCells.Isolate,    isolate.previewColor, isolate.canPreview)
            };
            foreach (var (cells, c, canPreview) in previewSettingsRect)
            {
                if (canPreview) DrawCellPreviews(cells, c);
            }
        }

        private void DrawCellPreviews(List<Vector3Int> cells, Color c)
        {
            if (cells == null || cells.Count == 0) return;

            Handles.color = new Color(c.r, c.g, c.b, 0.4f);
            var layout = tilemap.layoutGrid.cellLayout;
            if (layout is GridLayout.CellLayout.Hexagon)
            {
                Draw_Hex(cells);
            }
            else
            {
                Draw_Rect(cells);
            }

            void Draw_Rect(List<Vector3Int> list)
            {
                foreach (var cell in list)
                {
                    var center = tilemap.GetCellCenterWorld(cell);
                    var right  = tilemap.GetCellCenterWorld(cell + Vector3Int.right) - center;
                    var up     = tilemap.GetCellCenterWorld(cell + Vector3Int.up)    - center;

                    var p0 = center - right * 0.5f - up * 0.5f;
                    var p1 = center + right * 0.5f - up * 0.5f;
                    var p2 = center + right * 0.5f + up * 0.5f;
                    var p3 = center - right * 0.5f + up * 0.5f;

                    Handles.DrawAAConvexPolygon(p0, p1, p2, p3);
                }
            }

            /// <summary>
            /// Calculates hexagon corners directly from Grid.cellSize:
            /// - Replaces previous midpoint-based sampling which could yield duplicate or missing vertices, causing malformed shapes
            /// - Reduces GetCellCenterWorld calls for better performance
            /// - Prevents distortion if an adjacent cell is missing
            /// </summary>
            void Draw_Hex(List<Vector3Int> cells)
            {
                var layout = tilemap.layoutGrid.cellLayout;
                if (layout != GridLayout.CellLayout.Hexagon) return;

                var size = tilemap.layoutGrid.cellSize;
                float halfW = size.x * 0.5f;
                float halfH = size.y * 0.5f;

                foreach (var cell in cells)
                {
                    Vector3 center  = tilemap.GetCellCenterWorld(cell);
                    Vector3[] verts = new Vector3[6];

                    for (int i = 0; i < 6; i++)
                    {
                        float angleDeg = 60f * i + 30f;
                        float rad = Mathf.Deg2Rad * angleDeg;
                        verts[i] = new Vector3(
                            center.x + halfW * Mathf.Cos(rad),
                            center.y + halfH * Mathf.Sin(rad),
                            center.z
                        );
                    }
                    Handles.color = new Color(c.r, c.g, c.b, 0.4f);
                    Handles.DrawAAConvexPolygon(verts);
                }
            }
        }
    }
}
