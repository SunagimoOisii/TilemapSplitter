using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEditor.UIElements;

public class TilemapSplitterWindow : EditorWindow
{
    private enum ClassificationOption { Vertical, Horizontal, Both, Independent, Ignore }

    private ClassificationOption crossOption = ClassificationOption.Vertical;
    private ClassificationOption tOption = ClassificationOption.Vertical;
    private ClassificationOption isolateOption = ClassificationOption.Vertical;
    private bool generateVerticalEdge = true;
    private bool generateHorizontalEdge = true;

    private int crossLayer;
    private int tLayer;
    private int isolateLayer;
    private int verticalLayer;
    private int horizontalLayer;

    private Tilemap original;

    [MenuItem("ツール/タイルマップ分割ウィンドウ")]
    public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("タイルマップ分割");

    public void CreateGUI()
    {
        // ルート要素とスクロールビュー設定
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

        // 縦エッジの生成設定
        var vToggle = new Toggle("縦エッジを生成する") { value = generateVerticalEdge };
        container.Add(vToggle);
        vToggle.RegisterValueChangedCallback(evt => generateVerticalEdge = evt.newValue);
        var vLayerField = new LayerField("縦エッジ レイヤー", verticalLayer);
        container.Add(vLayerField);
        vLayerField.RegisterValueChangedCallback(evt => verticalLayer = evt.newValue);

        // 横エッジの生成設定
        var hToggle = new Toggle("横エッジを生成する") { value = generateHorizontalEdge };
        container.Add(hToggle);
        hToggle.RegisterValueChangedCallback(evt => generateHorizontalEdge = evt.newValue);
        var hLayerField = new LayerField("横エッジ レイヤー", horizontalLayer);
        container.Add(hLayerField);
        hLayerField.RegisterValueChangedCallback(evt => horizontalLayer = evt.newValue);

        // 区切り線
        AddSeparator(container);

        // 交差タイルの設定
        CreateEnumWithLayer(container, "交差タイルの扱い", crossOption, val => crossOption = val, crossLayer, val => crossLayer = val, "交差タイル レイヤー");
        // T字タイルの設定
        CreateEnumWithLayer(container, "T字タイルの扱い", tOption, val => tOption = val, tLayer, val => tLayer = val, "T字タイル レイヤー");
        // 孤立タイルの設定
        CreateEnumWithLayer(container, "孤立タイルの扱い", isolateOption, val => isolateOption = val, isolateLayer, val => isolateLayer = val, "孤立タイル レイヤー");

        // 分割実行ボタン
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

    private void SplitTilemap()
    {
        var bounds = original.cellBounds;
        var positions = new List<Vector3Int>();
        foreach (var pos in bounds.allPositionsWithin)
            if (original.GetTile(pos) != null)
                positions.Add(pos);
        var tiles = new HashSet<Vector3Int>(positions);

        var crossTiles = new List<Vector3Int>();
        var tTiles = new List<Vector3Int>();
        var vertTiles = new List<Vector3Int>();
        var horTiles = new List<Vector3Int>();
        var isolateTiles = new List<Vector3Int>();

        foreach (var pos in positions)
        {
            bool up = tiles.Contains(pos + Vector3Int.up);
            bool down = tiles.Contains(pos + Vector3Int.down);
            bool left = tiles.Contains(pos + Vector3Int.left);
            bool right = tiles.Contains(pos + Vector3Int.right);
            int count = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);
            bool anyV = up || down;
            bool anyH = left || right;

            if (count == 4)
                ApplyClassification(pos, crossOption, crossTiles, vertTiles, horTiles);
            else if (count == 3)
                ApplyClassification(pos, tOption, tTiles, vertTiles, horTiles);
            else if (anyV && !anyH && generateVerticalEdge)
                vertTiles.Add(pos);
            else if (anyH && !anyV && generateHorizontalEdge)
                horTiles.Add(pos);
            else if (count == 0)
                ApplyClassification(pos, isolateOption, isolateTiles, vertTiles, horTiles);
        }

        CreateTiles(crossOption, "CrossTiles", crossTiles, crossLayer);
        CreateTiles(tOption, "TJunctionTiles", tTiles, tLayer);
        if (generateVerticalEdge) CreateTiles(ClassificationOption.Vertical, "VerticalEdge", vertTiles, verticalLayer);
        if (generateHorizontalEdge) CreateTiles(ClassificationOption.Horizontal, "HorizontalEdge", horTiles, horizontalLayer);
        CreateTiles(isolateOption, "IsolateTiles", isolateTiles, isolateLayer);
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

    private void CreateTiles(ClassificationOption opt, string name, List<Vector3Int> data, int layer)
    {
        if (data == null || data.Count == 0) return;
        if ((name == "CrossTiles" || name == "TJunctionTiles" || name == "IsolateTiles") && opt != ClassificationOption.Independent) return;
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
