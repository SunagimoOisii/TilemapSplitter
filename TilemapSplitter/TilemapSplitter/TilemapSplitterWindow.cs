using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Tilemaps;
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

    // プレビュー用タイルリスト
    private List<Vector3Int> previewCrossTiles = new List<Vector3Int>();
    private List<Vector3Int> previewTTiles = new List<Vector3Int>();
    private List<Vector3Int> previewCornerTiles = new List<Vector3Int>();
    private List<Vector3Int> previewIsolateTiles = new List<Vector3Int>();
    private List<Vector3Int> previewVertTiles = new List<Vector3Int>();
    private List<Vector3Int> previewHorTiles = new List<Vector3Int>();

    [MenuItem("ツール/タイルマップ分割ウィンドウ")]
    public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("タイルマップ分割");

    // Sceneビューでプレビュー描画登録
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // UI Toolkitエントリポイント
    public void CreateGUI()
    {
        var root = rootVisualElement;
        var scroll = new ScrollView();
        root.Add(scroll);
        var container = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                paddingLeft = 10,
                paddingTop = 10
            }
        };
        scroll.Add(container);

        // 分割対象 Tilemap の選択
        var originalField = new ObjectField("元 Tilemap") { objectType = typeof(Tilemap), value = original };
        container.Add(originalField);
        var helpBox = new HelpBox("分割対象を選択してください", HelpBoxMessageType.Info);
        container.Add(helpBox);
        helpBox.visible = (original == null);
        originalField.RegisterValueChangedCallback(evt =>
        {
            original = evt.newValue as Tilemap;
            helpBox.visible = (original == null);
        });

        // 区切り線
        AddSeparator(container);
        // 縦エッジ設定
        var vToggle = new Toggle("縦エッジを生成する") { value = generateVerticalEdge };
        container.Add(vToggle);
        vToggle.RegisterValueChangedCallback(evt => generateVerticalEdge = evt.newValue);
        var vLayerField = new LayerField("縦エッジ レイヤー", verticalLayer);
        container.Add(vLayerField);
        vLayerField.RegisterValueChangedCallback(evt => verticalLayer = evt.newValue);

        // 横エッジ設定
        var hToggle = new Toggle("横エッジを生成する") { value = generateHorizontalEdge };
        container.Add(hToggle);
        hToggle.RegisterValueChangedCallback(evt => generateHorizontalEdge = evt.newValue);
        var hLayerField = new LayerField("横エッジ レイヤー", horizontalLayer);
        container.Add(hLayerField);
        hLayerField.RegisterValueChangedCallback(evt => horizontalLayer = evt.newValue);

        // 区切り線
        AddSeparator(container);
        // タイル分類設定
        CreateEnumWithLayer(container, "交差タイルの扱い", crossOption, val => crossOption = val, crossLayer, val => crossLayer = val, "交差タイル レイヤー");
        CreateEnumWithLayer(container, "T字タイルの扱い", tOption, val => tOption = val, tLayer, val => tLayer = val, "T字タイル レイヤー");
        CreateEnumWithLayer(container, "角タイルの扱い", cornerOption, val => cornerOption = val, cornerLayer, val => cornerLayer = val, "角タイル レイヤー");
        CreateEnumWithLayer(container, "孤立タイルの扱い", isolateOption, val => isolateOption = val, isolateLayer, val => isolateLayer = val, "孤立タイル レイヤー");

        // 区切り線
        AddSeparator(container);
        // プレビュー設定
        container.Add(new Label("プレビュー設定"));
        var pc = new Toggle("交差プレビュー") { value = previewCross };
        container.Add(pc);
        pc.RegisterValueChangedCallback(evt => { previewCross = evt.newValue; UpdatePreview(); });
        var cc = new ColorField("交差色") { value = colorCross };
        container.Add(cc);
        cc.RegisterValueChangedCallback(evt => colorCross = evt.newValue);

        var pt = new Toggle("T字プレビュー") { value = previewT };
        container.Add(pt);
        pt.RegisterValueChangedCallback(evt => { previewT = evt.newValue; UpdatePreview(); });
        var tc = new ColorField("T字色") { value = colorT };
        container.Add(tc);
        tc.RegisterValueChangedCallback(evt => colorT = evt.newValue);

        var pco = new Toggle("角プレビュー") { value = previewCorner };
        container.Add(pco);
        pco.RegisterValueChangedCallback(evt => { previewCorner = evt.newValue; UpdatePreview(); });
        var coco = new ColorField("角色") { value = colorCorner };
        container.Add(coco);
        coco.RegisterValueChangedCallback(evt => colorCorner = evt.newValue);

        var pi = new Toggle("孤立プレビュー") { value = previewIsolate };
        container.Add(pi);
        pi.RegisterValueChangedCallback(evt => { previewIsolate = evt.newValue; UpdatePreview(); });
        var ic = new ColorField("孤立色") { value = colorIsolate };
        container.Add(ic);
        ic.RegisterValueChangedCallback(evt => colorIsolate = evt.newValue);

        var pv = new Toggle("縦エッジプレビュー") { value = previewVertical };
        container.Add(pv);
        pv.RegisterValueChangedCallback(evt => { previewVertical = evt.newValue; UpdatePreview(); });
        var vc = new ColorField("縦エッジ色") { value = colorVertical };
        container.Add(vc);
        vc.RegisterValueChangedCallback(evt => colorVertical = evt.newValue);

        var ph = new Toggle("横エッジプレビュー") { value = previewHorizontal };
        container.Add(ph);
        ph.RegisterValueChangedCallback(evt => { previewHorizontal = evt.newValue; UpdatePreview(); });
        var hc = new ColorField("横エッジ色") { value = colorHorizontal };
        container.Add(hc);
        hc.RegisterValueChangedCallback(evt => colorHorizontal = evt.newValue);

        // 区切り線
        AddSeparator(container);
        // 実行ボタン
        var splitButton = new Button(() =>
        {
            if (original == null)
            {
                EditorUtility.DisplayDialog("エラー", "元 Tilemap が設定されていません。", "OK");
                return;
            }
            SplitTilemap();
        }) { text = "分割実行" };
        splitButton.style.marginTop = 10;
        container.Add(splitButton);
    }

    // 分類結果をプレビュー用リストに設定
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

    // 実際の分割処理
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

    // Sceneビューにプレビューオーバーレイ描画
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

    // プレビュー描画用メソッド
    private void DrawPreviewList(List<Vector3Int> list, Color col)
    {
        Handles.color = new Color(col.r, col.g, col.b, 0.4f);
        float cellSize = original.cellSize.x;
        foreach (var pos in list)
        {
            Vector3 worldPos = original.CellToWorld(pos) + new Vector3(cellSize / 2, cellSize / 2);
            Rect rect = new Rect(worldPos.x - cellSize/2, worldPos.y - cellSize/2, cellSize, cellSize);
            Handles.DrawSolidRectangleWithOutline(rect, Handles.color, Color.clear);
        }
    }

    // 区切り線を追加
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

    // EnumField＋LayerField生成ヘルパー
    private static void CreateEnumWithLayer(VisualElement parent, string label, ClassificationOption currentOption, System.Action<ClassificationOption> setOption, int currentLayer, System.Action<int> setLayer, string layerLabel)
    {
        var enumField = new EnumField(label, currentOption);
        parent.Add(enumField);
        var layerContainer = new VisualElement();
        var layerField = new LayerField(layerLabel, currentLayer);
        layerContainer.Add(layerField);
        layerContainer.visible = (currentOption == ClassificationOption.Independent);
        parent.Add(layerContainer);

        enumField.RegisterValueChangedCallback(evt =>
        {
            var option = (ClassificationOption)evt.newValue;
            setOption(option);
            layerContainer.visible = (option == ClassificationOption.Independent);
        });
        layerField.RegisterValueChangedCallback(evt => setLayer(evt.newValue));
    }

    // 分類判定ヘルパー
    private static void ApplyClassification(Vector3Int pos, ClassificationOption opt, List<Vector3Int> independent, List<Vector3Int> vList, List<Vector3Int> hList)
    {
        switch (opt)
        {
            case ClassificationOption.Vertical:
                vList?.Add(pos);
                break;
            case ClassificationOption.Horizontal:
                hList?.Add(pos);
                break;
            case ClassificationOption.Corner:
                independent?.Add(pos);
                break;
            case ClassificationOption.Both:
                vList?.Add(pos);
                hList?.Add(pos);
                break;
            case ClassificationOption.Independent:
                independent?.Add(pos);
                break;
            case ClassificationOption.Ignore:
                break;
        }
    }

    // Tilemapオブジェクト生成
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
