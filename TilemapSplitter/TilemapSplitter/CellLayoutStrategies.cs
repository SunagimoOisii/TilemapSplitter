namespace TilemapSplitter
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    /// <summary>
    /// Strategy interface that provides processing based on cell layout.
    /// </summary>
    internal interface ICellLayoutStrategy
    {
        void CreateMergeEdgeToggle(VisualElement container, Func<bool> getter, Action<bool> setter);
        void CreateShapeFoldouts(VisualElement container);
        IEnumerator<bool> Classify(Tilemap source);
        void GenerateSplitTilemaps(Tilemap source, bool canMergeEdges, bool canAttachCollider);
        void SetupPreview(Tilemap source, TilemapPreviewDrawer drawer);
        void SetShapeCellsToPreview(TilemapPreviewDrawer drawer);
        void UpdateFoldoutTitles();
    }

    /// <summary>
    /// Layout strategy for rectangular grids.
    /// </summary>
    internal class RectLayoutStrategy : ICellLayoutStrategy
    {
        private readonly Dictionary<ShapeType_Rect, ShapeSetting> settingsDict;
        private readonly Action refreshPreview;
        private ShapeCells_Rect shapeCells = new();

        private Foldout verticalEdgeFoldOut;
        private Foldout horizontalEdgeFoldOut;
        private Foldout crossFoldOut;
        private Foldout tJunctionFoldOut;
        private Foldout cornerFoldOut;
        private Foldout isolateFoldOut;

        public RectLayoutStrategy(Dictionary<ShapeType_Rect, ShapeSetting> settings, Action refreshPreview)
        {
            settingsDict        = settings;
            this.refreshPreview = refreshPreview;
        }

        public void CreateMergeEdgeToggle(VisualElement container, Func<bool> getter, Action<bool> setter)
        {
            var mergeT  = new Toggle("Merge VerticalEdge, HorizontalEdge") { value = getter() };
            var mergeHB = new HelpBox("When merging, VerticalEdge shapeSettings_Rect take precedence",
                HelpBoxMessageType.Info);
            mergeT.RegisterValueChangedCallback(evt => setter(evt.newValue));
            container.Add(mergeT);
            container.Add(mergeHB);
        }

        public void CreateShapeFoldouts(VisualElement container)
        {
            var infos = new (ShapeType_Rect type, string title)[]
            {
                (ShapeType_Rect.VerticalEdge,   "VerticalEdge"),
                (ShapeType_Rect.HorizontalEdge, "HorizontalEdge"),
                (ShapeType_Rect.Cross,          "Cross"),
                (ShapeType_Rect.TJunction,      "T-Junction"),
                (ShapeType_Rect.Corner,         "Corner"),
                (ShapeType_Rect.Isolate,        "Isolate")
            };
            foreach (var info in infos)
            {
                var fold = CreateFoldout(container, info.type, info.title);
                switch (info.type)
                {
                    case ShapeType_Rect.VerticalEdge:   verticalEdgeFoldOut   = fold; break;
                    case ShapeType_Rect.HorizontalEdge: horizontalEdgeFoldOut = fold; break;
                    case ShapeType_Rect.Cross:          crossFoldOut          = fold; break;
                    case ShapeType_Rect.TJunction:      tJunctionFoldOut      = fold; break;
                    case ShapeType_Rect.Corner:         cornerFoldOut         = fold; break;
                    case ShapeType_Rect.Isolate:        isolateFoldOut        = fold; break;
                }
                TilemapSplitterWindow.AddHorizontalSeparator(container);
            }
        }

        private Foldout CreateFoldout(VisualElement parentContainer, ShapeType_Rect type, string title)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = settingsDict[type];
            if (type == ShapeType_Rect.VerticalEdge ||
                type == ShapeType_Rect.HorizontalEdge)
            {
                AddShapeSettingControls(fold, setting);
            }
            else
            {
                var enumF = new EnumFlagsField("Which obj to add to", setting.flags);
                fold.Add(enumF);

                Toggle previewT    = null;
                ColorField colorF  = null;
                (previewT, colorF) = AddShapeSettingControls(fold, setting);

                enumF.RegisterValueChangedCallback(evt =>
                {
                    setting.flags = (ShapeFlags)evt.newValue;
                    refreshPreview();
                    RefreshFoldoutUI(setting, fold, previewT, colorF);
                });

                RefreshFoldoutUI(setting, fold, previewT, colorF);
            }

            parentContainer.Add(fold);
            return fold;
        }

        private (Toggle previewToggle, ColorField colorField) AddShapeSettingControls(Foldout fold,
            ShapeSetting setting)
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

            return (previewT, colF);
        }

        private void RefreshFoldoutUI(ShapeSetting setting, Foldout fold,
            Toggle previewToggle, ColorField colField)
        {
            var opt = setting.flags;

            var exist = fold.Q<HelpBox>();
            if (exist != null) fold.Remove(exist);

            string msg   = null;
            string helpV = "The canPreview complies with VerticalEdge shapeSettings_Rect.";
            string helpH = "The canPreview complies with HorizontalEdge shapeSettings_Rect.";
            if      (opt.HasFlag(ShapeFlags.VerticalEdge))   msg = helpV;
            else if (opt.HasFlag(ShapeFlags.HorizontalEdge)) msg = helpH;

            if (string.IsNullOrEmpty(msg) == false)
            {
                fold.Add(new HelpBox(msg, HelpBoxMessageType.Info));
            }

            bool isVisible = opt.HasFlag(ShapeFlags.Independent);
            previewToggle.visible = isVisible;
            colField.visible      = isVisible;
        }

        public IEnumerator<bool> Classify(Tilemap source)
        {
            shapeCells = new ShapeCells_Rect();
            return TileShapeClassifier.ClassifyCoroutine_Rect(source, settingsDict, shapeCells);
        }

        public void GenerateSplitTilemaps(Tilemap source, bool canMergeEdges, bool canAttachCollider)
        {
            TilemapCreator.GenerateSplitTilemaps_Rect(source, shapeCells, settingsDict, canMergeEdges, canAttachCollider);
        }

        public void SetupPreview(Tilemap source, TilemapPreviewDrawer drawer)
        {
            drawer.Setup_Rect(source, settingsDict);
        }

        public void SetShapeCellsToPreview(TilemapPreviewDrawer drawer)
        {
            drawer.SetShapeCells(shapeCells);
        }

        public void UpdateFoldoutTitles()
        {
            var list = new (Foldout f, string name, int count)[]
            {
                (verticalEdgeFoldOut,   "VerticalEdge",   shapeCells.Vertical.Count),
                (horizontalEdgeFoldOut, "HorizontalEdge", shapeCells.Horizontal.Count),
                (crossFoldOut,          "Cross",         shapeCells.Cross.Count),
                (tJunctionFoldOut,      "TJunction",     shapeCells.TJunction.Count),
                (cornerFoldOut,         "Corner",        shapeCells.Corner.Count),
                (isolateFoldOut,        "Isolate",       shapeCells.Isolate.Count)
            };
            foreach (var (f, name, count) in list)
            {
                f.text = $"{name} (Count:{count})";
            }
        }
    }

    /// <summary>
    /// Layout strategy for hexagonal grids.
    /// </summary>
    internal class HexLayoutStrategy : ICellLayoutStrategy
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

        public HexLayoutStrategy(Dictionary<ShapeType_Hex, ShapeSetting> settings, Action refreshPreview)
        {
            settingsDict = settings;
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

        public IEnumerator<bool> Classify(Tilemap source)
        {
            shapeCells = new ShapeCells_Hex();
            return TileShapeClassifier.ClassifyCoroutine_Hex(source, settingsDict, shapeCells);
        }

        public void GenerateSplitTilemaps(Tilemap source, bool canMergeEdges, bool canAttachCollider)
        {
            TilemapCreator.GenerateSplitTilemaps_Hex(source, shapeCells, settingsDict, canAttachCollider);
        }

        public void SetupPreview(Tilemap source, TilemapPreviewDrawer drawer)
        {
            drawer.Setup_Hex(source, settingsDict);
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
