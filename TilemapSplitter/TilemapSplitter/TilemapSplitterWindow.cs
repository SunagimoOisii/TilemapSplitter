using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEditor.UIElements;

public class TilemapSplitterWindow : EditorWindow
{
    private enum ClassificationOption { Vertical, Horizontal, Corner, Both, Independent, Ignore }

    // ���ރI�v�V����
    private ClassificationOption crossOption = ClassificationOption.Vertical;
    private ClassificationOption tOption = ClassificationOption.Vertical;
    private ClassificationOption cornerOption = ClassificationOption.Vertical;
    private ClassificationOption isolateOption = ClassificationOption.Vertical;
    private bool generateVerticalEdge = true;
    private bool generateHorizontalEdge = true;

    // ���C���[�ݒ�
    private int crossLayer;
    private int tLayer;
    private int cornerLayer;
    private int isolateLayer;
    private int verticalLayer;
    private int horizontalLayer;

    // �v���r���[�ݒ�
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

    // �v���r���[�p�^�C�����X�g
    private List<Vector3Int> previewCrossTiles = new List<Vector3Int>();
    private List<Vector3Int> previewTTiles = new List<Vector3Int>();
    private List<Vector3Int> previewCornerTiles = new List<Vector3Int>();
    private List<Vector3Int> previewIsolateTiles = new List<Vector3Int>();
    private List<Vector3Int> previewVertTiles = new List<Vector3Int>();
    private List<Vector3Int> previewHorTiles = new List<Vector3Int>();

    [MenuItem("�c�[��/�^�C���}�b�v�����E�B���h�E")]
    public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("�^�C���}�b�v����");

    // Scene�r���[�Ńv���r���[�`��o�^
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // UI Toolkit�G���g���|�C���g
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

        // �����Ώ� Tilemap �̑I��
        var originalField = new ObjectField("�� Tilemap") { objectType = typeof(Tilemap), value = original };
        container.Add(originalField);
        var helpBox = new HelpBox("�����Ώۂ�I�����Ă�������", HelpBoxMessageType.Info);
        container.Add(helpBox);
        helpBox.visible = (original == null);
        originalField.RegisterValueChangedCallback(evt =>
        {
            original = evt.newValue as Tilemap;
            helpBox.visible = (original == null);
        });

        // ��؂��
        AddSeparator(container);
        // �c�G�b�W�ݒ�
        var vToggle = new Toggle("�c�G�b�W�𐶐�����") { value = generateVerticalEdge };
        container.Add(vToggle);
        vToggle.RegisterValueChangedCallback(evt => generateVerticalEdge = evt.newValue);
        var vLayerField = new LayerField("�c�G�b�W ���C���[", verticalLayer);
        container.Add(vLayerField);
        vLayerField.RegisterValueChangedCallback(evt => verticalLayer = evt.newValue);

        // ���G�b�W�ݒ�
        var hToggle = new Toggle("���G�b�W�𐶐�����") { value = generateHorizontalEdge };
        container.Add(hToggle);
        hToggle.RegisterValueChangedCallback(evt => generateHorizontalEdge = evt.newValue);
        var hLayerField = new LayerField("���G�b�W ���C���[", horizontalLayer);
        container.Add(hLayerField);
        hLayerField.RegisterValueChangedCallback(evt => horizontalLayer = evt.newValue);

        // ��؂��
        AddSeparator(container);
        // �^�C�����ސݒ�
        CreateEnumWithLayer(container, "�����^�C���̈���", crossOption, val => crossOption = val, crossLayer, val => crossLayer = val, "�����^�C�� ���C���[");
        CreateEnumWithLayer(container, "T���^�C���̈���", tOption, val => tOption = val, tLayer, val => tLayer = val, "T���^�C�� ���C���[");
        CreateEnumWithLayer(container, "�p�^�C���̈���", cornerOption, val => cornerOption = val, cornerLayer, val => cornerLayer = val, "�p�^�C�� ���C���[");
        CreateEnumWithLayer(container, "�Ǘ��^�C���̈���", isolateOption, val => isolateOption = val, isolateLayer, val => isolateLayer = val, "�Ǘ��^�C�� ���C���[");

        // ��؂��
        AddSeparator(container);
        // �v���r���[�ݒ�
        container.Add(new Label("�v���r���[�ݒ�"));
        var pc = new Toggle("�����v���r���[") { value = previewCross };
        container.Add(pc);
        pc.RegisterValueChangedCallback(evt => { previewCross = evt.newValue; UpdatePreview(); });
        var cc = new ColorField("�����F") { value = colorCross };
        container.Add(cc);
        cc.RegisterValueChangedCallback(evt => colorCross = evt.newValue);

        var pt = new Toggle("T���v���r���[") { value = previewT };
        container.Add(pt);
        pt.RegisterValueChangedCallback(evt => { previewT = evt.newValue; UpdatePreview(); });
        var tc = new ColorField("T���F") { value = colorT };
        container.Add(tc);
        tc.RegisterValueChangedCallback(evt => colorT = evt.newValue);

        var pco = new Toggle("�p�v���r���[") { value = previewCorner };
        container.Add(pco);
        pco.RegisterValueChangedCallback(evt => { previewCorner = evt.newValue; UpdatePreview(); });
        var coco = new ColorField("�p�F") { value = colorCorner };
        container.Add(coco);
        coco.RegisterValueChangedCallback(evt => colorCorner = evt.newValue);

        var pi = new Toggle("�Ǘ��v���r���[") { value = previewIsolate };
        container.Add(pi);
        pi.RegisterValueChangedCallback(evt => { previewIsolate = evt.newValue; UpdatePreview(); });
        var ic = new ColorField("�Ǘ��F") { value = colorIsolate };
        container.Add(ic);
        ic.RegisterValueChangedCallback(evt => colorIsolate = evt.newValue);

        var pv = new Toggle("�c�G�b�W�v���r���[") { value = previewVertical };
        container.Add(pv);
        pv.RegisterValueChangedCallback(evt => { previewVertical = evt.newValue; UpdatePreview(); });
        var vc = new ColorField("�c�G�b�W�F") { value = colorVertical };
        container.Add(vc);
        vc.RegisterValueChangedCallback(evt => colorVertical = evt.newValue);

        var ph = new Toggle("���G�b�W�v���r���[") { value = previewHorizontal };
        container.Add(ph);
        ph.RegisterValueChangedCallback(evt => { previewHorizontal = evt.newValue; UpdatePreview(); });
        var hc = new ColorField("���G�b�W�F") { value = colorHorizontal };
        container.Add(hc);
        hc.RegisterValueChangedCallback(evt => colorHorizontal = evt.newValue);

        // ��؂��
        AddSeparator(container);
        // ���s�{�^��
        var splitButton = new Button(() =>
        {
            if (original == null)
            {
                EditorUtility.DisplayDialog("�G���[", "�� Tilemap ���ݒ肳��Ă��܂���B", "OK");
                return;
            }
            SplitTilemap();
        }) { text = "�������s" };
        splitButton.style.marginTop = 10;
        container.Add(splitButton);
    }

    // ���ތ��ʂ��v���r���[�p���X�g�ɐݒ�
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

    // ���ۂ̕�������
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

    // Scene�r���[�Ƀv���r���[�I�[�o�[���C�`��
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

    // �v���r���[�`��p���\�b�h
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

    // ��؂����ǉ�
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

    // EnumField�{LayerField�����w���p�[
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

    // ���ޔ���w���p�[
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

    // Tilemap�I�u�W�F�N�g����
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
