using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

[Flags]
public enum ClassificationOption
{
    VerticalEdge   = 1 << 0,
    HorizontalEdge = 1 << 1,
    Independent    = 1 << 2,
}

public enum SettingType
{
    VerticalEdge = 0,
    HorizontalEdge,
    Cross,
    TJunction,
    Corner,
    Isolate,
}

[Serializable]
public class ClassificationSetting
{
    public ClassificationOption option;
    public int layer;
    public string tag = "Untagged";
    public bool canPreview = true;
    public Color color = Color.white;
}

[CreateAssetMenu(fileName = "TilemapSplitterSettings", menuName = "TilemapSplitter/Settings")]
public class TilemapSplitterSettings : ScriptableObject
{
    public ClassificationSetting[] settings = new ClassificationSetting[6];
    public bool canMergeEdges;

    public static TilemapSplitterSettings CreateDefault()
    {
        var asset = CreateInstance<TilemapSplitterSettings>();
        asset.settings = new ClassificationSetting[6]
        {
            new() { option = ClassificationOption.VerticalEdge,   color = Color.green   },
            new() { option = ClassificationOption.HorizontalEdge, color = Color.yellow  },
            new() { option = ClassificationOption.Independent,    color = Color.red     },
            new() { option = ClassificationOption.Independent,    color = Color.blue    },
            new() { option = ClassificationOption.Independent,    color = Color.cyan    },
            new() { option = ClassificationOption.Independent,    color = Color.magenta },
        };
        return asset;
    }
}

public class TilemapSplitterWindow : EditorWindow

{
    private const string SettingsAssetPath = "Assets/TilemapSplitter/TilemapSplitterSettings.asset";
    private TilemapSplitterSettings settingsAsset;
    private ClassificationSetting[] settings => settingsAsset.settings;
    private ClassificationSetting GetSetting(SettingType t) => settings[(int)t];


    //各生成タイル数表示のために使用
    private Foldout verticalEdgeFO;
    private Foldout horizontalEdgeFO;
    private Foldout crossFO;
    private Foldout tJunctionFO;
    private Foldout cornerFO;
    private Foldout isolateFO;

    private Tilemap original;
    private readonly List<Vector3Int> previewVertTiles    = new();
    private readonly List<Vector3Int> previewHorTiles     = new();
    private readonly List<Vector3Int> previewCrossTiles   = new();
    private readonly List<Vector3Int> previewTTiles       = new();
    private readonly List<Vector3Int> previewCornerTiles  = new();
    private readonly List<Vector3Int> previewIsolateTiles = new();

    //文字列リテラルの回避に使用
    private const string VerticalEdgeName   = "VerticalEdge";
    private const string HorizontalEdgeName = "HorizontalEdge";
    private const string CrossTileName      = "CrossTiles";
    private const string TJunctionTileName  = "TJunctionTiles";
    private const string CornerTileName     = "CornerTiles";
    private const string IsolateTileName    = "IsolateTiles";

    private bool canMergeEdges = false; //縦, 横エッジを同一オブジェクトにまとめるかどうか

    [MenuItem("Tools/TilemapSplitter")]
    public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("Split Tilemap");

    public void CreateGUI()
    {
        var root = rootVisualElement;

        //ScrollView, Container 設定
        var scroll    = new ScrollView();
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft   = 10;
        container.style.paddingRight  = 10;
        root.Add(scroll);
        scroll.Add(container);

        //Split Target Field 設定
        var helpBox = new HelpBox("Select the subject of the division", HelpBoxMessageType.Info);
        var originalField = new ObjectField("Split Tilemap");
        helpBox.visible = (original == null);
        originalField.objectType = typeof(Tilemap);
        originalField.value      = original;
        originalField.RegisterValueChangedCallback(evt =>
        {
            original = evt.newValue as Tilemap;
            helpBox.visible = (original == null);
            UpdatePreview();
        });
        container.Add(originalField);
        container.Add(helpBox);

        AddSeparator(container);

        //縦, 横エッジ統合チェックボックス
        var mergeToggle = new Toggle("Merge VerticalEdge, HorizontalEdge") { value = canMergeEdges };
        var mergeHB = new HelpBox("When merging, VerticalEdge settings take precedence",
            HelpBoxMessageType.Info);
        mergeToggle.RegisterValueChangedCallback(evt =>
        {
            canMergeEdges = evt.newValue;
            MarkDirty();
        });
        container.Add(mergeToggle);
        container.Add(mergeHB);

        //縦, 横エッジ Foldout 設定
        verticalEdgeFO   = CreateEdgeFoldout(container, VerticalEdgeName,   SettingType.VerticalEdge);
        AddSeparator(container);
        horizontalEdgeFO = CreateEdgeFoldout(container, HorizontalEdgeName, SettingType.HorizontalEdge);
        AddSeparator(container);

        //各タイルの設定項目
        var infos = new (string title, SettingType type)[]
        {
            ("Cross",      SettingType.Cross),
            ("T-Junction", SettingType.TJunction),
            ("Corner",     SettingType.Corner),
            ("Isolate",    SettingType.Isolate),
        };
        foreach (var info in infos)
        {
            var fold = CreateFoldout(container, info.title, GetSetting(info.type));
            switch (info.type)
            {
                case SettingType.Cross:      crossFO      = fold; break;
                case SettingType.TJunction:  tJunctionFO  = fold; break;
                case SettingType.Corner:     cornerFO     = fold; break;
                case SettingType.Isolate:    isolateFO    = fold; break;
            }
            AddSeparator(container);
        }

        //Execute Button
        var splitButton = new Button(() =>
        {
            if (original == null)
            {
                EditorUtility.DisplayDialog("Error", "The split target isn't set", "OK");
                return;
            }
            SplitTilemap();
        });
        splitButton.text = "Execute Splitting";
        splitButton.style.marginTop = 10;
        container.Add(splitButton);
    }

