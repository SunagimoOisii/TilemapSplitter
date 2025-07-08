using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Tilemaps;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;

public class TilemapSplitterWindow : EditorWindow
{
    private enum ClassificationOption { Vertical, Horizontal, Corner, Both, Independent, Ignore }

    // 分類オプション
    private ClassificationOption crossOption = ClassificationOption.Vertical;
    private ClassificationOption tOption = ClassificationOption.Vertical;
    private ClassificationOption cornerOption = ClassificationOption.Vertical;
    private ClassificationOption isolateOption = ClassificationOption.Vertical;
    private bool generateVerticalEdge = true;
    private bool generateHorizontalEdge = true;

    // レイヤー設定
    private int crossLayer;
    private int tLayer;
    private int cornerLayer;
    private int isolateLayer;
    private int verticalLayer;
    private int horizontalLayer;

    // プレビュー設定
    private bool previewCross;
    private bool previewT;
    private bool previewCorner;
    private bool previewIsolate;
    private bool previewVertical;
    private bool previewHorizontal;
    private Color colorCross = Color.red;
    private Color colorT = Color.blue;
    private Color colorCorner = Color.cyan;
    private Color colorIsolate = Color.magenta;
    private Color colorVertical = Color.green;
    private Color colorHorizontal = Color.yellow;

    private Tilemap original;
    private List<Vector3Int> previewCrossTiles = new List<Vector3Int>();
    private List<Vector3Int> previewTTiles = new List<Vector3Int>();
    private List<Vector3Int> previewCornerTiles = new List<Vector3Int>();
    private List<Vector3Int> previewIsolateTiles = new List<Vector3Int>();
    private List<Vector3Int> previewVertTiles = new List<Vector3Int>();
    private List<Vector3Int> previewHorTiles = new List<Vector3Int>();

