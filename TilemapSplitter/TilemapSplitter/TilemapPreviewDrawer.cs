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

            Handles.color = new Color(c.r, c.g, c.b, 0.4f);
            var cellSize = Vector3.Scale(tilemap.layoutGrid.cellSize, tilemap.transform.lossyScale);
            foreach (var cell in cells)
            {
                var worldPos = tilemap.CellToWorld(cell) +
                    Vector3.Scale(tilemap.tileAnchor, tilemap.transform.lossyScale);
                var rect = new Rect(
                    worldPos.x - cellSize.x / 2f,
                    worldPos.y - cellSize.y / 2f,
                    cellSize.x,
                    cellSize.y);
                Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
            }
        }
    }
}
