#nullable disable

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEditor.UIElements;

public class TilemapSplitterWindow : EditorWindow
{
    private enum ClassificationOption { Vertical, Horizontal, Corner, Both, Independent, Ignore }

    private struct ClassificationSetting
    {
        public ClassificationOption option;
        public int layer;
        public bool preview;
        public Color color;
    }

    private readonly ClassificationSetting[] settings = new ClassificationSetting[6]
    {
        new() { option = ClassificationOption.Vertical,     color = Color.red    }, // Cross
        new() { option = ClassificationOption.Vertical,     color = Color.blue   }, // T-Junction
        new() { option = ClassificationOption.Vertical,     color = Color.cyan   }, // Corner
        new() { option = ClassificationOption.Vertical,     color = Color.magenta}, // Isolate
        new() { option = ClassificationOption.Independent,  preview = true, color = Color.green  }, // Vertical Edge
        new() { option = ClassificationOption.Independent,  preview = true, color = Color.yellow }  // Horizontal Edge
    };

    private Tilemap original;
    private readonly List<Vector3Int> previewCrossTiles   = new();
    private readonly List<Vector3Int> previewTTiles       = new();
    private readonly List<Vector3Int> previewCornerTiles  = new();
    private readonly List<Vector3Int> previewIsolateTiles = new();
    private readonly List<Vector3Int> previewVertTiles    = new();
    private readonly List<Vector3Int> previewHorTiles     = new();

    [MenuItem("Tools/TimemapSplitter")]
    public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("Split Tilemap");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    public void CreateGUI()
    {
        var root = rootVisualElement;
        var scroll = new ScrollView();
        root.Add(scroll);
        var container = new VisualElement { style = { flexDirection = FlexDirection.Column, paddingLeft = 10, paddingTop = 10 } };
        scroll.Add(container);

        var originalField = new ObjectField("元 Tilemap") { objectType = typeof(Tilemap), value = original };
        container.Add(originalField);
        var helpBox = new HelpBox("分割対象を選択してください", HelpBoxMessageType.Info);
        container.Add(helpBox);
        helpBox.visible = (original == null);
        originalField.RegisterValueChangedCallback(evt => { original = evt.newValue as Tilemap; helpBox.visible = (original == null); UpdatePreview(); });

        AddSeparator(container);

        var infos = new (string title, int index, string helpV, string helpH, string helpI)[]
        {
            ("交差タイル",   0, "プレビューは縦タイルの設定で操作可能", "プレビューは横タイルの設定で操作可能", "Ignore では何も生成されないのでプレビューもない"),
            ("T字タイル",   1, "プレビューは縦タイルの設定で操作可能", "プレビューは横タイルの設定で操作可能", "Ignore では何も生成されないのでプレビューもない"),
            ("角タイル",     2, "プレビューは縦タイルの設定で操作可能", "プレビューは横タイルの設定で操作可能", "Ignore では何も生成されないのでプレビューもない"),
            ("孤立タイル",   3, "プレビューは縦タイルの設定で操作可能", "プレビューは横タイルの設定で操作可能", "Ignore では何も生成されないのでプレビューもない"),
            ("縦エッジ",     4, null, null, null),
            ("横エッジ",     5, null, null, null)
        };

        foreach (var info in infos)
        {
            int idx = info.index;
            CreateFoldout(container, info.title,
                () => settings[idx].option,
                v => settings[idx].option = v,
                () => settings[idx].layer,
                v => settings[idx].layer = v,
                () => settings[idx].preview,
                v => settings[idx].preview = v,
                () => settings[idx].color,
                v => settings[idx].color = v,
                info.helpV, info.helpH, info.helpI);
        }

        AddSeparator(container);

        var splitButton = new Button(() => { if (original == null) { EditorUtility.DisplayDialog("エラー", "元 Tilemap が設定されていません。", "OK"); return; } SplitTilemap(); }) { text = "分割実行" };
        splitButton.style.marginTop = 10;
        container.Add(splitButton);
    }

