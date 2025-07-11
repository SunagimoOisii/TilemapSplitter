namespace TilemapSplitter
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    internal class TilemapSplitterWindow : EditorWindow
    {
        private readonly TileShapeSetting[] settings = new TileShapeSetting[6]
        {
            new() { flags = TileShapeFlags.VerticalEdge,   previewColor = Color.green  },
            new() { flags = TileShapeFlags.HorizontalEdge, previewColor = Color.yellow },
            new() { flags = TileShapeFlags.Independent,    previewColor = Color.red    },
            new() { flags = TileShapeFlags.Independent,    previewColor = Color.blue   },
            new() { flags = TileShapeFlags.Independent,    previewColor = Color.cyan   },
            new() { flags = TileShapeFlags.Independent,    previewColor = Color.magenta },
        };
        private TileShapeSetting GetShapeSetting(TileShapeType t) => settings[(int)t];

        private Foldout verticalEdgeFoldOut;
        private Foldout horizontalEdgeFoldOut;
        private Foldout crossFoldOut;
        private Foldout tJunctionFoldOut;
        private Foldout cornerFoldOut;
        private Foldout isolateFoldOut;

        private Tilemap original;
        private ShapeCells result = new();
        private readonly TilemapPreviewDrawer previewDrawer = new();

        private bool canMergeEdges = false;

        [MenuItem("Tools/TilemapSplitter")]
        public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("Split Tilemap");

        private void OnEnable() { previewDrawer.Register(); }

        private void OnDisable() { previewDrawer.Unregister(); }

        public void CreateGUI()
        {
            var root = rootVisualElement;

            //Create a ScrollView and a container VisualElement
            var scroll    = new ScrollView();
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft   = 10;
            container.style.paddingRight  = 10;
            root.Add(scroll);
            scroll.Add(container);

            //Create an ObjectField and HelpBox for the user to select the source Tilemap asset
            var originalField = new ObjectField("Split Tilemap");
            var helpBox       = new HelpBox("Select the subject of the division", HelpBoxMessageType.Info);
            helpBox.visible = (original == null);
            originalField.objectType = typeof(Tilemap);
            originalField.value      = original;
            originalField.RegisterValueChangedCallback(evt =>
            {
                original = evt.newValue as Tilemap;
                helpBox.visible = (original == null);
                RefreshPreview();
            });
            container.Add(originalField);
            container.Add(helpBox);

            AddHorizontalSeparator(container);

            //Create Vertical, Horizontal Edge Shape Settings UI
            var mergeToggle = new Toggle("Merge VerticalEdge, HorizontalEdge");
            var mergeHB = new HelpBox("When merging, VerticalEdge shapeSettings take precedence",
                HelpBoxMessageType.Info);
            mergeToggle.value = canMergeEdges;
            mergeToggle.RegisterValueChangedCallback(evt => canMergeEdges = evt.newValue);
            container.Add(mergeToggle);
            container.Add(mergeHB);

            verticalEdgeFoldOut = CreateEdgeFoldout(container, "VerticalEdge",
                TileShapeType.VerticalEdge);
            AddHorizontalSeparator(container);
            horizontalEdgeFoldOut = CreateEdgeFoldout(container, "HorizontalEdge",
                TileShapeType.HorizontalEdge);
            AddHorizontalSeparator(container);

            //Create Split Each Shape Settings UI
            var infos = new (string title, TileShapeType type)[]
            {
                ("Cross",      TileShapeType.Cross),
                ("T-Junction", TileShapeType.TJunction),
                ("Corner",     TileShapeType.Corner),
                ("Isolate",    TileShapeType.Isolate),
            };
            foreach (var info in infos)
            {
                var fold = CreateFoldout(container, info.title, GetShapeSetting(info.type));
                switch (info.type)
                {
                    case TileShapeType.Cross:     crossFoldOut     = fold; break;
                    case TileShapeType.TJunction: tJunctionFoldOut = fold; break;
                    case TileShapeType.Corner:    cornerFoldOut    = fold; break;
                    case TileShapeType.Isolate:   isolateFoldOut   = fold; break;
                }
                AddHorizontalSeparator(container);
            }

            //Add the Execute Splitting button at the bottom of the UI
            var splitButton = new Button(() =>
            {
                if (original == null)
                {
                    EditorUtility.DisplayDialog("Error", "The split target isn't set", "OK");
                    return;
                }
                result = TileShapeClassifier.Classify(original, settings);
                TilemapCreator.GenerateSplitTilemaps(original, result, settings, canMergeEdges);
                RefreshPreview();
            });
            splitButton.text            = "Execute Splitting";
            splitButton.style.marginTop = 10;
            container.Add(splitButton);

            previewDrawer.Setup(original, settings);
        }

        private static void AddHorizontalSeparator(VisualElement parentContainer)
        {
            var separator = new VisualElement();
            separator.style.borderBottomWidth = 1;
            separator.style.borderBottomColor = Color.gray;
            separator.style.marginTop         = 5;
            separator.style.marginBottom      = 5;

            parentContainer.Add(separator);
        }

        private (Toggle previewToggle, ColorField colorField) AddShapeSettingControls(Foldout fold,
            TileShapeSetting setting)
        {
            var layerField = new LayerField("Layer", setting.layer);
            layerField.RegisterValueChangedCallback(evt => setting.layer = evt.newValue);
            fold.Add(layerField);

            var tagField = new TagField("Tag", setting.tag);
            tagField.RegisterValueChangedCallback(evt => setting.tag = evt.newValue);
            fold.Add(tagField);

            var previewToggle = new Toggle("Preview") { value = setting.canPreview };
            previewToggle.RegisterValueChangedCallback(evt =>
            {
                setting.canPreview = evt.newValue;
                RefreshPreview();
            });
            fold.Add(previewToggle);

            var colField = new ColorField("Preview Color") { value = setting.previewColor };
            colField.RegisterValueChangedCallback(evt => setting.previewColor = evt.newValue);
            fold.Add(colField);

            return (previewToggle, colField);
        }

        private Foldout CreateEdgeFoldout(VisualElement parentContainer, string title, TileShapeType type)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = GetShapeSetting(type);
            AddShapeSettingControls(fold, setting);

            parentContainer.Add(fold);
            return fold;
        }

        private Foldout CreateFoldout(VisualElement parentContainer, string title, TileShapeSetting setting)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var enumField = new EnumFlagsField("Which obj to add to", setting.flags);
            fold.Add(enumField);

            var (previewToggle, colField) = AddShapeSettingControls(fold, setting);

            enumField.RegisterValueChangedCallback(evt =>
            {
                setting.flags = (TileShapeFlags)evt.newValue;
                RefreshPreview();
                RefreshFoldoutUI(setting, fold, previewToggle, colField);
            });

            RefreshFoldoutUI(setting, fold, previewToggle, colField);
            parentContainer.Add(fold);
            return fold;
        }

        private void RefreshFoldoutUI(TileShapeSetting setting, Foldout fold,
            Toggle previewToggle, ColorField colField)
        {
            var opt = setting.flags;

            var exist = fold.Q<HelpBox>();
            if (exist != null) fold.Remove(exist);

            string msg   = null;
            string helpV = "The canPreview complies with VerticalEdge shapeSettings.";
            string helpH = "The canPreview complies with HorizontalEdge shapeSettings.";
            if      (opt.HasFlag(TileShapeFlags.VerticalEdge))   msg = helpV;
            else if (opt.HasFlag(TileShapeFlags.HorizontalEdge)) msg = helpH;

            if (string.IsNullOrEmpty(msg) == false)
            {
                fold.Add(new HelpBox(msg, HelpBoxMessageType.Info));
            }

            bool isVisible = opt.HasFlag(TileShapeFlags.Independent);
            previewToggle.visible = isVisible;
            colField.visible      = isVisible;
        }

        private void RefreshPreview()
        {
            if (original == null) return;

            result = TileShapeClassifier.Classify(original, settings);
            previewDrawer.Setup(original, settings);
            previewDrawer.SetShapeResult(result);
            SceneView.RepaintAll();
            UpdateFoldoutTitles();
        }

        private void UpdateFoldoutTitles()
        {
            var list = new (Foldout f, string name, int count)[]
            {
                (verticalEdgeFoldOut,   "VerticalEdge",   result.VerticalEdgesCells.Count),
                (horizontalEdgeFoldOut, "HorizontalEdge", result.HorizontalEdgesCells.Count),
                (crossFoldOut,          "CrossCells",     result.CrossCells.Count),
                (tJunctionFoldOut,      "TJunctionCells", result.TJunctionCells.Count),
                (cornerFoldOut,         "CornerCells",    result.CornerCells.Count),
                (isolateFoldOut,        "IsolateCells",   result.IsolateCells.Count),
            };
            foreach (var (f, name, count) in list)
            {
                f.text = $"{name} (Count:{count})";
            }
        }
    }
}
