namespace TilemapSplitter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    internal class TilemapSplitterWindow : EditorWindow
    {
        private const string PrefPrefix = "TilemapSplitter.";
        private static string CreateKey(string name) => PrefPrefix + name;

        private Dictionary<ShapeType, ShapeSetting> settingsDict = CreateDefaultSettings();
        private static Dictionary<ShapeType, ShapeSetting> CreateDefaultSettings() => new()
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
        private ShapeCells shapeCells = new();
        private readonly TilemapPreviewDrawer previewDrawer = new();

        private bool canMergeEdges       = false;
        private bool canAttachCollider   = false;
        private bool isRefreshingPreview = false;

        [MenuItem("Tools/TilemapSplitter")]
        public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("Split Tilemap");

        private void OnEnable()
        {
            LoadPrefs();
            previewDrawer.Register();
        }

        private void OnDisable()
        {
            SavePrefs();
            previewDrawer.Unregister();
        }

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
            var sourceF = new ObjectField("Split Tilemap");
            var helpBox     = new HelpBox("Select the subject of the division", HelpBoxMessageType.Info);
            helpBox.visible = (source == null);
            sourceF.objectType = typeof(Tilemap);
            sourceF.value      = source;
            sourceF.RegisterValueChangedCallback(evt =>
            {
                source = evt.newValue as Tilemap;
                helpBox.visible = (source == null);
                RefreshPreview();
            });
            container.Add(sourceF);
            container.Add(helpBox);

            AddHorizontalSeparator(container);

            //Create Split Settings Button
            var resetB = new Button(() =>
            {
                ResetPrefs();
                root.Clear();
                CreateGUI();
            });
            resetB.text            = "Reset Settings";
            resetB.style.marginTop = 5;
            container.Add(resetB);

            //Create Colliders Attach Button
            var attachT = new Toggle("Attach Colliders");
            attachT.value = canAttachCollider;
            attachT.RegisterValueChangedCallback(evt => canAttachCollider = evt.newValue);
            container.Add(attachT);

            //Create Vertical, Horizontal Edge Shape Settings UI
            var mergeT  = new Toggle("Merge VerticalEdge, HorizontalEdge");
            var mergeHB = new HelpBox("When merging, VerticalEdge shapeSettings take precedence",
                HelpBoxMessageType.Info);
            mergeT.value = canMergeEdges;
            mergeT.RegisterValueChangedCallback(evt => canMergeEdges = evt.newValue);
            container.Add(mergeT);
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
            var splitB = new Button(() =>
            {
                if (source == null)
                {
                    EditorUtility.DisplayDialog("Error", "The split target isn't set", "OK");
                    return;
                }
                StartCoroutine(SplitCoroutine());
            });
            splitB.text            = "Execute Splitting";
            splitB.style.marginTop = 10;
            container.Add(splitB);

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

            var enumF = new EnumFlagsField("Which obj to add to", setting.flags);
            fold.Add(enumF);

            var (previewToggle, colField) = AddShapeSettingControls(fold, setting);

            enumF.RegisterValueChangedCallback(evt =>
            {
                setting.flags = (ShapeFlags)evt.newValue;
                RefreshPreview();
                RefreshFoldoutUI(setting, fold, previewToggle, colField);
            });

            RefreshFoldoutUI(setting, fold, previewToggle, colField);
            parentContainer.Add(fold);
            return fold;
        }

        private (Toggle previewToggle, ColorField colorField) AddShapeSettingControls(Foldout fold,
            ShapeSetting setting)
        {
            var layerF = new LayerField("Layer", setting.layer);
            layerF.RegisterValueChangedCallback(evt => setting.layer = evt.newValue);
            fold.Add(layerF);

            var tagF = new TagField("Tag", setting.tag);
            tagF.RegisterValueChangedCallback(evt => setting.tag = evt.newValue);
            fold.Add(tagF);

            var previewT = new Toggle("Preview") { value = setting.canPreview };
            previewT.RegisterValueChangedCallback(evt =>
            {
                setting.canPreview = evt.newValue;
                RefreshPreview();
            });
            fold.Add(previewT);

            var colF = new ColorField("Preview Color") { value = setting.previewColor };
            colF.RegisterValueChangedCallback(evt => setting.previewColor = evt.newValue);
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
            shapeCells = new ShapeCells();
            var  e = TileShapeClassifier.ClassifyCoroutine(source, settingsDict, shapeCells);

            while (e.MoveNext()) yield return null;

            TilemapCreator.GenerateSplitTilemaps(source, shapeCells, settingsDict,
                canMergeEdges, canAttachCollider);
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (source == null || isRefreshingPreview) return;
            StartCoroutine(RefreshPreviewCoroutine());

            IEnumerator RefreshPreviewCoroutine()
            {
                isRefreshingPreview = true;

                shapeCells = new ShapeCells();
                var e = TileShapeClassifier.ClassifyCoroutine(source, settingsDict, shapeCells);
                while (e.MoveNext())
                {
                    yield return null;
                }

                previewDrawer.Setup(source, settingsDict);
                previewDrawer.SetShapeCells(shapeCells);
                SceneView.RepaintAll();
                UpdateFoldoutTitles();

                isRefreshingPreview = false;
            }
        }

        private void UpdateFoldoutTitles()
        {
            var list = new (Foldout f, string name, int count)[]
            {
                (verticalEdgeFoldOut,   "VerticalEdge",   shapeCells.VerticalCells.Count),
                (horizontalEdgeFoldOut, "HorizontalEdge", shapeCells.HorizontalCells.Count),
                (crossFoldOut,          "CrossCells",     shapeCells.CrossCells.Count),
                (tJunctionFoldOut,      "TJunctionCells", shapeCells.TJunctionCells.Count),
                (cornerFoldOut,         "CornerCells",    shapeCells.CornerCells.Count),
                (isolateFoldOut,        "IsolateCells",   shapeCells.IsolateCells.Count),
            };
            foreach (var (f, name, count) in list)
            {
                f.text = $"{name} (Count:{count})";
            }
        }

        #region Saving and Loading Split Settings using EditorPrefs

        private void SavePrefs()
        {
            if (source != null)
            {
                string path = AssetDatabase.GetAssetPath(source);
                EditorPrefs.SetString(CreateKey("SourcePath"), path);
            }
            else
            {
                EditorPrefs.DeleteKey(CreateKey("SourcePath"));
            }

            EditorPrefs.SetBool(CreateKey("CanMergeEdges"),  canMergeEdges);
            EditorPrefs.SetBool(CreateKey("AttachCollider"), canAttachCollider);

            foreach (var kv in settingsDict)
            {
                string name = kv.Key.ToString();
                var setting = kv.Value;
                EditorPrefs.SetInt(CreateKey($"{name}.Flags"), (int)setting.flags);
                EditorPrefs.SetInt(CreateKey($"{name}.Layer"), setting.layer);
                EditorPrefs.SetString(CreateKey($"{name}.Tag"), setting.tag);
                EditorPrefs.SetBool(CreateKey($"{name}.CanPreview"), setting.canPreview);
                EditorPrefs.SetString(CreateKey($"{name}.Color"),
                    ColorUtility.ToHtmlStringRGBA(setting.previewColor));
            }
        }

        private void LoadPrefs()
        {
            if (EditorPrefs.HasKey(CreateKey("SourcePath")))
            {
                var path = EditorPrefs.GetString(CreateKey("SourcePath"));
                source   = AssetDatabase.LoadAssetAtPath<Tilemap>(path);
            }

            canMergeEdges     = EditorPrefs.GetBool(CreateKey("CanMergeEdges"), canMergeEdges);
            canAttachCollider = EditorPrefs.GetBool(CreateKey("AttachCollider"), canAttachCollider);

            foreach (var kv in settingsDict)
            {
                var name    = kv.Key.ToString();
                var setting = kv.Value;
                setting.flags      = (ShapeFlags)EditorPrefs.GetInt(CreateKey($"{name}.Flags"), (int)setting.flags);
                setting.layer      = EditorPrefs.GetInt(CreateKey($"{name}.Layer"), setting.layer);
                setting.tag        = EditorPrefs.GetString(CreateKey($"{name}.Tag"), setting.tag);
                setting.canPreview = EditorPrefs.GetBool(CreateKey($"{name}.CanPreview"), setting.canPreview);
                string col = EditorPrefs.GetString(CreateKey($"{name}.Color"), ColorUtility.ToHtmlStringRGBA(setting.previewColor));
                if (ColorUtility.TryParseHtmlString("#" + col, out var c))
                {
                    setting.previewColor = c;
                }
            }
        }

        private void ResetPrefs()
        {
            EditorPrefs.DeleteKey(CreateKey("SourcePath"));
            EditorPrefs.DeleteKey(CreateKey("CanMergeEdges"));
            EditorPrefs.DeleteKey(CreateKey("AttachCollider"));

            foreach (ShapeType t in Enum.GetValues(typeof(ShapeType)))
            {
                string name = t.ToString();
                EditorPrefs.DeleteKey(CreateKey($"{name}.Flags"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Layer"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Tag"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.CanPreview"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Color"));
            }

            settingsDict      = CreateDefaultSettings();
            source            = null;
            canMergeEdges     = false;
            canAttachCollider = false;
        }

        #endregion
    }
}
