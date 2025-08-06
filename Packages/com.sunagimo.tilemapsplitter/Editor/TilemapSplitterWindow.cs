namespace TilemapSplitter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using Unity.EditorCoroutines.Editor;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    /// <summary>
    /// Editor window that splits a tilemap into multiple parts based on cell layout
    /// </summary>
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

        private Tilemap source;

        private ICellLayoutStrategy layoutStrategy;

        private readonly TilemapPreviewDrawer previewDrawer = new();

        private bool canMergeEdges       = false;
        private bool canAttachCollider   = false;
        private bool isRefreshingPreview = false;

        // Progress bar that supports cancellation
        private sealed class CancelableProgressBar : IProgress<float>
        {
            private readonly string title;
            public bool IsCancelled { get; private set; }

            public CancelableProgressBar(string title)
            {
                this.title = title;
            }

            public void Report(float value)
            {
                IsCancelled = EditorUtility.DisplayCancelableProgressBar(
                    title, $"分類中... {Mathf.RoundToInt(value * 100)}%", value);
            }

            public void Clear()
            {
                EditorUtility.ClearProgressBar();
            }
        }

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

            if (source == null || source.gameObject.activeInHierarchy == false) return;

            var layout = source.layoutGrid.cellLayout;
            layoutStrategy = (layout == GridLayout.CellLayout.Hexagon)
                ? new CellLayoutStrategy_Hex(settingsDict_hex, RefreshPreview)
                : new CellLayoutStrategy_Rect(settingsDict_rect, RefreshPreview);

            layoutStrategy.CreateMergeEdgeToggle(c, () => canMergeEdges, v => canMergeEdges = v);
            layoutStrategy.CreateShapeFoldouts(c);

            CreateExecuteButton(c);

            layoutStrategy.SetupPreview(source, previewDrawer);
            RefreshPreview();
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

        private void CreateExecuteButton(VisualElement container)
        {
            var splitB = new Button(() =>
            {
                if (source == null)
                {
                    EditorUtility.DisplayDialog("Error", "The split target isn't set", "OK");
                    return;
                }
                EditorCoroutineUtility.StartCoroutine(SplitCoroutine(), this);
            });
            splitB.text            = "Execute Splitting";
            splitB.style.marginTop = 10;
            container.Add(splitB);
        }

        public static void AddHorizontalSeparator(VisualElement parentContainer)
        {
            var separator = new VisualElement();
            separator.style.borderBottomWidth = 1;
            separator.style.borderBottomColor = Color.gray;
            separator.style.marginTop         = 5;
            separator.style.marginBottom      = 5;
            parentContainer.Add(separator);
        }

        private IEnumerator SplitCoroutine()
        {
            var progress = new CancelableProgressBar("分類");
            IEnumerator<bool> e = layoutStrategy.Classify(source, progress, () => progress.IsCancelled);
            while (e.MoveNext())
            {
                if (e.Current)
                {
                    progress.Clear();
                    yield break;
                }
                yield return null;
            }
            progress.Clear();
            layoutStrategy.GenerateSplitTilemaps(source, canMergeEdges, canAttachCollider);
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (source == null || isRefreshingPreview || layoutStrategy == null) return;
            EditorCoroutineUtility.StartCoroutine(RefreshPreviewCoroutine(), this);

            IEnumerator RefreshPreviewCoroutine()
            {
                isRefreshingPreview = true;

                var progress = new CancelableProgressBar("分類");
                IEnumerator<bool> e = layoutStrategy.Classify(source, progress, () => progress.IsCancelled);
                while (e.MoveNext())
                {
                    if (e.Current)
                    {
                        progress.Clear();
                        isRefreshingPreview = false;
                        yield break;
                    }
                    yield return null;
                }

                progress.Clear();
                layoutStrategy.SetupPreview(source, previewDrawer);
                layoutStrategy.SetShapeCellsToPreview(previewDrawer);
                SceneView.RepaintAll();
                layoutStrategy.UpdateFoldoutTitles();

                isRefreshingPreview = false;
            }
        }

        #region Saving and Loading Split Settings using EditorPrefs

        private void SavePrefs()
        {
            if (source != null)
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(source);
                EditorPrefs.SetString(CreateKey("SourceId"), id.ToString());
            }
            else
            {
                EditorPrefs.DeleteKey(CreateKey("SourceId"));
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
            if (EditorPrefs.HasKey(CreateKey("SourceId")))
            {
                var idStr = EditorPrefs.GetString(CreateKey("SourceId"));
                if (GlobalObjectId.TryParse(idStr, out var id))
                {
                    source = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Tilemap;
                }
            }
            else if (EditorPrefs.HasKey(CreateKey("SourcePath")))
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
                string col = EditorPrefs.GetString(CreateKey($"{name}.Color"),
                    ColorUtility.ToHtmlStringRGBA(setting.previewColor));
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
                string col = EditorPrefs.GetString(CreateKey($"Hex.{name}.Color"),
                    ColorUtility.ToHtmlStringRGBA(setting.previewColor));
                if (ColorUtility.TryParseHtmlString("#" + col, out var c))
                {
                    setting.previewColor = c;
                }
            }
        }

        private void ResetPrefs()
        {
            EditorPrefs.DeleteKey(CreateKey("SourceId"));
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
