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
        VerticalEdge    = 1 << 0,
        HorizontalEdge  = 1 << 1,
        Independent     = 1 << 2,
    }

    private enum SettingType
    {
        VerticalEdge = 0,
        HorizontalEdge,
        Cross,
        TJunction,
        Corner,
        Isolate,
    }

    private class ClassificationSetting
    {
        public ClassificationOption option;
        public int    layer;
        public string tag        = "Untagged";
        public bool   canPreview = true;
        public Color  color;
    }

    private readonly ClassificationSetting[] settings = new ClassificationSetting[6]
    {
        new() { option = ClassificationOption.VerticalEdge,  color = Color.green  },  //Vertical Edge
        new() { option = ClassificationOption.HorizontalEdge,color = Color.yellow },  //Horizontal Edge
        new() { option = ClassificationOption.Independent,   color = Color.red },     //Cross
        new() { option = ClassificationOption.Independent,   color = Color.blue },    //T-Junction
        new() { option = ClassificationOption.Independent,   color = Color.cyan },    //Corner
        new() { option = ClassificationOption.Independent,   color = Color.magenta }  //Isolate
    };
    private ClassificationSetting GetSetting(SettingType t) => settings[(int)t];

    //各生成タイル数表示のために使用
    private Foldout verticalEdgeFO;
    private Foldout horizontalEdgeFO;
    private Foldout crossFO;
    private Foldout tJunctionFO;
    private Foldout cornerFO;
    private Foldout isolateFO;

    private Tilemap original;
    private TilemapClassificationResult classification = new();

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
        mergeToggle.RegisterValueChangedCallback(evt => canMergeEdges = evt.newValue);
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
                case SettingType.Cross:     crossFO     = fold; break;
                case SettingType.TJunction: tJunctionFO = fold; break;
                case SettingType.Corner:    cornerFO    = fold; break;
                case SettingType.Isolate:   isolateFO   = fold; break;
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
        layerField.RegisterValueChangedCallback(evt => setting.layer = evt.newValue);
        fold.Add(layerField);

        var tagField = new TagField("Tag", setting.tag);
        tagField.RegisterValueChangedCallback(evt => setting.tag = evt.newValue);
        fold.Add(tagField);

        var previewToggle = new Toggle("Preview") { value = setting.canPreview };
        previewToggle.RegisterValueChangedCallback(evt =>
        {
            setting.canPreview = evt.newValue;
            UpdatePreview();
        });
        fold.Add(previewToggle);

        var colField = new ColorField("Preview Color") { value = setting.color };
        colField.RegisterValueChangedCallback(evt => setting.color = evt.newValue);
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

        //エッジ, タイルごとの設定取得
        var v       = GetSetting(SettingType.VerticalEdge);
        var h       = GetSetting(SettingType.HorizontalEdge);
        var cross   = GetSetting(SettingType.Cross);
        var t       = GetSetting(SettingType.TJunction);
        var corner  = GetSetting(SettingType.Corner);
        var isolate = GetSetting(SettingType.Isolate);

        //各エッジ, タイルオブジェクトを作成
        if (canMergeEdges)
        {
            var merged = new List<Vector3Int>(classification.VerticalEdges);
            merged.AddRange(classification.HorizontalEdges);
            CreateTiles(ClassificationOption.Independent, "EdgeTiles", merged, v.layer, v.tag);
        }
        else
        {
            CreateTiles(v.option, VerticalEdgeName,   classification.VerticalEdges,   v.layer, v.tag);
            CreateTiles(h.option, HorizontalEdgeName, classification.HorizontalEdges, h.layer, h.tag);
        }
        CreateTiles(cross.option,   CrossTileName,     classification.CrossTiles,    cross.layer,   cross.tag);
        CreateTiles(t.option,       TJunctionTileName, classification.TJunctionTiles, t.layer,       t.tag);
        CreateTiles(corner.option,  CornerTileName,    classification.CornerTiles,   corner.layer,  corner.tag);
        CreateTiles(isolate.option, IsolateTileName,   classification.IsolateTiles,  isolate.layer, isolate.tag);
    }

    private void UpdateFoldoutTitles()
    {
        var list = new (Foldout f, string name, int count)[]
        {
            (verticalEdgeFO,   VerticalEdgeName,   classification.VerticalEdges.Count),
            (horizontalEdgeFO, HorizontalEdgeName, classification.HorizontalEdges.Count),
            (crossFO,          CrossTileName,      classification.CrossTiles.Count),
            (tJunctionFO,      TJunctionTileName,  classification.TJunctionTiles.Count),
            (cornerFO,         CornerTileName,     classification.CornerTiles.Count),
            (isolateFO,        IsolateTileName,    classification.IsolateTiles.Count),
        };
        foreach (var (f, name, count) in list)
        {
            f.text = $"{name} (Count:{count})";
        }
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

        TilemapCreator.CreateTiles(original, data, name, layer, tag);
    }

    #region プレビュー機能

    private void OnEnable()  => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView sv)
    {
        if (original == null) return;

        if (settings[0].canPreview) TilemapPreviewDrawer.DrawPreviewList(original, classification.VerticalEdges,    settings[0].color);
        if (settings[1].canPreview) TilemapPreviewDrawer.DrawPreviewList(original, classification.HorizontalEdges,  settings[1].color);
        if (settings[2].canPreview) TilemapPreviewDrawer.DrawPreviewList(original, classification.CrossTiles,       settings[2].color);
        if (settings[3].canPreview) TilemapPreviewDrawer.DrawPreviewList(original, classification.TJunctionTiles,   settings[3].color);
        if (settings[4].canPreview) TilemapPreviewDrawer.DrawPreviewList(original, classification.CornerTiles,      settings[4].color);
        if (settings[5].canPreview) TilemapPreviewDrawer.DrawPreviewList(original, classification.IsolateTiles,     settings[5].color);
    }

    private void UpdatePreview()
    {
        if (original == null) return;

        classification = TilemapClassifier.Classify(original);

        SceneView.RepaintAll();
        UpdateFoldoutTitles();
    }

    #endregion
}
