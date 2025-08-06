namespace TilemapSplitter
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Tilemaps;

    /// <summary>
    /// Repository class that saves settings via EditorPrefs
    /// </summary>
    internal class TilemapSplitSettingsRepository
    {
        private const string PrefPrefix = "TilemapSplitter.";
        private static string CreateKey(string name) => PrefPrefix + name;

        public TilemapSplitSettings Load()
        {
            var data = new TilemapSplitSettings
            {
                rectSettings = CreateDefaultSettings_Rect(),
                hexSettings = CreateDefaultSettings_Hex()
            };

            if (EditorPrefs.HasKey(CreateKey("SourceId")))
            {
                var idStr = EditorPrefs.GetString(CreateKey("SourceId"));
                if (GlobalObjectId.TryParse(idStr, out var id))
                {
                    data.source = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Tilemap;
                }
            }
            else if (EditorPrefs.HasKey(CreateKey("SourcePath")))
            {
                var path = EditorPrefs.GetString(CreateKey("SourcePath"));
                data.source = AssetDatabase.LoadAssetAtPath<Tilemap>(path);
            }

            data.canMergeEdges = EditorPrefs.GetBool(CreateKey("CanMergeEdges"), data.canMergeEdges);
            data.canAttachCollider = EditorPrefs.GetBool(CreateKey("AttachCollider"), data.canAttachCollider);

            foreach (var kv in data.rectSettings)
            {
                string name = kv.Key.ToString();
                var setting = kv.Value;
                setting.flags = (ShapeFlags)EditorPrefs.GetInt(CreateKey($"{name}.Flags"), (int)setting.flags);
                setting.layer = EditorPrefs.GetInt(CreateKey($"{name}.Layer"), setting.layer);
                setting.tag = EditorPrefs.GetString(CreateKey($"{name}.Tag"), setting.tag);
                setting.canPreview = EditorPrefs.GetBool(CreateKey($"{name}.CanPreview"), setting.canPreview);
                string col = EditorPrefs.GetString(CreateKey($"{name}.Color"), ColorUtility.ToHtmlStringRGBA(setting.previewColor));
                if (ColorUtility.TryParseHtmlString("#" + col, out var c))
                {
                    setting.previewColor = c;
                }
            }

            foreach (var kv in data.hexSettings)
            {
                string name = kv.Key.ToString();
                var setting = kv.Value;
                setting.flags = (ShapeFlags)EditorPrefs.GetInt(CreateKey($"Hex.{name}.Flags"), (int)setting.flags);
                setting.layer = EditorPrefs.GetInt(CreateKey($"Hex.{name}.Layer"), setting.layer);
                setting.tag = EditorPrefs.GetString(CreateKey($"Hex.{name}.Tag"), setting.tag);
                setting.canPreview = EditorPrefs.GetBool(CreateKey($"Hex.{name}.CanPreview"), setting.canPreview);
                string col = EditorPrefs.GetString(CreateKey($"Hex.{name}.Color"), ColorUtility.ToHtmlStringRGBA(setting.previewColor));
                if (ColorUtility.TryParseHtmlString("#" + col, out var c))
                {
                    setting.previewColor = c;
                }
            }

            return data;
        }

        public void Save(TilemapSplitSettings data)
        {
            if (data.source != null)
            {
                var id = GlobalObjectId.GetGlobalObjectIdSlow(data.source);
                EditorPrefs.SetString(CreateKey("SourceId"), id.ToString());
            }
            else
            {
                EditorPrefs.DeleteKey(CreateKey("SourceId"));
            }

            EditorPrefs.SetBool(CreateKey("CanMergeEdges"), data.canMergeEdges);
            EditorPrefs.SetBool(CreateKey("AttachCollider"), data.canAttachCollider);

            foreach (var kv in data.rectSettings)
            {
                string name = kv.Key.ToString();
                var setting = kv.Value;
                EditorPrefs.SetInt(CreateKey($"{name}.Flags"), (int)setting.flags);
                EditorPrefs.SetInt(CreateKey($"{name}.Layer"), setting.layer);
                EditorPrefs.SetString(CreateKey($"{name}.Tag"), setting.tag);
                EditorPrefs.SetBool(CreateKey($"{name}.CanPreview"), setting.canPreview);
                EditorPrefs.SetString(CreateKey($"{name}.Color"),
                    ColorUtility.ToHtmlStringRGBA(setting.previewColor));
            }

            foreach (var kv in data.hexSettings)
            {
                string name = kv.Key.ToString();
                var setting = kv.Value;
                EditorPrefs.SetInt(CreateKey($"Hex.{name}.Flags"), (int)setting.flags);
                EditorPrefs.SetInt(CreateKey($"Hex.{name}.Layer"), setting.layer);
                EditorPrefs.SetString(CreateKey($"Hex.{name}.Tag"), setting.tag);
                EditorPrefs.SetBool(CreateKey($"Hex.{name}.CanPreview"), setting.canPreview);
                EditorPrefs.SetString(CreateKey($"Hex.{name}.Color"),
                    ColorUtility.ToHtmlStringRGBA(setting.previewColor));
            }
        }

        public TilemapSplitSettings Reset()
        {
            EditorPrefs.DeleteKey(CreateKey("SourceId"));
            EditorPrefs.DeleteKey(CreateKey("SourcePath"));
            EditorPrefs.DeleteKey(CreateKey("CanMergeEdges"));
            EditorPrefs.DeleteKey(CreateKey("AttachCollider"));

            foreach (ShapeType_Rect t in Enum.GetValues(typeof(ShapeType_Rect)))
            {
                string name = t.ToString();
                EditorPrefs.DeleteKey(CreateKey($"{name}.Flags"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Layer"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Tag"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.CanPreview"));
                EditorPrefs.DeleteKey(CreateKey($"{name}.Color"));
            }

            foreach (ShapeType_Hex t in Enum.GetValues(typeof(ShapeType_Hex)))
            {
                string name = t.ToString();
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Flags"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Layer"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Tag"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.CanPreview"));
                EditorPrefs.DeleteKey(CreateKey($"Hex.{name}.Color"));
            }

            return new TilemapSplitSettings
            {
                rectSettings = CreateDefaultSettings_Rect(),
                hexSettings = CreateDefaultSettings_Hex(),
                source = null,
                canMergeEdges = false,
                canAttachCollider = false
            };
        }

        public static Dictionary<ShapeType_Rect, ShapeSetting> CreateDefaultSettings_Rect() => new()
        {
            [ShapeType_Rect.VerticalEdge]   = new() { flags = ShapeFlags.VerticalEdge,   previewColor = Color.green  },
            [ShapeType_Rect.HorizontalEdge] = new() { flags = ShapeFlags.HorizontalEdge, previewColor = Color.yellow },
            [ShapeType_Rect.Cross]          = new() { flags = ShapeFlags.Independent,    previewColor = Color.red    },
            [ShapeType_Rect.TJunction]      = new() { flags = ShapeFlags.Independent,    previewColor = Color.blue   },
            [ShapeType_Rect.Corner]         = new() { flags = ShapeFlags.Independent,    previewColor = Color.cyan   },
            [ShapeType_Rect.Isolate]        = new() { flags = ShapeFlags.Independent,    previewColor = Color.magenta },
        };

        public static Dictionary<ShapeType_Hex, ShapeSetting> CreateDefaultSettings_Hex() => new()
        {
            [ShapeType_Hex.Full]      = new() { flags = ShapeFlags.Independent, previewColor = Color.red    },
            [ShapeType_Hex.Junction5] = new() { flags = ShapeFlags.Independent, previewColor = Color.blue   },
            [ShapeType_Hex.Junction4] = new() { flags = ShapeFlags.Independent, previewColor = Color.cyan   },
            [ShapeType_Hex.Junction3] = new() { flags = ShapeFlags.Independent, previewColor = Color.green  },
            [ShapeType_Hex.Edge]      = new() { flags = ShapeFlags.Independent, previewColor = Color.yellow },
            [ShapeType_Hex.Tip]       = new() { flags = ShapeFlags.Independent, previewColor = Color.magenta },
            [ShapeType_Hex.Isolate]   = new() { flags = ShapeFlags.Independent, previewColor = Color.gray   },
        };
    }
}
