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
        private Dictionary<HexShapeType, ShapeSetting> hexSettingsDict = CreateDefaultHexSettings();

        private static Dictionary<ShapeType, ShapeSetting> CreateDefaultSettings() => new()
        {
            [ShapeType.VerticalEdge]   = new() { flags = ShapeFlags.VerticalEdge,   previewColor = Color.green  },
            [ShapeType.HorizontalEdge] = new() { flags = ShapeFlags.HorizontalEdge, previewColor = Color.yellow },
            [ShapeType.Cross]          = new() { flags = ShapeFlags.Independent,    previewColor = Color.red    },
            [ShapeType.TJunction]      = new() { flags = ShapeFlags.Independent,    previewColor = Color.blue   },
            [ShapeType.Corner]         = new() { flags = ShapeFlags.Independent,    previewColor = Color.cyan   },
            [ShapeType.Isolate]        = new() { flags = ShapeFlags.Independent,    previewColor = Color.magenta },
        };

        private static Dictionary<HexShapeType, ShapeSetting> CreateDefaultHexSettings() => new()
        {
            [HexShapeType.Full]     = new() { flags = ShapeFlags.Independent, previewColor = Color.red    },
            [HexShapeType.Junction] = new() { flags = ShapeFlags.Independent, previewColor = Color.blue   },
            [HexShapeType.Corner]   = new() { flags = ShapeFlags.Independent, previewColor = Color.cyan   },
            [HexShapeType.Edge]     = new() { flags = ShapeFlags.Independent, previewColor = Color.green  },
            [HexShapeType.Tip]      = new() { flags = ShapeFlags.Independent, previewColor = Color.yellow },
            [HexShapeType.Isolate]  = new() { flags = ShapeFlags.Independent, previewColor = Color.magenta },
        };

        private Foldout verticalEdgeFoldOut;
        private Foldout horizontalEdgeFoldOut;
        private Foldout crossFoldOut;
        private Foldout tJunctionFoldOut;
        private Foldout cornerFoldOut;
        private Foldout isolateFoldOut;
        private Foldout fullFoldOut;
        private Foldout junctionFoldOut;
        private Foldout hexCornerFoldOut;
        private Foldout edgeFoldOut;
        private Foldout tipFoldOut;
        private Foldout hexIsolateFoldOut;

        private Tilemap source;
        private ShapeCells shapeCells = new();
        private HexShapeCells hexShapeCells = new();
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
            if (source != null && source.layoutGrid.cellLayout == GridLayout.CellLayout.Hexagon)
                CreateHexShapeFoldouts(c);
            else
                CreateShapeFoldouts(c);
            CreateExecuteButton(c);
            if (source != null && source.layoutGrid.cellLayout == GridLayout.CellLayout.Hexagon)
                previewDrawer.Setup(source, hexSettingsDict);
            else
                previewDrawer.Setup(source, settingsDict);
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
                source    = evt.newValue as Tilemap;
                hp.visible = (source == null);
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
            var attachT = new Toggle("Attach Colliders");
            attachT.value = canAttachCollider;
            attachT.RegisterValueChangedCallback(evt => canAttachCollider = evt.newValue);
            container.Add(attachT);
        }

        private void CreateMergeEdgeToggle(VisualElement container)
        {
            var mergeT  = new Toggle("Merge VerticalEdge, HorizontalEdge");
            var mergeHB = new HelpBox("When merging, VerticalEdge shapeSettings take precedence",
                HelpBoxMessageType.Info);
            mergeT.value = canMergeEdges;
            mergeT.RegisterValueChangedCallback(evt => canMergeEdges = evt.newValue);
            container.Add(mergeT);
            container.Add(mergeHB);
        }

        private void CreateShapeFoldouts(VisualElement container)
        {
            var infos = new (ShapeType type, string title)[]
            {
                (ShapeType.VerticalEdge,   "VerticalEdge"),
                (ShapeType.HorizontalEdge, "HorizontalEdge"),
                (ShapeType.Cross,          "Cross"),
                (ShapeType.TJunction,      "T-Junction"),
                (ShapeType.Corner,         "Corner"),
                (ShapeType.Isolate,        "Isolate")
            };
            foreach (var info in infos)
            {
                var fold = CreateFoldout(container, info.type, info.title);
                switch (info.type)
                {
                    case ShapeType.VerticalEdge:   verticalEdgeFoldOut   = fold; break;
                    case ShapeType.HorizontalEdge: horizontalEdgeFoldOut = fold; break;
                    case ShapeType.Cross:          crossFoldOut          = fold; break;
                    case ShapeType.TJunction:      tJunctionFoldOut      = fold; break;
                    case ShapeType.Corner:         cornerFoldOut         = fold; break;
                    case ShapeType.Isolate:        isolateFoldOut        = fold; break;
                }
                AddHorizontalSeparator(container);
            }
        }

        private void CreateHexShapeFoldouts(VisualElement container)
        {
            var infos = new (HexShapeType type, string title)[]
            {
                (HexShapeType.Full,     "Full"),
                (HexShapeType.Junction, "Junction"),
                (HexShapeType.Corner,   "Corner"),
                (HexShapeType.Edge,     "Edge"),
                (HexShapeType.Tip,      "Tip"),
                (HexShapeType.Isolate,  "Isolate")
            };
            foreach (var info in infos)
            {
                var fold = CreateFoldout(container, info.type, info.title);
                switch (info.type)
                {
                    case HexShapeType.Full:     fullFoldOut     = fold; break;
                    case HexShapeType.Junction: junctionFoldOut = fold; break;
                    case HexShapeType.Corner:   hexCornerFoldOut = fold; break;
                    case HexShapeType.Edge:     edgeFoldOut     = fold; break;
                    case HexShapeType.Tip:      tipFoldOut      = fold; break;
                    case HexShapeType.Isolate:  hexIsolateFoldOut = fold; break;
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

        private Foldout CreateFoldout(VisualElement parentContainer, ShapeType type, string title)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = settingsDict[type];
            if (type == ShapeType.VerticalEdge ||
                type == ShapeType.HorizontalEdge)
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

        private Foldout CreateFoldout(VisualElement parentContainer, HexShapeType type, string title)
        {
            var fold = new Foldout();
            fold.text                          = title;
            fold.style.unityFontStyleAndWeight = FontStyle.Bold;

            var setting = hexSettingsDict[type];
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
            bool isHex = source.layoutGrid.cellLayout == GridLayout.CellLayout.Hexagon;
            IEnumerator e;
            if (isHex)
            {
                hexShapeCells = new HexShapeCells();
                e = TileShapeClassifier.ClassifyCoroutine(source, hexSettingsDict, hexShapeCells);
            }
            else
            {
                shapeCells = new ShapeCells();
                e = TileShapeClassifier.ClassifyCoroutine(source, settingsDict, shapeCells);
            }

            while (e.MoveNext()) yield return null;

            if (isHex)
            {
                TilemapCreator.GenerateSplitTilemaps(source, hexShapeCells, hexSettingsDict, canAttachCollider);
            }
            else
            {
                TilemapCreator.GenerateSplitTilemaps(source, shapeCells, settingsDict,
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
                    hexShapeCells = new HexShapeCells();
                    e = TileShapeClassifier.ClassifyCoroutine(source, hexSettingsDict, hexShapeCells);
                }
                else
                {
                    shapeCells = new ShapeCells();
                    e = TileShapeClassifier.ClassifyCoroutine(source, settingsDict, shapeCells);
                }
                while (e.MoveNext())
                {
                    yield return null;
                }

                if (isHex)
                {
                    previewDrawer.Setup(source, hexSettingsDict);
                    previewDrawer.SetShapeCells(hexShapeCells);
                }
                else
                {
                    previewDrawer.Setup(source, settingsDict);
                    previewDrawer.SetShapeCells(shapeCells);
                }
                SceneView.RepaintAll();
                UpdateFoldoutTitles();

                isRefreshingPreview = false;
            }
        }

        private void UpdateFoldoutTitles()
        {
            if (source != null && source.layoutGrid.cellLayout == GridLayout.CellLayout.Hexagon)
            {
                var list = new (Foldout f, string name, int count)[]
                {
                    (fullFoldOut,     "Full",     hexShapeCells.FullCells.Count),
                    (junctionFoldOut, "Junction", hexShapeCells.JunctionCells.Count),
                    (hexCornerFoldOut,"Corner",   hexShapeCells.CornerCells.Count),
                    (edgeFoldOut,     "Edge",     hexShapeCells.EdgeCells.Count),
                    (tipFoldOut,      "Tip",      hexShapeCells.TipCells.Count),
                    (hexIsolateFoldOut,"Isolate", hexShapeCells.IsolateCells.Count),
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

            foreach (var kv in hexSettingsDict)
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

            foreach (var kv in hexSettingsDict)
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

            foreach (ShapeType t in Enum.GetValues(typeof(ShapeType)))
            {
                string name = t.ToString();
                EditorPrefs.DeleteKey(CreateKey($"{name}.Flags"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Layer"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Tag"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.CanPreview"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Color"));
            }

            foreach (HexShapeType t in Enum.GetValues(typeof(HexShapeType)))
            {
                string name = t.ToString();
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Flags"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Layer"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Tag"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.CanPreview"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Color"));
            }

            settingsDict      = CreateDefaultSettings();
            hexSettingsDict   = CreateDefaultHexSettings();
            source            = null;
            canMergeEdges     = false;
            canAttachCollider = false;
        }

        #endregion
    }
}
