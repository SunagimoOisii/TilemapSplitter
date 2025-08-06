namespace TilemapSplitter
{
    using System.Collections;
    using Unity.EditorCoroutines.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 分割処理やプレビューを担当するサービス
    /// </summary>
    internal class TilemapSplitterService
    {
        private TilemapSplitSettings settings;
        private ICellLayoutStrategy layoutStrategy;
        private readonly TilemapPreviewDrawer previewDrawer = new();
        private bool isRefreshingPreview;

        public TilemapSplitterService(TilemapSplitSettings settings)
        {
            this.settings = settings;
        }

        public TilemapSplitSettings Settings => settings;

        public Tilemap Source { get => settings.source; set => settings.source = value; }
        public bool CanMergeEdges { get => settings.canMergeEdges; set => settings.canMergeEdges = value; }
        public bool CanAttachCollider { get => settings.canAttachCollider; set => settings.canAttachCollider = value; }
        public System.Collections.Generic.Dictionary<ShapeType_Rect, ShapeSetting> RectSettings => settings.rectSettings;
        public System.Collections.Generic.Dictionary<ShapeType_Hex, ShapeSetting> HexSettings => settings.hexSettings;
        public ICellLayoutStrategy LayoutStrategy => layoutStrategy;

        public void ApplySettings(TilemapSplitSettings newSettings)
        {
            settings = newSettings;
        }

        public void RegisterPreview() => previewDrawer.Register();
        public void UnregisterPreview() => previewDrawer.Unregister();

        public void SetupLayoutStrategy(EditorWindow window)
        {
            if (Source == null) return;
            var layout = Source.layoutGrid.cellLayout;
            layoutStrategy = (layout == GridLayout.CellLayout.Hexagon)
                ? new CellLayoutStrategy_Hex(HexSettings, () => RefreshPreview(window))
                : new CellLayoutStrategy_Rect(RectSettings, () => RefreshPreview(window));
        }

        public void SetupPreview()
        {
            layoutStrategy?.SetupPreview(Source, previewDrawer);
        }

        public void RefreshPreview(EditorWindow window)
        {
            if (Source == null || isRefreshingPreview || layoutStrategy == null) return;
            EditorCoroutineUtility.StartCoroutine(RefreshPreviewCoroutine(), window);

            IEnumerator RefreshPreviewCoroutine()
            {
                isRefreshingPreview = true;

                var progress = new CancelableProgressBar("分類");
                IEnumerator<bool> e = layoutStrategy.Classify(Source, progress, () => progress.IsCancelled);
                while (e.MoveNext())
                {
                    if (e.Current)
                    {
                        progress.Clear();
                        isRefreshingPreview = false;
                        yield break;
                    }
                    yield return null;
                }

                progress.Clear();
                layoutStrategy.SetupPreview(Source, previewDrawer);
                layoutStrategy.SetShapeCellsToPreview(previewDrawer);
                SceneView.RepaintAll();
                layoutStrategy.UpdateFoldoutTitles();

                isRefreshingPreview = false;
            }
        }

        public void ExecuteSplit(EditorWindow window)
        {
            EditorCoroutineUtility.StartCoroutine(SplitCoroutine(), window);

            IEnumerator SplitCoroutine()
            {
                var progress = new CancelableProgressBar("分類");
                IEnumerator<bool> e = layoutStrategy.Classify(Source, progress, () => progress.IsCancelled);
                while (e.MoveNext())
                {
                    if (e.Current)
                    {
                        progress.Clear();
                        yield break;
                    }
                    yield return null;
                }
                progress.Clear();
                layoutStrategy.GenerateSplitTilemaps(Source, CanMergeEdges, CanAttachCollider);
                RefreshPreview(window);
            }
        }

        /// <summary>
        /// キャンセル可能なプログレスバー
        /// </summary>
        private sealed class CancelableProgressBar : IProgress<float>
        {
            private readonly string title;
            public bool IsCancelled { get; private set; }

            public CancelableProgressBar(string title)
            {
                this.title = title;
            }

            public void Report(float value)
            {
                IsCancelled = EditorUtility.DisplayCancelableProgressBar(
                    title, $"分類中... {Mathf.RoundToInt(value * 100)}%", value);
            }

            public void Clear()
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
