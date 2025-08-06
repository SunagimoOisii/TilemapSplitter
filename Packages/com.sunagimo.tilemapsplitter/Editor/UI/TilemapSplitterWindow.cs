namespace TilemapSplitter
{
    using UnityEditor;

    /// <summary>
    /// Window for splitting tilemaps
    /// </summary>
    internal class TilemapSplitterWindow : EditorWindow
    {
        private readonly TilemapSplitSettingsRepository repository = new();
        private TilemapSplitterService service;

        [MenuItem("Tools/TilemapSplitter")]
        public static void ShowWindow() => GetWindow<TilemapSplitterWindow>("Split Tilemap");

        private void OnEnable()
        {
            var data = repository.Load();
            service = new TilemapSplitterService(data);
            service.RegisterPreview();
        }

        private void OnDisable()
        {
            repository.Save(service.Settings);
            service.UnregisterPreview();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            TilemapSplitterUI.Build(this, service);
        }

        public void ResetSettings()
        {
            service.ApplySettings(repository.Reset());
            CreateGUI();
        }
    }
}
