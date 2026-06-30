using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameSkinApplier : MonoBehaviour
{
    private readonly ISkinLoader skinLoader = new AddressableSkinLoader();

    private void Start()
    {
        Apply();
    }

    private void Apply()
    {
        ApplyPrefabSkin("poolstick", "PoolStick/StartGameAnimation/PoolAnimator", "DefaultPoolStick");
        ApplyPrefabSkin("board", "==GAME/Board", "BoardDefault");
        ApplyExistingBallMaterials();
    }

    private void ApplyPrefabSkin(string category, string targetPath, string defaultChildName)
    {
        string selected = MetaGameSave.GetSelectedSkin(category);

        if (string.IsNullOrWhiteSpace(selected) || selected == "default")
            return;

        Transform target = FindByLoosePath(targetPath);

        if (target == null)
            return;

        string address = GetSkinAddress(category, selected);

        if (string.IsNullOrWhiteSpace(address))
            return;

        List<GameObject> oldSkinObjects = GetExistingSkinObjects(target, defaultChildName, selected, address);

        skinLoader.LoadSkin(address, target, instance =>
        {
            if (instance == null)
                return;

            instance.name = address;

            foreach (GameObject oldSkinObject in oldSkinObjects)
            {
                if (oldSkinObject != null)
                    Destroy(oldSkinObject);
            }
        });
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
            int startIndex = NamesMatch(root.name, parts[0]) ? 1 : 0;

            if (startIndex == 0)
            {
                current = FindChildByLooseName(root.transform, parts[0]);

                if (current == null)
                    continue;

                startIndex = 1;
            }

            bool matched = true;

            for (int i = startIndex; i < parts.Length; i++)
            {
                current = FindChildByLooseName(current, parts[i]);

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

    private static Transform FindChildByLooseName(Transform parent, string name)
    {
        Transform exactDirectChild = FindChild(parent, name, directOnly: true, exact: true);

        if (exactDirectChild != null)
            return exactDirectChild;

        Transform exactDescendant = FindChild(parent, name, directOnly: false, exact: true);

        if (exactDescendant != null)
            return exactDescendant;

        Transform looseDirectChild = FindChild(parent, name, directOnly: true, exact: false);

        if (looseDirectChild != null)
            return looseDirectChild;

        return FindChild(parent, name, directOnly: false, exact: false);
    }

    private static Transform FindChild(Transform parent, string name, bool directOnly, bool exact)
    {
        string cleanName = NormalizeName(name);

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child == parent)
                continue;

            if (directOnly && child.parent != parent)
                continue;

            string cleanChildName = NormalizeName(child.name);
            bool matched = exact
                ? cleanChildName.Equals(cleanName, StringComparison.OrdinalIgnoreCase)
                : cleanChildName.Contains(cleanName, StringComparison.OrdinalIgnoreCase);

            if (matched)
                return child;
        }

        return null;
    }

    private static List<GameObject> GetExistingSkinObjects(Transform target, string defaultChildName, string selected, string address)
    {
        List<GameObject> skinObjects = new();

        for (int i = 0; i < target.childCount; i++)
        {
            Transform child = target.GetChild(i);

            if (child.name.Contains(defaultChildName, StringComparison.OrdinalIgnoreCase)
                || child.name.Contains(selected, StringComparison.OrdinalIgnoreCase)
                || child.name.Contains(address, StringComparison.OrdinalIgnoreCase))
            {
                skinObjects.Add(child.gameObject);
            }
        }

        return skinObjects;
    }

    private static string GetSkinAddress(string category, string selected)
    {
        string id = selected.ToLowerInvariant();

        return category switch
        {
            "poolstick" => ToAddressPrefix(id) + "PoolStick",
            "board" => id switch
            {
                "blue" => "BoardBlue",
                "green" or "purple" => "BoardPurple",
                "pink" or "red" => "BoardRed",
                _ => string.Empty
            },
            _ => string.Empty
        };
    }

    private static string ToAddressPrefix(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static bool NamesMatch(string candidate, string target)
    {
        return NormalizeName(candidate).Equals(NormalizeName(target), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeName(string value)
    {
        return value.Replace("=", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace("(Clone)", string.Empty)
            .Trim();
    }
}
