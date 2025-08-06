namespace TilemapSplitter
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    /// <summary>
    /// ウィンドウのUI構築を担当するクラス
    /// </summary>
    internal static class TilemapSplitterUI
    {
        public static void Build(TilemapSplitterWindow window, TilemapSplitterService service)
        {
            var container = CreateScrollableContainer(window.rootVisualElement);
            CreateSourceField(container, service, window);
            CreateResetButton(container, window);
            CreateColliderToggle(container, service);

            if (service.Source == null || service.Source.gameObject.activeInHierarchy == false) return;

            service.SetupLayoutStrategy(window);
            service.LayoutStrategy.CreateMergeEdgeToggle(container, () => service.CanMergeEdges, v => service.CanMergeEdges = v);
            service.LayoutStrategy.CreateShapeFoldouts(container);

            CreateExecuteButton(container, service, window);

            service.SetupPreview();
            service.RefreshPreview(window);
        }

        private static VisualElement CreateScrollableContainer(VisualElement root)
        {
            var scroll = new ScrollView();
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft = 10;
            container.style.paddingRight = 10;
            root.Add(scroll);
            scroll.Add(container);

            AddHorizontalSeparator(container);
            return container;
        }

        private static void CreateSourceField(VisualElement container, TilemapSplitterService service, TilemapSplitterWindow window)
        {
            var sourceF = new ObjectField("Split Tilemap");
            var hp = new HelpBox("Select the subject of the division", HelpBoxMessageType.Info);
            sourceF.objectType = typeof(Tilemap);
            sourceF.value = service.Source;
            sourceF.RegisterValueChangedCallback(evt =>
            {
                service.Source = evt.newValue as Tilemap;
                hp.visible = (service.Source == null);

                window.CreateGUI();
                service.RefreshPreview(window);
            });
            hp.visible = (service.Source == null);
            container.Add(sourceF);
            container.Add(hp);
        }

        private static void CreateResetButton(VisualElement container, TilemapSplitterWindow window)
        {
            var resetB = new Button(() => window.ResetSettings());
            resetB.text = "Reset Settings";
            resetB.style.marginTop = 5;
            container.Add(resetB);
        }

        private static void CreateColliderToggle(VisualElement container, TilemapSplitterService service)
        {
            var attachT = new Toggle("Attach Colliders");
            attachT.value = service.CanAttachCollider;
            attachT.RegisterValueChangedCallback(evt => service.CanAttachCollider = evt.newValue);
            container.Add(attachT);
        }

        private static void CreateExecuteButton(VisualElement container, TilemapSplitterService service, TilemapSplitterWindow window)
        {
            var splitB = new Button(() =>
            {
                if (service.Source == null)
                {
                    EditorUtility.DisplayDialog("Error", "The split target isn't set", "OK");
                    return;
                }
                service.ExecuteSplit(window);
            });
            splitB.text = "Execute Splitting";
            splitB.style.marginTop = 10;
            container.Add(splitB);
        }

        public static void AddHorizontalSeparator(VisualElement parentContainer)
        {
            var separator = new VisualElement();
            separator.style.borderBottomWidth = 1;
            separator.style.borderBottomColor = Color.gray;
            separator.style.marginTop = 5;
            separator.style.marginBottom = 5;
            parentContainer.Add(separator);
        }
    }
}
