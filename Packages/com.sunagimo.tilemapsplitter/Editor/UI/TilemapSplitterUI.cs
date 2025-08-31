namespace TilemapSplitter
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    /// <summary>
    /// Class responsible for building the window UI
    /// </summary>
    internal static class TilemapSplitterUI
    {
        public static void Build(TilemapSplitterWindow window, TilemapSplitterService service)
        {
            var container = CreateScrollableContainer(window.rootVisualElement);
            CreateSourceField(container, service, window);
            CreateResetButton(container, window);
            CreateColliderToggle(container, service);

            // Validation + guard messages
            var (canProceed, helpBox, layoutNote) = CreateGuards(container, service);

            if (service.Source != null && service.Source.gameObject.activeInHierarchy)
            {
                service.SetupLayoutStrategy(window);
                service.LayoutStrategy?.CreateMergeEdgeToggle(container, () => service.CanMergeEdges, v => service.CanMergeEdges = v);
                service.LayoutStrategy?.CreateShapeFoldouts(container);
            }

            CreateExecuteButton(container, service, window, canProceed);

            if (canProceed)
            {
                service.SetupPreview();
                service.RefreshPreview(window);
            }
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
            var hp = new HelpBox("Select the tilemap to split", HelpBoxMessageType.Info);
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

        private static void CreateExecuteButton(VisualElement container, TilemapSplitterService service, TilemapSplitterWindow window, bool canProceed)
        {
            var splitB = new Button(() =>
            {
                if (service.Source == null)
                {
                    EditorUtility.DisplayDialog("Error", "Tilemap to split is not set", "OK");
                    return;
                }
                if (service.Source.layoutGrid == null)
                {
                    EditorUtility.DisplayDialog("Error", "The selected Tilemap is not under a Grid. Please place it under a Grid GameObject.", "OK");
                    return;
                }
                if (service.Source.GetUsedTilesCount() == 0)
                {
                    EditorUtility.DisplayDialog("Warning", "The selected Tilemap has no tiles. Add tiles before splitting.", "OK");
                    return;
                }
                service.ExecuteSplit(window);
            });
            splitB.text = "Execute Split";
            splitB.style.marginTop = 10;
            splitB.SetEnabled(canProceed);
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

        private static (bool canProceed, HelpBox guardBox, HelpBox layoutNote) CreateGuards(VisualElement container, TilemapSplitterService service)
        {
            bool canProceed = true;

            HelpBox guardBox = null;
            HelpBox layoutBox = null;

            if (service.Source == null)
            {
                guardBox = new HelpBox("Tilemap is not assigned. Please set a target Tilemap.", HelpBoxMessageType.Info);
                canProceed = false;
            }
            else if (service.Source.layoutGrid == null)
            {
                guardBox = new HelpBox("The selected Tilemap is not under a Grid. Please place it under a Grid GameObject.", HelpBoxMessageType.Error);
                canProceed = false;
            }
            else if (service.Source.GetUsedTilesCount() == 0)
            {
                guardBox = new HelpBox("The selected Tilemap has no tiles. Add tiles to proceed.", HelpBoxMessageType.Warning);
                canProceed = false;
            }
            else
            {
                var layout = service.Source.layoutGrid.cellLayout;
                // Provide notes for supported layouts. Rect/Hex are fully targeted; Isometric has known limitations.
                if (layout == GridLayout.CellLayout.Isometric || layout == GridLayout.CellLayout.IsometricZAsY)
                {
                    layoutBox = new HelpBox("Isometric layout: Unity's draw-order limitations apply. Classification works, but exact cross-Tilemap ordering may not be achievable.", HelpBoxMessageType.Info);
                }
            }

            if (guardBox != null) container.Add(guardBox);
            if (layoutBox != null) container.Add(layoutBox);

            return (canProceed, guardBox, layoutBox);
        }
    }
}
