namespace TilemapSplitter
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// Public API to execute Tilemap splitting without opening the GUI window.
    /// Mirrors the internal logic used by the editor window: classify -> generate.
    /// </summary>
    public static class TilemapSplitterApi
    {
        /// <summary>
        /// Split the specified Tilemap and return the list of generated GameObjects.
        /// Uses current saved settings and overrides runtime options via parameters.
        /// </summary>
        public static List<GameObject> Split(Tilemap source,
            bool canMergeEdges = false,
            bool canAttachCollider = false,
            IProgress<float> progress = null,
            Func<bool> isCancelled = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source), "Tilemap source is null");
            }
            if (source.gameObject.activeInHierarchy == false)
            {
                Debug.LogWarning("TilemapSplitterApi.Split: source is inactive in hierarchy; aborting.");
                return new List<GameObject>(0);
            }

            // Load defaults and override by parameters
            var repo     = new TilemapSplitSettingsRepository();
            var settings = repo.Load();
            settings.source            = source;
            settings.canMergeEdges     = canMergeEdges;
            settings.canAttachCollider = canAttachCollider;

            // Choose strategy by layout (Rect or Hex). Preview refresh is a no-op here.
            ICellLayoutStrategy strategy;
            var layout = source.layoutGrid.cellLayout;
            if (layout == GridLayout.CellLayout.Hexagon)
            {
                strategy = new CellLayoutStrategy_Hex(settings.hexSettings, () => { });
            }
            else
            {
                strategy = new CellLayoutStrategy_Rect(settings.rectSettings, () => { });
            }

            // Classify synchronously (supports cancellation)
            var e = strategy.Classify(source, progress, isCancelled);
            while (e.MoveNext())
            {
                if (e.Current) return new List<GameObject>(0);
            }

            // Group into one Undo step
            int group = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Tilemap Split");
            try
            {
                return strategy.GenerateSplitTilemaps(source, settings.canMergeEdges, settings.canAttachCollider);
            }
            finally
            {
                Undo.CollapseUndoOperations(group);
            }
        }
    }
}
