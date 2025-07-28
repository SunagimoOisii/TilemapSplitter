namespace TilemapSplitter
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// Draws color-coded previews of classified tiles on the SceneView
    /// </summary>
    internal class TilemapPreviewDrawer
    {
        private Tilemap tilemap;
        private Dictionary<ShapeType, ShapeSetting> shapeSettings;
        private Dictionary<HexShapeType, ShapeSetting> hexShapeSettings;
        private ShapeCells shapeCells;
        private HexShapeCells hexShapeCells;

        public void Setup(Tilemap source, Dictionary<ShapeType, ShapeSetting> settings)
        {
            tilemap         = source;
            shapeSettings   = settings;
            hexShapeSettings = null;
        }

        public void Setup(Tilemap source, Dictionary<HexShapeType, ShapeSetting> settings)
        {
            tilemap         = source;
            hexShapeSettings = settings;
            shapeSettings    = null;
        }

        public void SetShapeCells(ShapeCells sc) => shapeCells = sc;
        public void SetShapeCells(HexShapeCells sc) => hexShapeCells = sc;

        public void Register() =>   SceneView.duringSceneGui += OnSceneGUI;
        public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnSceneGUI(SceneView sv)
        {
            if (tilemap == null) return;

            if (hexShapeSettings != null && hexShapeCells != null)
            {
                var full     = hexShapeSettings[HexShapeType.Full];
                var junction = hexShapeSettings[HexShapeType.Junction];
                var co       = hexShapeSettings[HexShapeType.Corner];
                var edge     = hexShapeSettings[HexShapeType.Edge];
                var tip      = hexShapeSettings[HexShapeType.Tip];
                var i        = hexShapeSettings[HexShapeType.Isolate];

                var previewSettings = new (List<Vector3Int> cells, Color c, bool canPreview)[]
                {
                    (hexShapeCells.FullCells,     full.previewColor,     full.canPreview),
                    (hexShapeCells.JunctionCells, junction.previewColor, junction.canPreview),
                    (hexShapeCells.CornerCells,   co.previewColor,   co.canPreview),
                    (hexShapeCells.EdgeCells,     edge.previewColor,     edge.canPreview),
                    (hexShapeCells.TipCells,      tip.previewColor,      tip.canPreview),
                    (hexShapeCells.IsolateCells,  i.previewColor,  i.canPreview)
                };
                foreach (var (cells, c, canPreview) in previewSettings)
                {
                    if (canPreview) DrawCellPreviews(cells, c);
                }
                return;
            }

            if (shapeSettings == null || shapeCells == null) return;

            var v       = shapeSettings[ShapeType.VerticalEdge];
            var h       = shapeSettings[ShapeType.HorizontalEdge];
            var cross   = shapeSettings[ShapeType.Cross];
            var t       = shapeSettings[ShapeType.TJunction];
            var corner  = shapeSettings[ShapeType.Corner];
            var isolate = shapeSettings[ShapeType.Isolate];

            var previewSettingsRect = new (List<Vector3Int> cells, Color c, bool canPreview)[]
            {
                (shapeCells.VerticalCells,   v.previewColor,       v.canPreview),
                (shapeCells.HorizontalCells, h.previewColor,       h.canPreview),
                (shapeCells.CrossCells,      cross.previewColor,   cross.canPreview),
                (shapeCells.TJunctionCells,  t.previewColor ,      t.canPreview),
                (shapeCells.CornerCells,     corner.previewColor,  corner.canPreview),
                (shapeCells.IsolateCells,    isolate.previewColor, isolate.canPreview)
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
                DrawHex(cells);
            }
            else
            {
                DrawRect(cells);
            }

            void DrawRect(List<Vector3Int> list)
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
            void DrawHex(List<Vector3Int> cells)
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
