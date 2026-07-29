using UnityEditor;

/// <summary>
/// NuGet 復元で meta が再生成されても System.CodeDom の Editor 除外を維持する
/// (Unity Editor 標準の System.dll と型が重複し CS0433 になるため)
/// </summary>
public sealed class NuGetPackageImportSettings : AssetPostprocessor
{
    private void OnPreprocessAsset()
    {
        if (!assetPath.StartsWith("Assets/Packages/System.CodeDom.") || !assetPath.EndsWith(".dll")) return;
        if (assetImporter is not PluginImporter plugin) return;

        plugin.SetCompatibleWithAnyPlatform(true);
        plugin.SetExcludeEditorFromAnyPlatform(true);
        plugin.SetCompatibleWithEditor(false);
    }
}