    private static void AddSeparator(VisualElement parent)
    {
        var separator = new VisualElement();
        separator.style.borderBottomWidth = 1;
        separator.style.borderBottomColor = Color.gray;
        separator.style.marginTop         = 5;
        separator.style.marginBottom      = 5;

        parent.Add(separator);
    }

    private (Toggle previewToggle, ColorField colorField) AddCommonFields(Foldout fold, 
        ClassificationSetting setting)
    {
        var layerField = new LayerField("Layer", setting.layer);
        layerField.RegisterValueChangedCallback(evt =>
        {
            setting.layer = evt.newValue;
            MarkDirty();
        });
        fold.Add(layerField);

        var tagField = new TagField("Tag", setting.tag);
        tagField.RegisterValueChangedCallback(evt =>
        {
            setting.tag = evt.newValue;
            MarkDirty();
        });
        fold.Add(tagField);

        var previewToggle = new Toggle("Preview") { value = setting.canPreview };
        previewToggle.RegisterValueChangedCallback(evt =>
        {
            setting.canPreview = evt.newValue;
            UpdatePreview();
            MarkDirty();
        });
        fold.Add(previewToggle);

        var colField = new ColorField("Preview Color") { value = setting.color };
        colField.RegisterValueChangedCallback(evt =>
        {
            setting.color = evt.newValue;
            MarkDirty();
        });
        fold.Add(colField);

        return (previewToggle, colField);
    }

    //縦, 横エッジ専用の Foldout 作成
    private Foldout CreateEdgeFoldout(VisualElement parent, string title, SettingType type)
    {
        var setting = GetSetting(type);
        var fold = new Foldout { text = title };
        fold.style.unityFontStyleAndWeight = FontStyle.Bold;

        AddCommonFields(fold, setting);

        parent.Add(fold);
        return fold;
    }

    private Foldout CreateFoldout(VisualElement parent, string title, ClassificationSetting setting)
    {
        var fold = new Foldout { text = title };
        fold.style.unityFontStyleAndWeight = FontStyle.Bold;

        var enumField = new EnumFlagsField("Which obj to add to", setting.option);
        fold.Add(enumField);

        var (previewToggle, colField) = AddCommonFields(fold, setting);

        enumField.RegisterValueChangedCallback(evt =>
        {
            setting.option = (ClassificationOption)evt.newValue;
            UpdatePreview();
            UpdateUI(setting, fold, previewToggle, colField);
            MarkDirty();
        });

        UpdateUI(setting, fold, previewToggle, colField);
        parent.Add(fold);
        return fold;
    }

    void UpdateUI(ClassificationSetting setting, Foldout fold,
        Toggle previewToggle, ColorField colField)
    {
        var opt = setting.option;
        
        var exist = fold.Q<HelpBox>();
        if (exist != null) fold.Remove(exist);

        //HelpBox 作成
        string msg = null;
        string helpV = "The canPreview complies with VerticalEdge settings.";
        string helpH = "The canPreview complies with HorizontalEdge settings.";
        if      (opt.HasFlag(ClassificationOption.VerticalEdge))   msg = helpV;
        else if (opt.HasFlag(ClassificationOption.HorizontalEdge)) msg = helpH;

        if (string.IsNullOrEmpty(msg) == false)
        {
            fold.Add(new HelpBox(msg, HelpBoxMessageType.Info));
        }

        bool isVisible = opt.HasFlag(ClassificationOption.Independent);
        previewToggle.visible = isVisible;
        colField.visible      = isVisible;
    }

