namespace TilemapSplitter
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.Tilemaps;
    using UnityEngine.UIElements;

    /// <summary>
    /// Strategy interface that provides operations according to cell layout
    /// </summary>
    internal interface ICellLayoutStrategy
    {
        void CreateMergeEdgeToggle(VisualElement container, Func<bool> getter, Action<bool> setter);
        void CreateShapeFoldouts(VisualElement container);
        IEnumerator<bool> Classify(Tilemap source, IProgress<float> progress = null,
            Func<bool> isCancelled = null);
        void GenerateSplitTilemaps(Tilemap source, bool canMergeEdges, bool canAttachCollider);
        void SetupPreview(Tilemap source, TilemapPreviewDrawer drawer);
        void SetShapeCellsToPreview(TilemapPreviewDrawer drawer);
        void UpdateFoldoutTitles();
    }
}