namespace TilemapSplitter
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    internal class TilemapSplitterWindow : EditorWindow
    {
        private readonly Dictionary<ShapeType, ShapeSetting> settingsDict = new()
        {
            [ShapeType.VerticalEdge]   = new() { flags = ShapeFlags.VerticalEdge,   previewColor = Color.green  },
            [ShapeType.HorizontalEdge] = new() { flags = ShapeFlags.HorizontalEdge, previewColor = Color.yellow },
            [ShapeType.Cross]          = new() { flags = ShapeFlags.Independent,    previewColor = Color.red    },
            [ShapeType.TJunction]      = new() { flags = ShapeFlags.Independent,    previewColor = Color.blue   },
            [ShapeType.Corner]         = new() { flags = ShapeFlags.Independent,    previewColor = Color.cyan   },
            [ShapeType.Isolate]        = new() { flags = ShapeFlags.Independent,    previewColor = Color.magenta },
        };

        private Foldout verticalEdgeFoldOut;
        private Foldout horizontalEdgeFoldOut;
        private Foldout crossFoldOut;
        private Foldout tJunctionFoldOut;
        private Foldout cornerFoldOut;
        private Foldout isolateFoldOut;

        private Tilemap source;
        private ShapeCells result = new();
        private readonly TilemapPreviewDrawer previewDrawer = new();

        private bool canMergeEdges = false;
        private bool isRefreshingPreview = false;

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
            var sourceField = new ObjectField("Split Tilemap");
            var helpBox       = new HelpBox("Select the subject of the division", HelpBoxMessageType.Info);
            helpBox.visible = (source == null);
            sourceField.objectType = typeof(Tilemap);
            sourceField.value      = source;
            sourceField.RegisterValueChangedCallback(evt =>
            {
                source = evt.newValue as Tilemap;
                helpBox.visible = (source == null);
                RefreshPreview();
            });
            container.Add(sourceField);
            container.Add(helpBox);

            AddHorizontalSeparator(container);

            //Create Vertical, Horizontal Edge Shape Settings UI
            var mergeToggle = new Toggle("Merge VerticalEdge, HorizontalEdge");
            var mergeHB     = new HelpBox("When merging, VerticalEdge shapeSettings take precedence",
                HelpBoxMessageType.Info);
            mergeToggle.value = canMergeEdges;
            mergeToggle.RegisterValueChangedCallback(evt => canMergeEdges = evt.newValue);
            container.Add(mergeToggle);
            container.Add(mergeHB);

            verticalEdgeFoldOut   = CreateEdgeFoldout(container, "VerticalEdge", ShapeType.VerticalEdge);
            AddHorizontalSeparator(container);
            horizontalEdgeFoldOut = CreateEdgeFoldout(container, "HorizontalEdge", ShapeType.HorizontalEdge);
            AddHorizontalSeparator(container);

            //Create Split Each Shape Settings UI
            var infos = new (string title, ShapeType type)[]
            {
                ("Cross",      ShapeType.Cross),
                ("T-Junction", ShapeType.TJunction),
                ("Corner",     ShapeType.Corner),
                ("Isolate",    ShapeType.Isolate),
            };
            foreach (var info in infos)
            {
                var fold = CreateFoldout(container, info.title, settingsDict[info.type]);
                switch (info.type)
                {
                    case ShapeType.Cross:     crossFoldOut     = fold; break;
                    case ShapeType.TJunction: tJunctionFoldOut = fold; break;
                    case ShapeType.Corner:    cornerFoldOut    = fold; break;
                    case ShapeType.Isolate:   isolateFoldOut   = fold; break;
                }
                AddHorizontalSeparator(container);
            }

            //Add the Execute Splitting button at the bottom of the UI
            var splitButton = new Button(() =>
            {
                if (source == null)
                {
                    EditorUtility.DisplayDialog("Error", "The split target isn't set", "OK");
                    return;
                }
                StartCoroutine(SplitCoroutine());
            });
            splitButton.text            = "Execute Splitting";
            splitButton.style.marginTop = 10;
            container.Add(splitButton);

            previewDrawer.Setup(source, settingsDict);
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

        private static void StartCoroutine(IEnumerator e)
        {
            EditorApplication.update += Update;

            void Update()
            {
                if (e.MoveNext() == false) EditorApplication.update -= Update;
            }
        }

        private IEnumerator SplitCoroutine()
        {
            result = new ShapeCells();
            var  e = TileShapeClassifier.ClassifyCoroutine(source, settingsDict, result);

            while (e.MoveNext()) yield return null;

            TilemapCreator.GenerateSplitTilemaps(source, result, settingsDict, canMergeEdges);
            RefreshPreview();
        }

        private (Toggle previewToggle, ColorField colorField) AddShapeSettingControls(Foldout fold,
            ShapeSetting setting)
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

        private Foldout CreateEdgeFoldout(VisualElement parentContainer, string title, ShapeType type)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = settingsDict[type];
            AddShapeSettingControls(fold, setting);

            parentContainer.Add(fold);
            return fold;
        }

        private Foldout CreateFoldout(VisualElement parentContainer, string title, ShapeSetting setting)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var enumField = new EnumFlagsField("Which obj to add to", setting.flags);
            fold.Add(enumField);

            var (previewToggle, colField) = AddShapeSettingControls(fold, setting);

            enumField.RegisterValueChangedCallback(evt =>
            {
                setting.flags = (ShapeFlags)evt.newValue;
                RefreshPreview();
                RefreshFoldoutUI(setting, fold, previewToggle, colField);
            });

            RefreshFoldoutUI(setting, fold, previewToggle, colField);
            parentContainer.Add(fold);
            return fold;
        }

        private void RefreshFoldoutUI(ShapeSetting setting, Foldout fold,
            Toggle previewToggle, ColorField colField)
        {
            var opt = setting.flags;

            var exist = fold.Q<HelpBox>();
            if (exist != null) fold.Remove(exist);

            string msg   = null;
            string helpV = "The canPreview complies with VerticalEdge shapeSettings.";
            string helpH = "The canPreview complies with HorizontalEdge shapeSettings.";
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

        private void RefreshPreview()
        {
            if (source == null || isRefreshingPreview) return;
            StartCoroutine(RefreshPreviewCoroutine());

            IEnumerator RefreshPreviewCoroutine()
            {
                isRefreshingPreview = true;

                result = new ShapeCells();
                var e = TileShapeClassifier.ClassifyCoroutine(source, settingsDict, result);
                while (e.MoveNext())
                {
                    yield return null;
                }

                previewDrawer.Setup(source, settingsDict);
                previewDrawer.SetShapeResult(result);
                SceneView.RepaintAll();
                UpdateFoldoutTitles();

                isRefreshingPreview = false;
            }
        }

        private void UpdateFoldoutTitles()
        {
            var list = new (Foldout f, string name, int count)[]
            {
                (verticalEdgeFoldOut,   "VerticalEdge",   result.VerticalCells.Count),
                (horizontalEdgeFoldOut, "HorizontalEdge", result.HorizontalCells.Count),
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