    private void SplitTilemap()
    {
        UpdatePreview();
        UpdateFoldoutTitles();

        CreateTiles(GetSetting(SettingType.Cross).option, CrossTileName, previewCrossTiles,
            GetSetting(SettingType.Cross).layer, GetSetting(SettingType.Cross).tag);
        CreateTiles(GetSetting(SettingType.TJunction).option, TJunctionTileName, previewTTiles,
            GetSetting(SettingType.TJunction).layer, GetSetting(SettingType.TJunction).tag);
        CreateTiles(GetSetting(SettingType.Corner).option, CornerTileName, previewCornerTiles,
            GetSetting(SettingType.Corner).layer, GetSetting(SettingType.Corner).tag);

        if (canMergeEdges) //縦横エッジ統合
        {
            var merged = new List<Vector3Int>(previewVertTiles);
            merged.AddRange(previewHorTiles);
            CreateTiles(ClassificationOption.Independent, "EdgeTiles", merged,
                GetSetting(SettingType.VerticalEdge).layer,
                GetSetting(SettingType.VerticalEdge).tag);
        }
        else
        {
            CreateTiles(GetSetting(SettingType.VerticalEdge).option, VerticalEdgeName,   previewVertTiles,
                GetSetting(SettingType.VerticalEdge).layer, GetSetting(SettingType.VerticalEdge).tag);
            CreateTiles(GetSetting(SettingType.HorizontalEdge).option, HorizontalEdgeName, previewHorTiles,
                GetSetting(SettingType.HorizontalEdge).layer, GetSetting(SettingType.HorizontalEdge).tag);
        }

        CreateTiles(GetSetting(SettingType.Isolate).option, IsolateTileName, previewIsolateTiles,
            GetSetting(SettingType.Isolate).layer, GetSetting(SettingType.Isolate).tag);
    }

    private void UpdateFoldoutTitles()
    {
        if (verticalEdgeFO   != null) verticalEdgeFO.text   = $"{VerticalEdgeName} (Count:{previewVertTiles.Count})";
        if (horizontalEdgeFO != null) horizontalEdgeFO.text = $"{HorizontalEdgeName} (Count:{previewHorTiles.Count})";
        if (crossFO          != null) crossFO.text          = $"{CrossTileName} (Count:{previewCrossTiles.Count})";
        if (tJunctionFO      != null) tJunctionFO.text      = $"{TJunctionTileName} (Count:{previewTTiles.Count})";
        if (cornerFO         != null) cornerFO.text         = $"{CornerTileName} (Count:{previewCornerTiles.Count})";
        if (isolateFO        != null) isolateFO.text        = $"{IsolateTileName} (Count:{previewIsolateTiles.Count})";
    }

    private void CreateTiles(ClassificationOption opt, string name, List<Vector3Int> data, 
        int layer, string tag)
    {
        if (data == null ||
            data.Count == 0) return;

        bool requiresIndependent = (name == CrossTileName)  || (name == TJunctionTileName) ||
                                   (name == CornerTileName) || (name == IsolateTileName);
        if (requiresIndependent &&
            opt.HasFlag(ClassificationOption.Independent) == false) return;

        var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(original.transform.parent, false);
        obj.layer = layer;
        obj.tag   = tag;

        var renderer = obj.GetComponent<TilemapRenderer>();
        if (original.TryGetComponent<TilemapRenderer>(out var oriRenderer))
        {
            renderer.sortingLayerID = oriRenderer.sortingLayerID;
            renderer.sortingOrder   = oriRenderer.sortingOrder;
        }
        else Debug.LogWarning("Since TilemapRenderer is not attached to the split target," +
            "the TilemapRenderer of the generated object was generated with the default settings.");

        var tm = obj.GetComponent<Tilemap>();
        foreach(var p in data) tm.SetTile(p, original.GetTile(p));

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
    }

