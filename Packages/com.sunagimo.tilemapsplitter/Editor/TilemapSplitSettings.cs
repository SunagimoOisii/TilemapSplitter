namespace TilemapSplitter
{
    using System.Collections.Generic;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// 分割設定データ
    /// </summary>
    internal class TilemapSplitSettings
    {
        public Tilemap source;
        public bool canMergeEdges;
        public bool canAttachCollider;
        public Dictionary<ShapeType_Rect, ShapeSetting> rectSettings;
        public Dictionary<ShapeType_Hex, ShapeSetting> hexSettings;
    }
}
