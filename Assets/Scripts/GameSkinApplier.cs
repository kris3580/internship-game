using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class GameSkinApplier : MonoBehaviour
{
    private void Start()
    {
        Apply();
    }

    private void Apply()
    {
        ApplyPrefabSkin("poolstick", "PoolStick/StartGameAnimation/PoolAnimator", "DefaultPoolStick", "Assets/Prefabs/PoolSticks");
        ApplyPrefabSkin("board", "==GAME/Board", "BoardDefault", "Assets/Prefabs/Board");
        ApplyExistingBallMaterials();
    }

    private void ApplyPrefabSkin(string category, string targetPath, string defaultChildName, string folder)
    {
        string selected = MetaGameSave.GetSelectedSkin(category);

        if (string.IsNullOrWhiteSpace(selected) || selected == "default")
            return;

        Transform target = FindByLoosePath(targetPath);

        if (target == null)
            return;

        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Transform child = target.GetChild(i);

            if (child.name.Contains(defaultChildName, StringComparison.OrdinalIgnoreCase)
                || child.name.Contains(selected, StringComparison.OrdinalIgnoreCase))
            {
                Destroy(child.gameObject);
            }
        }

        GameObject prefab = LoadSkinPrefab(folder, selected);

        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, target);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
    }

    private void ApplyExistingBallMaterials()
    {
        foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (candidate.hideFlags != HideFlags.None || !candidate.scene.IsValid())
                continue;

            BallSkinUtility.ApplySelectedBallMaterial(candidate);
        }
    }

    private static Transform FindByLoosePath(string path)
    {
        string[] parts = path.Split('/');

        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform current = root.transform;
            int startIndex = root.name.Equals(parts[0], StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            if (startIndex == 0)
            {
                current = FindChildContains(root.transform, parts[0]);

                if (current == null)
                    continue;

                startIndex = 1;
            }

            bool matched = true;

            for (int i = startIndex; i < parts.Length; i++)
            {
                current = FindChildContains(current, parts[i]);

                if (current == null)
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
                return current;
        }

        return null;
    }

    private static Transform FindChildContains(Transform parent, string name)
    {
        string cleanName = name.Replace("=", string.Empty).Trim();

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child == parent)
                continue;

            if (child.name.Contains(cleanName, StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private static GameObject LoadSkinPrefab(string folder, string selected)
    {
#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (fileName.Contains(selected, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
#endif
        return null;
    }
}