    private void UpdatePreview()
    {
        if (original == null) return;
        var bounds = original.cellBounds;
        var positions = new List<Vector3Int>();
        foreach (var pos in bounds.allPositionsWithin)
            if (original.GetTile(pos) != null)
                positions.Add(pos);
        var tiles = new HashSet<Vector3Int>(positions);

        ClearPreviewLists();
        foreach (var pos in positions)
            ClassifyTileNeighbors(pos, tiles);
        SceneView.RepaintAll();
    }

    private void ClearPreviewLists()
    {
        previewCrossTiles.Clear();
        previewTTiles.Clear();
        previewCornerTiles.Clear();
        previewIsolateTiles.Clear();
        previewVertTiles.Clear();
        previewHorTiles.Clear();
    }

    private void ClassifyTileNeighbors(Vector3Int pos, HashSet<Vector3Int> tiles)
    {
        bool up = tiles.Contains(pos + Vector3Int.up);
        bool down = tiles.Contains(pos + Vector3Int.down);
        bool left = tiles.Contains(pos + Vector3Int.left);
        bool right = tiles.Contains(pos + Vector3Int.right);
        int count = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);
        bool anyV = up || down;
        bool anyH = left || right;

        if (count == 4)
            ApplyClassification(pos, settings[0].option, previewCrossTiles, previewVertTiles, previewHorTiles);
        else if (count == 3)
            ApplyClassification(pos, settings[1].option, previewTTiles, previewVertTiles, previewHorTiles);
        else if (count == 2 && anyV && anyH)
            ApplyClassification(pos, settings[2].option, previewCornerTiles, previewVertTiles, previewHorTiles);
        else if (anyV && !anyH)
            previewVertTiles.Add(pos);
        else if (anyH && !anyV)
            previewHorTiles.Add(pos);
        else if (count == 0)
            ApplyClassification(pos, settings[3].option, previewIsolateTiles, previewVertTiles, previewHorTiles);
    }

    private void SplitTilemap()
    {
        UpdatePreview();
        CreateTiles(settings[0].option, "CrossTiles", previewCrossTiles, settings[0].layer);
        CreateTiles(settings[1].option, "TJunctionTiles", previewTTiles, settings[1].layer);
        CreateTiles(settings[2].option, "CornerTiles", previewCornerTiles, settings[2].layer);
        if (settings[4].option == ClassificationOption.Independent)
            CreateTiles(ClassificationOption.Vertical, "VerticalEdge", previewVertTiles, settings[4].layer);
        if (settings[5].option == ClassificationOption.Independent)
            CreateTiles(ClassificationOption.Horizontal, "HorizontalEdge", previewHorTiles, settings[5].layer);
        CreateTiles(settings[3].option, "IsolateTiles", previewIsolateTiles, settings[3].layer);
    }

    private void OnSceneGUI(SceneView sv)
    {
        if (original == null) return;
        if (settings[0].preview) DrawPreviewList(previewCrossTiles, settings[0].color);
        if (settings[1].preview) DrawPreviewList(previewTTiles, settings[1].color);
        if (settings[2].preview) DrawPreviewList(previewCornerTiles, settings[2].color);
        if (settings[3].preview) DrawPreviewList(previewIsolateTiles, settings[3].color);
        if (settings[4].preview) DrawPreviewList(previewVertTiles, settings[4].color);
        if (settings[5].preview) DrawPreviewList(previewHorTiles, settings[5].color);
    }

    private void DrawPreviewList(List<Vector3Int> list, Color col)
    {
        Handles.color = new Color(col.r, col.g, col.b, 0.4f);
        float cellSize = original.cellSize.x;
        foreach (var pos in list)
        {
            Vector3 worldPos = original.CellToWorld(pos) + new Vector3(cellSize/2, cellSize/2);
            Rect rect = new(worldPos.x - cellSize/2, worldPos.y - cellSize/2, cellSize, cellSize);
            Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
        }
    }

    private static void AddSeparator(VisualElement parent)
    {
        var separator = new VisualElement
        {
            style =
            {
                borderBottomWidth = 1,
                borderBottomColor = Color.gray,
                marginTop = 5,
                marginBottom = 5
            }
        };
        parent.Add(separator);
    }

    private void CreateFoldout(VisualElement parent, string title,
        System.Func<ClassificationOption> getOption, System.Action<ClassificationOption> setOption,
        System.Func<int> getLayer, System.Action<int> setLayer,
        System.Func<bool> getPreview, System.Action<bool> setPreview,
        System.Func<Color> getColor, System.Action<Color> setColor,
        string helpVert, string helpHorz, string helpIgnore)
    {
        var fold = new Foldout { text = title };
        fold.style.unityFontStyleAndWeight = FontStyle.Bold;
        var enumField = new EnumField("生成設定", getOption());
        fold.Add(enumField);
        if (getLayer != null)
        {
            var layerField = new LayerField("レイヤー", getLayer());
            fold.Add(layerField);
            layerField.RegisterValueChangedCallback(evt => setLayer(evt.newValue));
        }
        Toggle previewToggle = null;
        ColorField colField = null;
        if (getPreview != null && getColor != null)
        {
            previewToggle = new Toggle("プレビュー") { value = getPreview() };
            previewToggle.RegisterValueChangedCallback(evt => { setPreview(evt.newValue); UpdatePreview(); });
            fold.Add(previewToggle);
            colField = new ColorField("色") { value = getColor() };
            colField.RegisterValueChangedCallback(evt => setColor(evt.newValue));
            fold.Add(colField);
        }
        System.Action updateUI = () =>
        {
            var opt = getOption();
            ApplyHelp(fold, opt, helpVert, helpHorz, helpIgnore);
            bool show = opt == ClassificationOption.Independent;
            if (previewToggle != null) previewToggle.visible = show;
            if (colField != null) colField.visible = show;
        };
        enumField.RegisterValueChangedCallback(evt => { setOption((ClassificationOption)evt.newValue); UpdatePreview(); updateUI(); });
        updateUI();
        parent.Add(fold);
    }

    private void ApplyHelp(Foldout fold, ClassificationOption opt, string helpVert, string helpHorz, string helpIgnore)
    {
        var exist = fold.Q<HelpBox>();
        if (exist != null) fold.Remove(exist);
        string msg = null;
        if (opt == ClassificationOption.Vertical) msg = helpVert;
        else if (opt == ClassificationOption.Horizontal) msg = helpHorz;
        else if (opt == ClassificationOption.Ignore) msg = helpIgnore;
        if (!string.IsNullOrEmpty(msg)) fold.Add(new HelpBox(msg, HelpBoxMessageType.Info));
    }

    private static void ApplyClassification(Vector3Int pos, ClassificationOption opt, List<Vector3Int> indep, List<Vector3Int> vList, List<Vector3Int> hList)
    {
        switch (opt)
        {
            case ClassificationOption.Vertical: vList?.Add(pos); break;
            case ClassificationOption.Horizontal: hList?.Add(pos); break;
            case ClassificationOption.Corner: indep?.Add(pos); break;
            case ClassificationOption.Both: vList?.Add(pos); hList?.Add(pos); break;
            case ClassificationOption.Independent: indep?.Add(pos); break;
            case ClassificationOption.Ignore: break;
        }
    }

    private void CreateTiles(ClassificationOption opt, string name, List<Vector3Int> data, int layer)
    {
        if (data == null || data.Count == 0) return;
        if ((name == "CrossTiles" || name == "TJunctionTiles" || name == "CornerTiles" || name == "IsolateTiles") && opt != ClassificationOption.Independent) return;
        var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(original.transform.parent, false);
        obj.layer = layer;
        var renderer = obj.GetComponent<TilemapRenderer>();
        var orig = original.GetComponent<TilemapRenderer>();
        renderer.sortingLayerID = orig.sortingLayerID;
        renderer.sortingOrder = orig.sortingOrder;
        var tm = obj.GetComponent<Tilemap>();
        foreach (var p in data) tm.SetTile(p, original.GetTile(p));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
    }
}