    [MenuItem("ツール/タイルマップ分割ウィンドウ")]
    public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("タイルマップ分割");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    // UI Toolkitエントリポイント
    public void CreateGUI()
    {
        var root = rootVisualElement;
        var scroll = new ScrollView();
        root.Add(scroll);
        var container = new VisualElement { style = { flexDirection = FlexDirection.Column, paddingLeft = 10, paddingTop = 10 } };
        scroll.Add(container);

        // 元 Tilemap 選択
        var originalField = new ObjectField("元 Tilemap") { objectType = typeof(Tilemap), value = original };
        container.Add(originalField);
        var helpBox = new HelpBox("分割対象を選択してください", HelpBoxMessageType.Info);
        container.Add(helpBox);
        helpBox.visible = (original == null);
        originalField.RegisterValueChangedCallback(evt => { original = evt.newValue as Tilemap; helpBox.visible = (original == null); UpdatePreview(); });

        AddSeparator(container);
        CreateFoldout(container, "交差タイル",
            () => crossOption, v => { crossOption = v; UpdatePreview(); },
            () => crossLayer, v => crossLayer = v,
            () => previewCross, v => { previewCross = v; UpdatePreview(); },
            () => colorCross, v => colorCross = v,
            "プレビューは縦タイルの設定で操作可能",
            "プレビューは横タイルの設定で操作可能",
            "Ignore では何も生成されないのでプレビューもない");
        CreateFoldout(container, "T字タイル",
            () => tOption, v => { tOption = v; UpdatePreview(); },
            () => tLayer, v => tLayer = v,
            () => previewT, v => { previewT = v; UpdatePreview(); },
            () => colorT, v => colorT = v,
            "プレビューは縦タイルの設定で操作可能",
            "プレビューは横タイルの設定で操作可能",
            "Ignore では何も生成されないのでプレビューもない");
        CreateFoldout(container, "角タイル",
            () => cornerOption, v => { cornerOption = v; UpdatePreview(); },
            () => cornerLayer, v => cornerLayer = v,
            () => previewCorner, v => { previewCorner = v; UpdatePreview(); },
            () => colorCorner, v => colorCorner = v,
            "プレビューは縦タイルの設定で操作可能",
            "プレビューは横タイルの設定で操作可能",
            "Ignore では何も生成されないのでプレビューもない");
        CreateFoldout(container, "孤立タイル",
            () => isolateOption, v => { isolateOption = v; UpdatePreview(); },
            () => isolateLayer, v => isolateLayer = v,
            () => previewIsolate, v => { previewIsolate = v; UpdatePreview(); },
            () => colorIsolate, v => colorIsolate = v,
            "プレビューは縦タイルの設定で操作可能",
            "プレビューは横タイルの設定で操作可能",
            "Ignore では何も生成されないのでプレビューもない");

        AddSeparator(container);
        CreateFoldout(container, "縦エッジ",
            () => generateVerticalEdge ? ClassificationOption.Independent : ClassificationOption.Ignore,
            v => generateVerticalEdge = (v == ClassificationOption.Independent),
            () => verticalLayer, v => verticalLayer = v,
            () => previewVertical, v => { previewVertical = v; UpdatePreview(); },
            () => colorVertical, v => colorVertical = v,
            null, null, null);
        CreateFoldout(container, "横エッジ",
            () => generateHorizontalEdge ? ClassificationOption.Independent : ClassificationOption.Ignore,
            v => generateHorizontalEdge = (v == ClassificationOption.Independent),
            () => horizontalLayer, v => horizontalLayer = v,
            () => previewHorizontal, v => { previewHorizontal = v; UpdatePreview(); },
            () => colorHorizontal, v => colorHorizontal = v,
            null, null, null);

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

        previewCrossTiles.Clear(); previewTTiles.Clear(); previewCornerTiles.Clear(); previewIsolateTiles.Clear(); previewVertTiles.Clear(); previewHorTiles.Clear();
        foreach (var pos in positions)
        {
            bool up = tiles.Contains(pos + Vector3Int.up);
            bool down = tiles.Contains(pos + Vector3Int.down);
            bool left = tiles.Contains(pos + Vector3Int.left);
            bool right = tiles.Contains(pos + Vector3Int.right);
            int count = (up?1:0)+(down?1:0)+(left?1:0)+(right?1:0);
            bool anyV = up||down, anyH = left||right;
            if (count == 4) ApplyClassification(pos, crossOption, previewCrossTiles, previewVertTiles, previewHorTiles);
            else if (count == 3) ApplyClassification(pos, tOption, previewTTiles, previewVertTiles, previewHorTiles);
            else if (count == 2 && anyV && anyH) ApplyClassification(pos, cornerOption, previewCornerTiles, previewVertTiles, previewHorTiles);
            else if (anyV && !anyH) previewVertTiles.Add(pos);
            else if (anyH && !anyV) previewHorTiles.Add(pos);
            else if (count == 0) ApplyClassification(pos, isolateOption, previewIsolateTiles, previewVertTiles, previewHorTiles);
        }
        SceneView.RepaintAll();
    }

    private void SplitTilemap()
    {
        UpdatePreview();
        CreateTiles(crossOption, "CrossTiles", previewCrossTiles, crossLayer);
        CreateTiles(tOption, "TJunctionTiles", previewTTiles, tLayer);
        CreateTiles(cornerOption, "CornerTiles", previewCornerTiles, cornerLayer);
        if (generateVerticalEdge) CreateTiles(ClassificationOption.Vertical, "VerticalEdge", previewVertTiles, verticalLayer);
        if (generateHorizontalEdge) CreateTiles(ClassificationOption.Horizontal, "HorizontalEdge", previewHorTiles, horizontalLayer);
        CreateTiles(isolateOption, "IsolateTiles", previewIsolateTiles, isolateLayer);
    }

    private void OnSceneGUI(SceneView sv)
    {
        if (original == null) return;
        if (previewCross) DrawPreviewList(previewCrossTiles, colorCross);
        if (previewT) DrawPreviewList(previewTTiles, colorT);
        if (previewCorner) DrawPreviewList(previewCornerTiles, colorCorner);
        if (previewIsolate) DrawPreviewList(previewIsolateTiles, colorIsolate);
        if (previewVertical) DrawPreviewList(previewVertTiles, colorVertical);
        if (previewHorizontal) DrawPreviewList(previewHorTiles, colorHorizontal);
    }

