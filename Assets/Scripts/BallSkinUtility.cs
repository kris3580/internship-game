using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class BallSkinUtility
{
    private static readonly Dictionary<string, Material> LoadedMaterials = new();

    public static string CurrentBallSkinId => MetaGameSave.GetSelectedSkin("balls");

    public static void ApplySelectedBallMaterial(GameObject ball)
    {
        if (ball == null || !int.TryParse(ball.tag, out _))
            return;

        Material material = LoadBallMaterial(CurrentBallSkinId);

        if (material == null)
            return;

        foreach (Renderer renderer in ball.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;
    }

    public static int GetCurrentSkinIndex()
    {
        string id = CurrentBallSkinId.ToLowerInvariant();

        if (id.Contains("skin1"))
            return 1;
        if (id.Contains("skin2"))
            return 2;
        if (id.Contains("skin3"))
            return 3;
        if (id.Contains("skin4"))
            return 4;
        if (id.Contains("skin5"))
            return 5;

        return 0;
    }

    private static Material LoadBallMaterial(string skinId)
    {
        string materialName = "Pool Ball Default";
        string lower = (skinId ?? string.Empty).ToLowerInvariant();

        for (int i = 1; i <= 5; i++)
        {
            if (lower.Contains($"skin{i}"))
            {
                materialName = $"Pool Ball Skin{i}";
                break;
            }
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Material>($"Assets/Models/Balls/{materialName}.mat");
#else
        if (LoadedMaterials.TryGetValue(materialName, out Material cachedMaterial))
            return cachedMaterial;

        AsyncOperationHandle<Material> handle = Addressables.LoadAssetAsync<Material>(materialName);
        Material material = handle.WaitForCompletion();

        if (material != null)
            LoadedMaterials[materialName] = material;

        return material;
#endif
    }
}
