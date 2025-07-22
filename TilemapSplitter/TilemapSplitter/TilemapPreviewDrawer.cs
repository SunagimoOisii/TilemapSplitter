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
        private ShapeCells shapeCells;

        public void Setup(Tilemap source, Dictionary<ShapeType, ShapeSetting> settings)
        {
            tilemap       = source;
            shapeSettings = settings;
        }

        public void SetShapeCells(ShapeCells sc) => shapeCells = sc;

        public void Register() =>   SceneView.duringSceneGui += OnSceneGUI;
        public void Unregister() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnSceneGUI(SceneView sv)
        {
            if (tilemap == null || shapeCells == null) return;

            //Read preview settings(color, visibility) for each tile classification
            var v       = shapeSettings[ShapeType.VerticalEdge];
            var h       = shapeSettings[ShapeType.HorizontalEdge];
            var cross   = shapeSettings[ShapeType.Cross];
            var t       = shapeSettings[ShapeType.TJunction];
            var corner  = shapeSettings[ShapeType.Corner];
            var isolate = shapeSettings[ShapeType.Isolate];

            //Draw each cell only if its preview flag is enabled, using the specified preview color
            var previewSettings = new (List<Vector3Int> cells, Color c, bool canPreview)[]
            {
                (shapeCells.VerticalCells,   v.previewColor,       v.canPreview),
                (shapeCells.HorizontalCells, h.previewColor,       h.canPreview),
                (shapeCells.CrossCells,      cross.previewColor,   cross.canPreview),
                (shapeCells.TJunctionCells,  t.previewColor ,      t.canPreview),
                (shapeCells.CornerCells,     corner.previewColor,  corner.canPreview),
                (shapeCells.IsolateCells,    isolate.previewColor, isolate.canPreview)
            };
            foreach (var (cells, c, canPreview) in previewSettings)
            {
                if (canPreview) DrawCellPreviews(cells, c);
            }
        }

        private void DrawCellPreviews(List<Vector3Int> cells, Color c)
        {
            if (cells == null || cells.Count == 0) return;

            //Convert cell size of Grid component to world coordinates
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

                Handles.DrawAAConvexPolygon(p0, p1, p2, p3);
            }
        }
    }
}
