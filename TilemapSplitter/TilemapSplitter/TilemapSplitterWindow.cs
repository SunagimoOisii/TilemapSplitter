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

        private Dictionary<ShapeType_Rect, ShapeSetting> settingsDict_rect = CreateDefaultSettings_Rect();
        private Dictionary<ShapeType_Hex, ShapeSetting>  settingsDict_hex  = CreateDefaultSettings_Hex();

        private static Dictionary<ShapeType_Rect, ShapeSetting> CreateDefaultSettings_Rect() => new()
        {
            [ShapeType_Rect.VerticalEdge]   = new() { flags = ShapeFlags.VerticalEdge,   previewColor = Color.green  },
            [ShapeType_Rect.HorizontalEdge] = new() { flags = ShapeFlags.HorizontalEdge, previewColor = Color.yellow },
            [ShapeType_Rect.Cross]          = new() { flags = ShapeFlags.Independent,    previewColor = Color.red    },
            [ShapeType_Rect.TJunction]      = new() { flags = ShapeFlags.Independent,    previewColor = Color.blue   },
            [ShapeType_Rect.Corner]         = new() { flags = ShapeFlags.Independent,    previewColor = Color.cyan   },
            [ShapeType_Rect.Isolate]        = new() { flags = ShapeFlags.Independent,    previewColor = Color.magenta },
        };
        private static Dictionary<ShapeType_Hex, ShapeSetting> CreateDefaultSettings_Hex() => new()
        {
            [ShapeType_Hex.Full]      = new() { flags = ShapeFlags.Independent, previewColor = Color.red    },
            [ShapeType_Hex.Junction5] = new() { flags = ShapeFlags.Independent, previewColor = Color.blue   },
            [ShapeType_Hex.Junction4] = new() { flags = ShapeFlags.Independent, previewColor = Color.cyan   },
            [ShapeType_Hex.Junction3] = new() { flags = ShapeFlags.Independent, previewColor = Color.green  },
            [ShapeType_Hex.Edge]      = new() { flags = ShapeFlags.Independent, previewColor = Color.yellow },
            [ShapeType_Hex.Tip]       = new() { flags = ShapeFlags.Independent, previewColor = Color.magenta },
            [ShapeType_Hex.Isolate]   = new() { flags = ShapeFlags.Independent, previewColor = Color.gray   },
        };

        //For Rectangle or Isolate
        private Foldout verticalEdgeFoldOut;
        private Foldout horizontalEdgeFoldOut;
        private Foldout crossFoldOut;
        private Foldout tJunctionFoldOut;
        private Foldout cornerFoldOut;
        private Foldout isolateFoldOut;
        //For Hexagon
        private Foldout fullFoldOut;
        private Foldout junction5FoldOut;
        private Foldout junction4FoldOut;
        private Foldout junction3FoldOut;
        private Foldout edgeFoldOut;
        private Foldout tipFoldOut;
        private Foldout hexIsolateFoldOut;

        private Tilemap source;

        private ShapeCells_Rect    shapeCells = new();
        private ShapeCells_Hex hexShapeCells = new();

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
            var c = CreateScrollableContainer();
            CreateSourceField(c);
            CreateResetButton(c);
            CreateColliderToggle(c);
            CreateMergeEdgeToggle(c);

            if (source == null) return;

            var layout = source.layoutGrid.cellLayout;
            if (layout == GridLayout.CellLayout.Hexagon) CreateShapeFoldouts_Hex(c);
            else                                         CreateShapeFoldouts_Rect(c);

            CreateExecuteButton(c);

            if (layout == GridLayout.CellLayout.Hexagon) previewDrawer.Setup_Hex(source, settingsDict_hex);
            else                                         previewDrawer.Setup_Rect(source, settingsDict_rect);
        }

        private VisualElement CreateScrollableContainer()
        {
            //Create a ScrollView and a container VisualElement
            var scroll    = new ScrollView();
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft   = 10;
            container.style.paddingRight  = 10;
            rootVisualElement.Add(scroll);
            scroll.Add(container);

            AddHorizontalSeparator(container);
            return container;
        }

        private void CreateSourceField(VisualElement container)
        {
            var sourceF = new ObjectField("Split Tilemap");
            var hp      = new HelpBox("Select the subject of the division", HelpBoxMessageType.Info);
            sourceF.objectType = typeof(Tilemap);
            sourceF.value      = source;
            sourceF.RegisterValueChangedCallback(evt =>
            {
                source     = evt.newValue as Tilemap;
                hp.visible = (source == null);

                rootVisualElement.Clear();
                CreateGUI();
                RefreshPreview();
            });
            hp.visible = (source == null);
            container.Add(sourceF);
            container.Add(hp);
        }

        private void CreateResetButton(VisualElement container)
        {
            var resetB = new Button(() =>
            {
                ResetPrefs();
                rootVisualElement.Clear();
                CreateGUI();
            });
            resetB.text            = "Reset Settings";
            resetB.style.marginTop = 5;
            container.Add(resetB);
        }

        private void CreateColliderToggle(VisualElement container)
        {
            var attachT   = new Toggle("Attach Colliders");
            attachT.value = canAttachCollider;
            attachT.RegisterValueChangedCallback(evt => canAttachCollider = evt.newValue);
            container.Add(attachT);
        }

        private void CreateMergeEdgeToggle(VisualElement container)
        {
            var mergeT  = new Toggle("Merge VerticalEdge, HorizontalEdge");
            var mergeHB = new HelpBox("When merging, VerticalEdge shapeSettings_Rect take precedence",
                HelpBoxMessageType.Info);
            mergeT.value = canMergeEdges;
            mergeT.RegisterValueChangedCallback(evt => canMergeEdges = evt.newValue);
            container.Add(mergeT);
            container.Add(mergeHB);
        }

        private void CreateShapeFoldouts_Rect(VisualElement container)
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
                var fold = CreateFoldout_Rect(container, info.type, info.title);
                switch (info.type)
                {
                    case ShapeType_Rect.VerticalEdge:   verticalEdgeFoldOut   = fold; break;
                    case ShapeType_Rect.HorizontalEdge: horizontalEdgeFoldOut = fold; break;
                    case ShapeType_Rect.Cross:          crossFoldOut          = fold; break;
                    case ShapeType_Rect.TJunction:      tJunctionFoldOut      = fold; break;
                    case ShapeType_Rect.Corner:         cornerFoldOut         = fold; break;
                    case ShapeType_Rect.Isolate:        isolateFoldOut        = fold; break;
                }
                AddHorizontalSeparator(container);
            }
        }

        private void CreateShapeFoldouts_Hex(VisualElement container)
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
                var fold = CreateFoldout_Hex(container, info.type, info.title);
                switch (info.type)
                {
                    case ShapeType_Hex.Full:      fullFoldOut        = fold; break;
                    case ShapeType_Hex.Junction5: junction5FoldOut   = fold; break;
                    case ShapeType_Hex.Junction4: junction4FoldOut   = fold; break;
                    case ShapeType_Hex.Junction3: junction3FoldOut   = fold; break;
                    case ShapeType_Hex.Edge:      edgeFoldOut        = fold; break;
                    case ShapeType_Hex.Tip:       tipFoldOut         = fold; break;
                    case ShapeType_Hex.Isolate:   hexIsolateFoldOut  = fold; break;
                }
                AddHorizontalSeparator(container);
            }
        }

        private void CreateExecuteButton(VisualElement container)
        {
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

        private Foldout CreateFoldout_Rect(VisualElement parentContainer, ShapeType_Rect type, string title)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = settingsDict_rect[type];
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
                    RefreshPreview();
                    RefreshFoldoutUI(setting, fold, previewT, colorF);
                });

                RefreshFoldoutUI(setting, fold, previewT, colorF);
            }

            parentContainer.Add(fold);
            return fold;
        }

        private Foldout CreateFoldout_Hex(VisualElement parentContainer, ShapeType_Hex type, string title)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = settingsDict_hex[type];
            AddShapeSettingControls(fold, setting);

            parentContainer.Add(fold);
            return fold;
        }

        private (Toggle previewToggle, ColorField colorField) AddShapeSettingControls(Foldout fold,
            ShapeSetting setting)
        {
            //Create controls and register callbacks
            var layerF   = new LayerField("Layer", setting.layer);
            var tagF     = new TagField("Tag", setting.tag);
            var previewT = new Toggle("Preview") { value = setting.canPreview };
            var colF     = new ColorField("Preview Color") { value = setting.previewColor };
            layerF.RegisterValueChangedCallback(evt => setting.layer = evt.newValue);
            tagF.RegisterValueChangedCallback(evt => setting.tag = evt.newValue);
            previewT.RegisterValueChangedCallback(evt =>
            {
                setting.canPreview = evt.newValue;
                RefreshPreview();
            });
            colF.RegisterValueChangedCallback(evt => setting.previewColor = evt.newValue);

            //Add creation controls to foldout
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
            bool isHex = source.layoutGrid.cellLayout == GridLayout.CellLayout.Hexagon;
            IEnumerator e;
            if (isHex)
            {
                hexShapeCells = new ShapeCells_Hex();
                e = TileShapeClassifier.ClassifyCoroutine_Hex(source, settingsDict_hex, hexShapeCells);
            }
            else
            {
                shapeCells = new ShapeCells_Rect();
                e = TileShapeClassifier.ClassifyCoroutine_Rect(source, settingsDict_rect, shapeCells);
            }

            while (e.MoveNext()) yield return null;

            if (isHex)
            {
                TilemapCreator.GenerateSplitTilemaps_Hex(source, hexShapeCells, settingsDict_hex, canAttachCollider);
            }
            else
            {
                TilemapCreator.GenerateSplitTilemaps_Rect(source, shapeCells, settingsDict_rect,
                    canMergeEdges, canAttachCollider);
            }
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (source == null || isRefreshingPreview) return;
            StartCoroutine(RefreshPreviewCoroutine());

            IEnumerator RefreshPreviewCoroutine()
            {
                isRefreshingPreview = true;

                bool isHex = source.layoutGrid.cellLayout == GridLayout.CellLayout.Hexagon;
                IEnumerator e;
                if (isHex)
                {
                    hexShapeCells = new ShapeCells_Hex();
                    e = TileShapeClassifier.ClassifyCoroutine_Hex(source, settingsDict_hex, hexShapeCells);
                }
                else
                {
                    shapeCells = new ShapeCells_Rect();
                    e = TileShapeClassifier.ClassifyCoroutine_Rect(source, settingsDict_rect, shapeCells);
                }
                while (e.MoveNext())
                {
                    yield return null;
                }

                if (isHex)
                {
                    previewDrawer.Setup_Hex(source, settingsDict_hex);
                    previewDrawer.SetShapeCells(hexShapeCells);
                }
                else
                {
                    previewDrawer.Setup_Rect(source, settingsDict_rect);
                    previewDrawer.SetShapeCells(shapeCells);
                }
                SceneView.RepaintAll();
                UpdateFoldoutTitles();

                isRefreshingPreview = false;
            }
        }

        private void UpdateFoldoutTitles()
        {
            if (source.layoutGrid.cellLayout == GridLayout.CellLayout.Hexagon)
            {
                var list = new (Foldout f, string name, int count)[]
                {
                    (fullFoldOut,      "Full",      hexShapeCells.Full.Count),
                    (junction5FoldOut, "Junction5", hexShapeCells.Junction5.Count),
                    (junction4FoldOut, "Junction4", hexShapeCells.Junction4.Count),
                    (junction3FoldOut, "Junction3", hexShapeCells.Junction3.Count),
                    (edgeFoldOut,      "Edge",      hexShapeCells.Edge.Count),
                    (tipFoldOut,       "Tip",       hexShapeCells.Tip.Count),
                    (hexIsolateFoldOut,"Isolate",  hexShapeCells.Isolate.Count),
                };
                foreach (var (f, name, count) in list)
                {
                    f.text = $"{name} (Count:{count})";
                }
            }
            else
            {
                var list = new (Foldout f, string name, int count)[]
                {
                    (verticalEdgeFoldOut,   "VerticalEdge",   shapeCells.Vertical.Count),
                    (horizontalEdgeFoldOut, "HorizontalEdge", shapeCells.Horizontal.Count),
                    (crossFoldOut,          "Cross",     shapeCells.Cross.Count),
                    (tJunctionFoldOut,      "TJunction", shapeCells.TJunction.Count),
                    (cornerFoldOut,         "Corner",    shapeCells.Corner.Count),
                    (isolateFoldOut,        "Isolate",   shapeCells.Isolate.Count),
                };
                foreach (var (f, name, count) in list)
                {
                    f.text = $"{name} (Count:{count})";
                }
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

            foreach (var kv in settingsDict_rect)
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

            foreach (var kv in settingsDict_hex)
            {
                string name = kv.Key.ToString();
                var setting = kv.Value;
                EditorPrefs.SetInt(CreateKey($"Hex.{name}.Flags"), (int)setting.flags);
                EditorPrefs.SetInt(CreateKey($"Hex.{name}.Layer"), setting.layer);
                EditorPrefs.SetString(CreateKey($"Hex.{name}.Tag"), setting.tag);
                EditorPrefs.SetBool(CreateKey($"Hex.{name}.CanPreview"), setting.canPreview);
                EditorPrefs.SetString(CreateKey($"Hex.{name}.Color"),
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

            foreach (var kv in settingsDict_rect)
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

            foreach (var kv in settingsDict_hex)
            {
                var name    = kv.Key.ToString();
                var setting = kv.Value;
                setting.flags      = (ShapeFlags)EditorPrefs.GetInt(CreateKey($"Hex.{name}.Flags"), (int)setting.flags);
                setting.layer      = EditorPrefs.GetInt(CreateKey($"Hex.{name}.Layer"), setting.layer);
                setting.tag        = EditorPrefs.GetString(CreateKey($"Hex.{name}.Tag"), setting.tag);
                setting.canPreview = EditorPrefs.GetBool(CreateKey($"Hex.{name}.CanPreview"), setting.canPreview);
                string col = EditorPrefs.GetString(CreateKey($"Hex.{name}.Color"), ColorUtility.ToHtmlStringRGBA(setting.previewColor));
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

            foreach (ShapeType_Rect t in Enum.GetValues(typeof(ShapeType_Rect)))
            {
                string name = t.ToString();
                EditorPrefs.DeleteKey(CreateKey($"{name}.Flags"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Layer"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Tag"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.CanPreview"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Color"));
            }

            foreach (ShapeType_Hex t in Enum.GetValues(typeof(ShapeType_Hex)))
            {
                string name = t.ToString();
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Flags"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Layer"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Tag"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.CanPreview"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Color"));
            }

            settingsDict_rect      = CreateDefaultSettings_Rect();
            settingsDict_hex   = CreateDefaultSettings_Hex();
            source            = null;
            canMergeEdges     = false;
            canAttachCollider = false;
        }

        #endregion
    }
}
