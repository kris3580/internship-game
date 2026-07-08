using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class AddressablesSkinSetup
{
    private const string GroupName = "Skin Prefabs";
    private static readonly string[] SkinFolders =
    {
        "Assets/Prefabs/Board",
        "Assets/Prefabs/PoolSticks"
    };
    private static readonly string[] BallMaterialFolders =
    {
        "Assets/Models/Balls"
    };

    [MenuItem("Tools/Setup Skin Addressables")]
    public static void Setup()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

        AddressableAssetGroup group = settings.FindGroup(GroupName)
            ?? settings.CreateGroup(
                GroupName,
                false,
                false,
                true,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));

        EnsureSchema<BundledAssetGroupSchema>(group);
        EnsureSchema<ContentUpdateGroupSchema>(group);

        foreach (string folder in SkinFolders)
            AddAssetsInFolder(settings, group, folder, "*.prefab");

        foreach (string folder in BallMaterialFolders)
            AddAssetsInFolder(settings, group, folder, "Pool Ball*.mat");

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("Skin prefabs registered as Addressables.");
    }

    private static void EnsureSchema<T>(AddressableAssetGroup group)
        where T : AddressableAssetGroupSchema
    {
        if (group != null && group.GetSchema<T>() == null)
            group.AddSchema<T>();
    }

    private static void AddAssetsInFolder(
        AddressableAssetSettings settings,
        AddressableAssetGroup group,
        string folder,
        string searchPattern)
    {
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        string[] assetPaths = Directory.GetFiles(folder, searchPattern, SearchOption.TopDirectoryOnly);

        foreach (string path in assetPaths)
        {
            string assetPath = path.Replace("\\", "/");
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(assetPath);
        }
    }
}
