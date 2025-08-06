namespace TilemapSplitter
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    /// <summary>
    /// Layout strategy for hexagonal grids
    /// </summary>
    internal class CellLayoutStrategy_Hex : ICellLayoutStrategy
    {
        private readonly Dictionary<ShapeType_Hex, ShapeSetting> settingsDict;
        private readonly Action refreshPreview;
        private ShapeCells_Hex shapeCells = new();

        private Foldout fullFoldOut;
        private Foldout junction5FoldOut;
        private Foldout junction4FoldOut;
        private Foldout junction3FoldOut;
        private Foldout edgeFoldOut;
        private Foldout tipFoldOut;
        private Foldout hexIsolateFoldOut;

        public CellLayoutStrategy_Hex(Dictionary<ShapeType_Hex, ShapeSetting> settings, Action refreshPreview)
        {
            settingsDict        = settings;
            this.refreshPreview = refreshPreview;
        }

        public void CreateMergeEdgeToggle(VisualElement container, Func<bool> getter, Action<bool> setter)
        {
        }

        public void CreateShapeFoldouts(VisualElement container)
        {
            var infos = new (ShapeType_Hex type, string title)[]
            {
                (ShapeType_Hex.Full,      "Full"),
                (ShapeType_Hex.Junction5, "Junction5"),
                (ShapeType_Hex.Junction4, "Junction4"),
                (ShapeType_Hex.Junction3, "Junction3"),
                (ShapeType_Hex.Edge,      "Edge"),
                (ShapeType_Hex.Tip,       "Tip"),
                (ShapeType_Hex.Isolate,   "Isolate")
            };
            foreach (var info in infos)
            {
                var fold = CreateFoldout(container, info.type, info.title);
                switch (info.type)
                {
                    case ShapeType_Hex.Full:      fullFoldOut       = fold; break;
                    case ShapeType_Hex.Junction5: junction5FoldOut  = fold; break;
                    case ShapeType_Hex.Junction4: junction4FoldOut  = fold; break;
                    case ShapeType_Hex.Junction3: junction3FoldOut  = fold; break;
                    case ShapeType_Hex.Edge:      edgeFoldOut       = fold; break;
                    case ShapeType_Hex.Tip:       tipFoldOut        = fold; break;
                    case ShapeType_Hex.Isolate:   hexIsolateFoldOut = fold; break;
                }
                TilemapSplitterWindow.AddHorizontalSeparator(container);
            }
        }

        private Foldout CreateFoldout(VisualElement parentContainer, ShapeType_Hex type, string title)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = settingsDict[type];
            AddShapeSettingControls(fold, setting);

            parentContainer.Add(fold);
            return fold;
        }

        private void AddShapeSettingControls(Foldout fold, ShapeSetting setting)
        {
            var layerF   = new LayerField("Layer", setting.layer);
            var tagF     = new TagField("Tag", setting.tag);
            var previewT = new Toggle("Preview") { value = setting.canPreview };
            var colF     = new ColorField("Preview Color") { value = setting.previewColor };
            layerF.RegisterValueChangedCallback(evt => setting.layer = evt.newValue);
            tagF.RegisterValueChangedCallback(evt => setting.tag = evt.newValue);
            previewT.RegisterValueChangedCallback(evt =>
            {
                setting.canPreview = evt.newValue;
                refreshPreview();
            });
            colF.RegisterValueChangedCallback(evt => setting.previewColor = evt.newValue);

            fold.Add(layerF);
            fold.Add(tagF);
            fold.Add(previewT);
            fold.Add(colF);
        }

        public IEnumerator<bool> Classify(Tilemap source, IProgress<float> progress = null,
            Func<bool> isCancelled = null)
        {
            shapeCells = new ShapeCells_Hex();
            return TileShapeClassifier.ClassifyCoroutine(source, settingsDict, shapeCells, 100, progress, isCancelled);
        }

        public void GenerateSplitTilemaps(Tilemap source, bool canMergeEdges, bool canAttachCollider)
        {
            TilemapCreator.GenerateSplitTilemaps(source, shapeCells, settingsDict, canAttachCollider);
        }

        public void SetupPreview(Tilemap source, TilemapPreviewDrawer drawer)
        {
            drawer.Setup(source, settingsDict);
        }

        public void SetShapeCellsToPreview(TilemapPreviewDrawer drawer)
        {
            drawer.SetShapeCells(shapeCells);
        }

        public void UpdateFoldoutTitles()
        {
            var list = new (Foldout f, string name, int count)[]
            {
                (fullFoldOut,      "Full",      shapeCells.Full.Count),
                (junction5FoldOut, "Junction5", shapeCells.Junction5.Count),
                (junction4FoldOut, "Junction4", shapeCells.Junction4.Count),
                (junction3FoldOut, "Junction3", shapeCells.Junction3.Count),
                (edgeFoldOut,      "Edge",      shapeCells.Edge.Count),
                (tipFoldOut,       "Tip",       shapeCells.Tip.Count),
                (hexIsolateFoldOut,"Isolate",   shapeCells.Isolate.Count)
            };
            foreach (var (f, name, count) in list)
            {
                f.text = $"{name} (Count:{count})";
            }
        }
    }
}
