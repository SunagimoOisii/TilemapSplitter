namespace TilemapSplitter
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// Draw classification results in SceneView with colors
    /// </summary>
    internal class TilemapPreviewDrawer
    {
        private Tilemap tilemap;
        private Dictionary<ShapeType_Rect, ShapeSetting> shapeSettings_Rect;
        private Dictionary<ShapeType_Hex, ShapeSetting>  shapeSettings_Hex;
        private ShapeCells_Rect shapeCells_Rect;
        private ShapeCells_Hex  shapeCells_Hex;
        private ICellDrawer     cellDrawer;

        /// <summary>
        /// Initialize for rectangular tiles
        /// </summary>
        public void Setup_Rect(Tilemap source, Dictionary<ShapeType_Rect, ShapeSetting> settings)
        {
            tilemap            = source;
            shapeSettings_Rect = settings;
            shapeSettings_Hex  = null;
            cellDrawer         = new RectCellDrawer(tilemap);
        }
        /// <summary>
        /// Initialize for hexagonal tiles
        /// </summary>
        public void Setup_Hex(Tilemap source, Dictionary<ShapeType_Hex, ShapeSetting> settings)
        {
            tilemap            = source;
            shapeSettings_Hex  = settings;
            shapeSettings_Rect = null;
            cellDrawer         = new HexCellDrawer(tilemap);
        }

        public void SetShapeCells(ShapeCells_Rect sc) => shapeCells_Rect = sc;
        public void SetShapeCells(ShapeCells_Hex sc) => shapeCells_Hex = sc;

        public void Register() =>   SceneView.duringSceneGui += OnSceneGUI;
        public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

        /// <summary>
        /// Draw classified cells
        /// </summary>
        private void OnSceneGUI(SceneView sv)
        {
            if (tilemap == null || tilemap.gameObject.activeInHierarchy == false) return;

            if (shapeSettings_Hex != null && shapeCells_Hex != null)
            {
                var full = shapeSettings_Hex[ShapeType_Hex.Full];
                var j5   = shapeSettings_Hex[ShapeType_Hex.Junction5];
                var j4   = shapeSettings_Hex[ShapeType_Hex.Junction4];
                var j3   = shapeSettings_Hex[ShapeType_Hex.Junction3];
                var edge = shapeSettings_Hex[ShapeType_Hex.Edge];
                var tip  = shapeSettings_Hex[ShapeType_Hex.Tip];
                var i    = shapeSettings_Hex[ShapeType_Hex.Isolate];

                var previewSettings = new (List<Vector3Int> cells, Color c, bool canPreview)[]
                {
                    (shapeCells_Hex.Full,      full.previewColor, full.canPreview),
                    (shapeCells_Hex.Junction5, j5.previewColor,   j5.canPreview),
                    (shapeCells_Hex.Junction4, j4.previewColor,   j4.canPreview),
                    (shapeCells_Hex.Junction3, j3.previewColor,   j3.canPreview),
                    (shapeCells_Hex.Edge,      edge.previewColor, edge.canPreview),
                    (shapeCells_Hex.Tip,       tip.previewColor,  tip.canPreview),
                    (shapeCells_Hex.Isolate,   i.previewColor,    i.canPreview)
                };
                foreach (var (cells, c, canPreview) in previewSettings)
                {
                    if (canPreview) DrawCellPreviews(cells, c);
                }
            }
            else if (shapeSettings_Rect != null && shapeCells_Rect != null)
            {
                var v       = shapeSettings_Rect[ShapeType_Rect.VerticalEdge];
                var h       = shapeSettings_Rect[ShapeType_Rect.HorizontalEdge];
                var cross   = shapeSettings_Rect[ShapeType_Rect.Cross];
                var t       = shapeSettings_Rect[ShapeType_Rect.TJunction];
                var corner  = shapeSettings_Rect[ShapeType_Rect.Corner];
                var isolate = shapeSettings_Rect[ShapeType_Rect.Isolate];

                var previewSettingsRect = new (List<Vector3Int> cells, Color c, bool canPreview)[]
                {
                    (shapeCells_Rect.Vertical,   v.previewColor,       v.canPreview),
                    (shapeCells_Rect.Horizontal, h.previewColor,       h.canPreview),
                    (shapeCells_Rect.Cross,      cross.previewColor,   cross.canPreview),
                    (shapeCells_Rect.TJunction,  t.previewColor ,      t.canPreview),
                    (shapeCells_Rect.Corner,     corner.previewColor,  corner.canPreview),
                    (shapeCells_Rect.Isolate,    isolate.previewColor, isolate.canPreview)
                };
                foreach (var (cells, c, canPreview) in previewSettingsRect)
                {
                    if (canPreview) DrawCellPreviews(cells, c);
                }
            }
        }

        /// <summary>
        /// Draw specified cells with polygons
        /// </summary>
        private void DrawCellPreviews(List<Vector3Int> cells, Color c)
        {
            if (cells.Count == 0 || cellDrawer == null) return;
            cellDrawer.Draw(cells, c);
        }

        /// <summary>
        /// Drawing strategy for each grid type
        /// </summary>
        private interface ICellDrawer
        {
            void Draw(List<Vector3Int> cells, Color c);
        }

        /// <summary>
        /// Draw rectangular grids
        /// </summary>
        private class RectCellDrawer : ICellDrawer
        {
            private readonly Tilemap tilemap;
            public RectCellDrawer(Tilemap tilemap) => this.tilemap = tilemap;

            public void Draw(List<Vector3Int> cells, Color c)
            {
                Handles.color = new Color(c.r, c.g, c.b, 0.4f);
                foreach (var cell in cells)
                {
                    var center = tilemap.GetCellCenterWorld(cell);
                    var right  = tilemap.GetCellCenterWorld(cell + Vector3Int.right) - center;
                    var up     = tilemap.GetCellCenterWorld(cell + Vector3Int.up)    - center;

                    var p0 = center - right * 0.5f - up * 0.5f;
                    var p1 = center + right * 0.5f - up * 0.5f;
                    var p2 = center + right * 0.5f + up * 0.5f;
                    var p3 = center - right * 0.5f + up * 0.5f;

                    //Draw by connecting the four corners of the cell
                    Handles.DrawAAConvexPolygon(p0, p1, p2, p3);
                }
            }
        }

        /// <summary>
        /// Draw hexagonal grids
        /// </summary>
        private class HexCellDrawer : ICellDrawer
        {
            private readonly Tilemap tilemap;
            private readonly bool    isPointTop;

            public HexCellDrawer(Tilemap tilemap)
            {
                this.tilemap = tilemap;
                var center = tilemap.GetCellCenterWorld(Vector3Int.zero);
                var right  = tilemap.GetCellCenterWorld(Vector3Int.right);
                isPointTop = Mathf.Approximately(center.y, right.y); //Determine if top is a point
            }

            public void Draw(List<Vector3Int> cells, Color c)
            {
                var size = tilemap.layoutGrid.cellSize;
                float halfW = size.x * 0.5f;
                float halfH = size.y * 0.5f;

                foreach (var cell in cells)
                {
                    Vector3 center  = tilemap.GetCellCenterWorld(cell);
                    Vector3[] verts = new Vector3[6];

                    float startDeg = isPointTop ? 30f : 0f;
                    for (int i = 0; i < 6; i++)
                    {
                        float angleDeg = 60f * i + startDeg;
                        float rad = Mathf.Deg2Rad * angleDeg;
                        //Calculate each vertex position of the hexagon
                        verts[i] = new Vector3(center.x + halfW * Mathf.Cos(rad),
                            center.y + halfH * Mathf.Sin(rad),
                            center.z);
                    }
                    Handles.color = new Color(c.r, c.g, c.b, 0.4f);
                    Handles.DrawAAConvexPolygon(verts);
                }
            }
        }
    }
}
