using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressablesSkinSetup
{
    private const string GroupName = "Skin Prefabs";
    private static readonly string[] SkinFolders =
    {
        "Assets/Prefabs/Board",
        "Assets/Prefabs/PoolSticks"
    };

    [MenuItem("Tools/Setup Skin Addressables")]
    public static void Setup()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

        AddressableAssetGroup group = settings.FindGroup(GroupName)
            ?? settings.CreateGroup(GroupName, false, false, true, null);

        foreach (string folder in SkinFolders)
            AddPrefabsInFolder(settings, group, folder);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("Skin prefabs registered as Addressables.");
    }

    private static void AddPrefabsInFolder(
        AddressableAssetSettings settings,
        AddressableAssetGroup group,
        string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        string[] prefabPaths = Directory.GetFiles(folder, "*.prefab", SearchOption.TopDirectoryOnly);

        foreach (string path in prefabPaths)
        {
            string assetPath = path.Replace("\\", "/");
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(assetPath);
        }
    }
}