    private void DrawPreviewList(List<Vector3Int> list, Color col)
    {
        Handles.color = new Color(col.r, col.g, col.b, 0.4f);
        float cellSize = original.cellSize.x;
        foreach (var pos in list)
        {
            Vector3 worldPos = original.CellToWorld(pos) + new Vector3(cellSize/2, cellSize/2);
            Rect rect = new Rect(worldPos.x - cellSize/2, worldPos.y - cellSize/2, cellSize, cellSize);
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
        Func<ClassificationOption> getOption, Action<ClassificationOption> setOption,
        Func<int> getLayer, Action<int> setLayer,
        Func<bool> getPreview, Action<bool> setPreview,
        Func<Color> getColor, Action<Color> setColor,
        string helpVert, string helpHorz, string helpIgnore)
    {
        var fold = new Foldout { text = title };
        fold.style.unityFontStyleAndWeight = FontStyle.Bold;
        var enumField = new EnumField("生成設定", getOption());
        fold.Add(enumField);
        enumField.RegisterValueChangedCallback(evt =>
        {
            var opt = (ClassificationOption)evt.newValue;
            setOption(opt);
            UpdatePreview();
            ApplyHelp(fold, opt, helpVert, helpHorz, helpIgnore);
            // プレビューUI 再表示
            var prevToggle = fold.Query<Toggle>().First();
var colField = fold.Query<ColorField>().First();
            bool show = opt == ClassificationOption.Independent;
            if (prevToggle != null) prevToggle.visible = show;
            if (colField != null) colField.visible = show;
        });
        if (getLayer != null)
        {
            var layerField = new LayerField("レイヤー", getLayer());
            fold.Add(layerField);
            layerField.RegisterValueChangedCallback(evt => setLayer(evt.newValue));
        }
        var opt = getOption();
        ApplyHelp(fold, opt, helpVert, helpHorz, helpIgnore);
        // プレビューUI
        if (getPreview != null && getColor != null)
        {
            var toggle = new Toggle("プレビュー") { value = getPreview(), visible = (opt == ClassificationOption.Independent) };
            toggle.RegisterValueChangedCallback(evt => { setPreview(evt.newValue); UpdatePreview(); });
            fold.Add(toggle);
            var colorField = new ColorField("色") { value = getColor(), visible = (opt == ClassificationOption.Independent) };
            colorField.RegisterValueChangedCallback(evt => setColor(evt.newValue));
            fold.Add(colorField);
        }
        parent.Add(fold);
    }

    private void ApplyHelp(Foldout fold, ClassificationOption opt, string helpVert, string helpHorz, string helpIgnore)
    {
        var existing = fold.Q<HelpBox>(); if (existing != null) fold.Remove(existing);
        string msg = null;
        if (opt == ClassificationOption.Vertical) msg = helpVert;
        else if (opt == ClassificationOption.Horizontal) msg = helpHorz;
        else if (opt == ClassificationOption.Ignore) msg = helpIgnore;
        if (!string.IsNullOrEmpty(msg)) fold.Add(new HelpBox(msg, HelpBoxMessageType.Info));
    }

    private static void ApplyClassification(Vector3Int pos, ClassificationOption opt, List<Vector3Int> independent, List<Vector3Int> vList, List<Vector3Int> hList)
    {
        switch (opt)
        {
            case ClassificationOption.Vertical: vList?.Add(pos); break;
            case ClassificationOption.Horizontal: hList?.Add(pos); break;
            case ClassificationOption.Corner: independent?.Add(pos); break;
            case ClassificationOption.Both: vList?.Add(pos); hList?.Add(pos); break;
            case ClassificationOption.Independent: independent?.Add(pos); break;
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
