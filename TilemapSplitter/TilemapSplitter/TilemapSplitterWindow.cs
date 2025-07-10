using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class TilemapSplitterWindow : EditorWindow
{
    private readonly ClassificationSetting[] settings = new ClassificationSetting[6]
    {
        new() { option = ClassificationOption.VerticalEdge,  color = Color.green  },
        new() { option = ClassificationOption.HorizontalEdge,color = Color.yellow },
        new() { option = ClassificationOption.Independent,   color = Color.red    },
        new() { option = ClassificationOption.Independent,   color = Color.blue   },
        new() { option = ClassificationOption.Independent,   color = Color.cyan   },
        new() { option = ClassificationOption.Independent,   color = Color.magenta },
    };
    private ClassificationSetting GetSetting(SettingType t) => settings[(int)t];

    private Foldout verticalEdgeFO;
    private Foldout horizontalEdgeFO;
    private Foldout crossFO;
    private Foldout tJunctionFO;
    private Foldout cornerFO;
    private Foldout isolateFO;

    private Tilemap original;
    private ClassificationResult result = new();
    private readonly TilemapPreviewDrawer previewDrawer = new();

    private bool canMergeEdges = false;

    [MenuItem("Tools/TilemapSplitter")]
    public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("Split Tilemap");

    private void OnEnable()
    {
        previewDrawer.Register();
    }

    private void OnDisable()
    {
        previewDrawer.Unregister();
    }

    public void CreateGUI()
    {
        var root = rootVisualElement;

        var scroll    = new ScrollView();
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.paddingLeft   = 10;
        container.style.paddingRight  = 10;
        root.Add(scroll);
        scroll.Add(container);

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

        var mergeToggle = new Toggle("Merge VerticalEdge, HorizontalEdge") { value = canMergeEdges };
        var mergeHB = new HelpBox("When merging, VerticalEdge settings take precedence",
            HelpBoxMessageType.Info);
        mergeToggle.RegisterValueChangedCallback(evt => canMergeEdges = evt.newValue);
        container.Add(mergeToggle);
        container.Add(mergeHB);

        verticalEdgeFO   = CreateEdgeFoldout(container, "VerticalEdge",   SettingType.VerticalEdge);
        AddSeparator(container);
        horizontalEdgeFO = CreateEdgeFoldout(container, "HorizontalEdge", SettingType.HorizontalEdge);
        AddSeparator(container);

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

        var splitButton = new Button(() =>
        {
            if (original == null)
            {
                EditorUtility.DisplayDialog("Error", "The split target isn't set", "OK");
                return;
            }
            result = TilemapClassifier.Classify(original, settings);
            TilemapCreator.Create(original, result, settings, canMergeEdges);
            UpdatePreview();
        });
        splitButton.text = "Execute Splitting";
        splitButton.style.marginTop = 10;
        container.Add(splitButton);

        previewDrawer.Initialize(original, settings);
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

    private void UpdatePreview()
    {
        if (original == null) return;
        result = TilemapClassifier.Classify(original, settings);
        previewDrawer.Initialize(original, settings);
        previewDrawer.SetResult(result);
        SceneView.RepaintAll();
        UpdateFoldoutTitles();
    }

    private void UpdateFoldoutTitles()
    {
        var list = new (Foldout f, string name, int count)[]
        {
            (verticalEdgeFO,   "VerticalEdge",   result.VerticalEdges.Count),
            (horizontalEdgeFO, "HorizontalEdge", result.HorizontalEdges.Count),
            (crossFO,          "CrossTiles",     result.CrossTiles.Count),
            (tJunctionFO,      "TJunctionTiles", result.TJunctionTiles.Count),
            (cornerFO,         "CornerTiles",    result.CornerTiles.Count),
            (isolateFO,        "IsolateTiles",   result.IsolateTiles.Count),
        };
        foreach (var (f, name, count) in list)
        {
            f.text = $"{name} (Count:{count})";
        }
    }
}
