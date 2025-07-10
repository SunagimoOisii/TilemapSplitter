using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapCreator
{
    private const string VerticalEdgeName   = "VerticalEdge";
    private const string HorizontalEdgeName = "HorizontalEdge";
    private const string CrossTileName      = "CrossTiles";
    private const string TJunctionTileName  = "TJunctionTiles";
    private const string CornerTileName     = "CornerTiles";
    private const string IsolateTileName    = "IsolateTiles";

    /// <summary>
    /// Tilemap �𕪊����A�V���� Tilemap �I�u�W�F�N�g�𐶐�
    /// </summary>
    public static void Create(Tilemap original, ClassificationResult result,
        ClassificationSetting[] settings, bool mergeEdges)
    {
        if (mergeEdges)
        {
            var merged = new List<Vector3Int>(result.VerticalEdges);
            merged.AddRange(result.HorizontalEdges);
            var v = settings[(int)SettingType.VerticalEdge];
            CreateTiles(original, ClassificationOption.Independent, "EdgeTiles", merged, v.layer, v.tag);
        }
        else
        {
            var v = settings[(int)SettingType.VerticalEdge];
            var h = settings[(int)SettingType.HorizontalEdge];
            CreateTiles(original, v.option, VerticalEdgeName, result.VerticalEdges, v.layer, v.tag);
            CreateTiles(original, h.option, HorizontalEdgeName, result.HorizontalEdges, h.layer, h.tag);
        }

        var cross   = settings[(int)SettingType.Cross];
        var t       = settings[(int)SettingType.TJunction];
        var corner  = settings[(int)SettingType.Corner];
        var isolate = settings[(int)SettingType.Isolate];

        CreateTiles(original, cross.option,   CrossTileName,     result.CrossTiles,   cross.layer,   cross.tag);
        CreateTiles(original, t.option,       TJunctionTileName, result.TJunctionTiles, t.layer,       t.tag);
        CreateTiles(original, corner.option,  CornerTileName,    result.CornerTiles,  corner.layer,  corner.tag);
        CreateTiles(original, isolate.option, IsolateTileName,   result.IsolateTiles, isolate.layer, isolate.tag);
    }

    /// <summary>
    /// �^�C�����W���X�g�ʂ�� Tilemap ������ GameObject �𐶐�
    /// </summary>
    private static void CreateTiles(Tilemap original, ClassificationOption opt, string name,
        List<Vector3Int> tilePositions, int layer, string tag)
    {
        if (tilePositions == null || 
            tilePositions.Count == 0) return;

        //Independent ���K�v�ȏꍇ�A�ݒ�ɂȂ���ΐ������Ȃ�
        bool isRequiredIndependentOption = name == CrossTileName  || name == TJunctionTileName ||
                                           name == CornerTileName || name == IsolateTileName;
        if (isRequiredIndependentOption && 
            opt.HasFlag(ClassificationOption.Independent) == false) return;

        //Tilemap, TilemapRenderer ������ GameObject ����
        //���C���[�����w��̂��̂ɕύX
        var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        obj.transform.SetParent(original.transform.parent, false);
        obj.layer = layer;
        obj.tag   = tag;

        //�������� TilemapRenderer ������΂��̐ݒ����v������
        var renderer = obj.GetComponent<TilemapRenderer>();
        if (original.TryGetComponent<TilemapRenderer>(out var oriRenderer))
        {
            renderer.sortingLayerID = oriRenderer.sortingLayerID;
            renderer.sortingOrder   = oriRenderer.sortingOrder;
        }
        else
        {
            Debug.LogWarning("Since TilemapRenderer is not attached to the split target, " +
                "the TilemapRenderer of the generated object was generated with the default settings.");
        }

        //���^�C���̐ݒ�𐶐��^�C���փR�s�[
        var tm = obj.GetComponent<Tilemap>();
        foreach (var p in tilePositions)
        {
            tm.SetTile(p, original.GetTile(p));
            tm.SetColor(p, original.GetColor(p));
            tm.SetTransformMatrix(p,original.GetTransformMatrix(p));
        }

        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
    }
}