    private void ClassifyTileNeighbors(Vector3Int pos, HashSet<Vector3Int> tiles)
    {
        //各方向隣接チェック
        bool up    = tiles.Contains(pos + Vector3Int.up);
        bool down  = tiles.Contains(pos + Vector3Int.down);
        bool left  = tiles.Contains(pos + Vector3Int.left);
        bool right = tiles.Contains(pos + Vector3Int.right);
        bool anyV  = up   || down;
        bool anyH  = left || right;
        int count  = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

        if (count == 4) //交差タイル
        {
            ApplyClassification(pos, settings[2].option, previewCrossTiles,
                previewVertTiles, previewHorTiles);
        }
        else if (count == 3) //T字タイル
        {
            ApplyClassification(pos, settings[3].option, previewTTiles,
                previewVertTiles, previewHorTiles);
        }
        else if (count == 2 && //角タイル
                 anyV &&
                 anyH)
        {
            ApplyClassification(pos, settings[4].option, previewCornerTiles,
                previewVertTiles, previewHorTiles);
        }
        else if (anyV && //縦エッジ
                 anyH == false)
        {
            previewVertTiles.Add(pos);
        }
        else if (anyH && //横エッジ
                 anyV == false)
        {
            previewHorTiles.Add(pos);
        }
        else if (count == 0) //孤立タイル
        {
            ApplyClassification(pos, settings[5].option, previewIsolateTiles,
                previewVertTiles, previewHorTiles);
        }
    }

    private static void ApplyClassification(Vector3Int pos, ClassificationOption opt,
        List<Vector3Int> indep, List<Vector3Int> vList, List<Vector3Int> hList)
    {
        if (opt.HasFlag(ClassificationOption.VerticalEdge))    vList?.Add(pos);
        if (opt.HasFlag(ClassificationOption.HorizontalEdge))  hList?.Add(pos);
        if (opt.HasFlag(ClassificationOption.Independent))     indep?.Add(pos);
    }

    private void LoadSettings()
    {
        settingsAsset = AssetDatabase.LoadAssetAtPath<TilemapSplitterSettings>(SettingsAssetPath);
        if (settingsAsset == null)
        {
            settingsAsset = TilemapSplitterSettings.CreateDefault();
            var dir = System.IO.Path.GetDirectoryName(SettingsAssetPath);
            if (System.IO.Directory.Exists(dir) == false)
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            AssetDatabase.CreateAsset(settingsAsset, SettingsAssetPath);
            AssetDatabase.SaveAssets();
        }

        canMergeEdges = settingsAsset.canMergeEdges;
    }

    private void SaveSettings()
    {
        if (settingsAsset == null) return;
        settingsAsset.canMergeEdges = canMergeEdges;
        EditorUtility.SetDirty(settingsAsset);
        AssetDatabase.SaveAssets();
    }

    private void MarkDirty() => EditorUtility.SetDirty(settingsAsset);

    #region プレビュー機能

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadSettings();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SaveSettings();
    }

    private void OnSceneGUI(SceneView sv)
    {
        if (original == null) return;

        if (settings[0].canPreview) DrawPreviewList(previewVertTiles,    settings[0].color);
        if (settings[1].canPreview) DrawPreviewList(previewHorTiles,     settings[1].color);
        if (settings[2].canPreview) DrawPreviewList(previewCrossTiles,   settings[2].color);
        if (settings[3].canPreview) DrawPreviewList(previewTTiles,       settings[3].color);
        if (settings[4].canPreview) DrawPreviewList(previewCornerTiles,  settings[4].color);
        if (settings[5].canPreview) DrawPreviewList(previewIsolateTiles, settings[5].color);
    }

    private void DrawPreviewList(List<Vector3Int> list, Color col)
    {
        Handles.color = new Color(col.r, col.g, col.b, 0.4f);
        var cellSize = original.cellSize;
        foreach(var pos in list)
        {
            Vector3 worldPos = original.CellToWorld(pos) + new Vector3(cellSize.x / 2f, cellSize.y / 2f);
            Rect rect = new(
                worldPos.x - cellSize.x / 2f,
                worldPos.y - cellSize.y / 2f,
                cellSize.x,
                cellSize.y);
            Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
        }
    }

    private void UpdatePreview()
    {
        if (original == null) return;

        var positions = new List<Vector3Int>();
        foreach (var pos in original.cellBounds.allPositionsWithin)
        {
            if (original.GetTile(pos) != null) positions.Add(pos);
        }

        previewCrossTiles.Clear();
        previewTTiles.Clear();
        previewCornerTiles.Clear();
        previewIsolateTiles.Clear();
        previewVertTiles.Clear();
        previewHorTiles.Clear();

        var tiles = new HashSet<Vector3Int>(positions);
        foreach (var pos in positions)
        {
            ClassifyTileNeighbors(pos, tiles);
        }

        SceneView.RepaintAll();
        UpdateFoldoutTitles();
    }

    #endregion
}
