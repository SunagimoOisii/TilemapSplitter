#nullable disable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class TilemapSplitterWindow : EditorWindow
{
    [Flags]
    private enum ClassificationOption
    {
        VerticalEdge    = 1 << 0,  // 1
        HorizontalEdge  = 1 << 1,  // 2
        Independent     = 1 << 2,  // 4
    }

    private struct ClassificationSetting
    {
        public ClassificationOption option;
        public int layer;
        public string tag;
        public bool preview;
        public Color color;
    }

    private readonly ClassificationSetting[] settings = new ClassificationSetting[6]
    {
        new() { option =ClassificationOption.VerticalEdge,  preview = true, color = Color.green  }, //Vertical Edge
        new() { option =ClassificationOption.HorizontalEdge,preview = true, color = Color.yellow }, //Horizontal Edge
        new() { option = ClassificationOption.Independent, color = Color.red     }, //Cross
        new() { option = ClassificationOption.Independent, color = Color.blue    }, //T-Junction
        new() { option = ClassificationOption.Independent, color = Color.cyan    }, //Corner
        new() { option = ClassificationOption.Independent, color = Color.magenta }  //Isolate
    };

    private Tilemap original;
    private readonly List<Vector3Int> previewCrossTiles   = new();
    private readonly List<Vector3Int> previewTTiles       = new();
    private readonly List<Vector3Int> previewCornerTiles  = new();
    private readonly List<Vector3Int> previewIsolateTiles = new();
    private readonly List<Vector3Int> previewVertTiles    = new();
    private readonly List<Vector3Int> previewHorTiles     = new();

    //縦, 横エッジを同一オブジェクトにまとめるかどうか
    private bool mergeEdges = false;

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
        var mergeToggle = new Toggle("Merge VerticalEdge, HorizontalEdge") { value = mergeEdges };
        mergeToggle.RegisterValueChangedCallback(evt => mergeEdges = evt.newValue);
        container.Add(mergeToggle);

        //縦, 横エッジ設定
        CreateEdgeFoldout(container, "VerticalEdge Edge", 0);
        CreateEdgeFoldout(container, "HorizontalEdge Edge", 1);

        AddSeparator(container);

        //各タイルの設定項目
        var infos = new (string title, int index)[]
        {
            ("Cross",      2),
            ("T-Junction", 3),
            ("Corner",     4),
            ("Isolate",    5),
        };
        foreach (var info in infos)
        {
            int idx = info.index;
            CreateFoldout(container, info.title, //各要素の get, set 時の処理を入れる
                () => settings[idx].option,  v => settings[idx].option  = v,  //ClassificationOption
                () => settings[idx].layer,   v => settings[idx].layer   = v,  //Layer
                v => settings[idx].tag     = v,                               //Tag
                () => settings[idx].preview, v => settings[idx].preview = v,  //Preview
                () => settings[idx].color,   v => settings[idx].color   = v); //Color
        }

        AddSeparator(container);

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

    //縦, 横エッジ専用の Foldout 作成
    private void CreateEdgeFoldout(VisualElement parent, string title, int idx)
    {
        var fold = new Foldout { text = title };
        fold.style.unityFontStyleAndWeight = FontStyle.Bold;

        // レイヤー設定
        var layerField = new LayerField("Layer", settings[idx].layer);
        layerField.RegisterValueChangedCallback(evt => settings[idx].layer = evt.newValue);
        fold.Add(layerField);

        //タグ設定
        var tagField = new TagField("Tag", "Untagged");
        tagField.RegisterValueChangedCallback(evt => settings[idx].tag = evt.newValue);
        fold.Add(tagField);

        // プレビュー設定
        var previewToggle = new Toggle("Preview");
        previewToggle.value = settings[idx].preview;
        previewToggle.RegisterValueChangedCallback(evt => 
        { 
            settings[idx].preview = evt.newValue;
            UpdatePreview();
        });
        fold.Add(previewToggle);

        // プレビュー色設定
        var colField = new ColorField("Preview Color");
        colField.value = settings[idx].color;
        colField.RegisterValueChangedCallback(evt => settings[idx].color = evt.newValue);
        fold.Add(colField);

        parent.Add(fold);
    }

    private void CreateFoldout(VisualElement parent, string title,
        Func<ClassificationOption> getOption, Action<ClassificationOption> setOption,
        Func<int> getLayer, Action<int> setLayer,
        Action<string> setTag,
        Func<bool> getPreview, Action<bool> setPreview,
        Func<Color> getColor, Action<Color> setColor)
    {
        var fold = new Foldout { text = title };
        fold.style.unityFontStyleAndWeight = FontStyle.Bold;

        var enumField = new EnumFlagsField("Which obj to add to", getOption());
        fold.Add(enumField);

        var layerField = new LayerField("Layer", getLayer());
        layerField.RegisterValueChangedCallback(evt => setLayer(evt.newValue));
        fold.Add(layerField);

        //タグ設定
        var tagField = new TagField("Tag", "Untagged");
        tagField.RegisterValueChangedCallback(evt => setTag(evt.newValue));
        fold.Add(tagField);

        var previewToggle = new Toggle("Preview") { value = getPreview() };
        previewToggle.RegisterValueChangedCallback(evt => 
        {
            setPreview(evt.newValue);
            UpdatePreview();
        });
        fold.Add(previewToggle);

        var colField = new ColorField("Preview Color") { value = getColor() };
        colField.RegisterValueChangedCallback(evt => setColor(evt.newValue));
        fold.Add(colField);

        enumField.RegisterValueChangedCallback(evt => 
        {
            setOption((ClassificationOption)evt.newValue);
            UpdatePreview();
            UpdateUI(getOption, fold, previewToggle, colField);
        });

        UpdateUI(getOption, fold, previewToggle, colField);
        parent.Add(fold);
    }

    void UpdateUI(Func<ClassificationOption> getOption, Foldout fold,
        Toggle previewToggle, ColorField colField)
    {
        var opt = getOption();
        
        var exist = fold.Q<HelpBox>();
        if (exist != null) fold.Remove(exist);

        //HelpBox 作成
        string msg = null;
        string helpV = "The preview complies with VerticalEdge settings.";
        string helpH = "The preview complies with HorizontalEdge settings.";
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
        CreateTiles(settings[2].option, "CrossTiles", previewCrossTiles,
            settings[2].layer, settings[2].tag);
        CreateTiles(settings[3].option, "TJunctionTiles", previewTTiles,
            settings[3].layer, settings[3].tag);
        CreateTiles(settings[4].option, "CornerTiles",    previewCornerTiles,
            settings[4].layer, settings[4].tag);

        if (mergeEdges) //縦横エッジ統合
        {
            var merged = new List<Vector3Int>(previewVertTiles);
            merged.AddRange(previewHorTiles);
            CreateTiles(ClassificationOption.Independent, "EdgeTiles", merged, 
                settings[0].layer, settings[0].tag);
        }
        else
        {
            CreateTiles(settings[0].option, "VerticalEdge",   previewVertTiles, 
                settings[0].layer, settings[0].tag);
            CreateTiles(settings[1].option, "HorizontalEdge", previewHorTiles,  
                settings[1].layer, settings[1].tag);
        }

        CreateTiles(settings[5].option, "IsolateTiles", previewIsolateTiles, 
            settings[5].layer, settings[5].tag);
    }

    private void CreateTiles(ClassificationOption opt, string name, List<Vector3Int> data, 
        int layer, string tag)
    {
        if (data == null ||
            data.Count == 0) return;

        bool requiresIndependent = (name == "CrossTiles") || (name == "TJunctionTiles") ||
                                   (name == "CornerTiles") || (name == "IsolateTiles");
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

    private static void ApplyClassification(Vector3Int pos, ClassificationOption opt,
        List<Vector3Int> indep, List<Vector3Int> vList, List<Vector3Int> hList)
    {
        if (opt.HasFlag(ClassificationOption.VerticalEdge))    vList?.Add(pos);
        if (opt.HasFlag(ClassificationOption.HorizontalEdge))  hList?.Add(pos);
        if (opt.HasFlag(ClassificationOption.Independent))     indep?.Add(pos);
    }

    #region 分割プレビュー機能

    private void OnEnable()  => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        if (original == null) return;

        if (settings[0].preview) DrawPreviewList(previewVertTiles,    settings[0].color);
        if (settings[1].preview) DrawPreviewList(previewHorTiles,     settings[1].color);
        if (settings[2].preview) DrawPreviewList(previewCrossTiles,   settings[2].color);
        if (settings[3].preview) DrawPreviewList(previewTTiles,       settings[3].color);
        if (settings[4].preview) DrawPreviewList(previewCornerTiles,  settings[4].color);
        if (settings[5].preview) DrawPreviewList(previewIsolateTiles, settings[5].color);
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

    #endregion
}
