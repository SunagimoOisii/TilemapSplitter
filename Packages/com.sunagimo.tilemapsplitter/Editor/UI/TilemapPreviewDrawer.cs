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
        private Dictionary<ShapeType_Rect, List<Vector3Int>> cellsDict_Rect;
        private Dictionary<ShapeType_Hex, List<Vector3Int>>  cellsDict_Hex;
        private ICellDrawer     cellDrawer;

        public void Setup(Tilemap source, Dictionary<ShapeType_Rect, ShapeSetting> settings)
        {
            tilemap            = source;
            shapeSettings_Rect = settings;
            shapeSettings_Hex  = null;
            cellDrawer         = new CellDrawer_Rect(tilemap);
        }
        public void Setup(Tilemap source, Dictionary<ShapeType_Hex, ShapeSetting> settings)
        {
            tilemap            = source;
            shapeSettings_Hex  = settings;
            shapeSettings_Rect = null;
            cellDrawer         = new CellDrawer_Hex(tilemap);
        }

        public void SetShapeCells(ShapeCells_Rect sc)
        {
            cellsDict_Rect = new Dictionary<ShapeType_Rect, List<Vector3Int>>
            {
                [ShapeType_Rect.VerticalEdge]   = sc.Vertical,
                [ShapeType_Rect.HorizontalEdge] = sc.Horizontal,
                [ShapeType_Rect.Cross]          = sc.Cross,
                [ShapeType_Rect.TJunction]      = sc.TJunction,
                [ShapeType_Rect.Corner]         = sc.Corner,
                [ShapeType_Rect.Isolate]        = sc.Isolate
            };
        }
        public void SetShapeCells(ShapeCells_Hex sc)
        {
            cellsDict_Hex = new Dictionary<ShapeType_Hex, List<Vector3Int>>
            {
                [ShapeType_Hex.Full]      = sc.Full,
                [ShapeType_Hex.Junction5] = sc.Junction5,
                [ShapeType_Hex.Junction4] = sc.Junction4,
                [ShapeType_Hex.Junction3] = sc.Junction3,
                [ShapeType_Hex.Edge]      = sc.Edge,
                [ShapeType_Hex.Tip]       = sc.Tip,
                [ShapeType_Hex.Isolate]   = sc.Isolate
            };
        }

        public void Register()   => SceneView.duringSceneGui += OnSceneGUI;
        public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

        /// <summary>
        /// Draw classified cells
        /// </summary>
        private void OnSceneGUI(SceneView sv)
        {
            if (tilemap == null || tilemap.gameObject.activeInHierarchy == false) return;

            if (shapeSettings_Hex != null && cellsDict_Hex != null)
            {
                foreach (var (cells, c, canPreview) in GetPreviewSettings(shapeSettings_Hex, cellsDict_Hex))
                {
                    if (canPreview) DrawCellPreviews(cells, c);
                }
            }
            else if (shapeSettings_Rect != null && cellsDict_Rect != null)
            {
                foreach (var (cells, c, canPreview) in GetPreviewSettings(shapeSettings_Rect, cellsDict_Rect))
                {
                    if (canPreview) DrawCellPreviews(cells, c);
                }
            }
        }

        private static (List<Vector3Int> cells, Color c, bool canPreview)[] GetPreviewSettings<T>(
            Dictionary<T, ShapeSetting> shapeSettings, Dictionary<T, List<Vector3Int>> shapeCells) where T : System.Enum
        {
            var result = new (List<Vector3Int> cells, Color c, bool canPreview)[shapeCells.Count];
            var idx    = 0;
            foreach (var kv in shapeCells)
            {
                var setting = shapeSettings[kv.Key];
                result[idx++] = (kv.Value, setting.previewColor, setting.canPreview);
            }
            return result;
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

        private class CellDrawer_Rect : ICellDrawer
        {
            private readonly Tilemap tilemap;
            public CellDrawer_Rect(Tilemap tilemap) => this.tilemap = tilemap;

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

        private class CellDrawer_Hex : ICellDrawer
        {
            private readonly Tilemap tilemap;
            private readonly bool    isPointTop;

            public CellDrawer_Hex(Tilemap tilemap)
            {
                this.tilemap = tilemap;
                var center   = tilemap.GetCellCenterWorld(Vector3Int.zero);
                var right    = tilemap.GetCellCenterWorld(Vector3Int.right);
                isPointTop   = Mathf.Approximately(center.y, right.y); //Determine if top is a point
            }

            public void Draw(List<Vector3Int> cells, Color c)
            {
                var size  = tilemap.layoutGrid.cellSize;
                var halfW = size.x * 0.5f;
                var halfH = size.y * 0.5f;

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
