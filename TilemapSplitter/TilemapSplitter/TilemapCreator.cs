using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapCreator
{
    public static void CreateTiles(Tilemap original, List<Vector3Int> data, string name,
        int layer, string tag)
    {
        if (original == null || data == null || data.Count == 0) return;

        var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(original.transform.parent, false);
        obj.layer = layer;
        obj.tag   = tag;

        var renderer = obj.GetComponent<TilemapRenderer>();
        if (original.TryGetComponent<TilemapRenderer>(out var oriRenderer))
        {
            renderer.sortingLayerID = oriRenderer.sortingLayerID;
            renderer.sortingOrder   = oriRenderer.sortingOrder;
        }
        else
        {
            Debug.LogWarning("Since TilemapRenderer is not attached to the split target," +
                "the TilemapRenderer of the generated object was generated with the default settings.");
        }

        var tm = obj.GetComponent<Tilemap>();
        foreach (var p in data) tm.SetTile(p, original.GetTile(p));

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
    }
}
