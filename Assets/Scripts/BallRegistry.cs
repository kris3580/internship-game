using System.Collections.Generic;
using UnityEngine;
using Zenject;

public sealed class BallRegistry : IBallRegistry, IInitializable
{
    private readonly List<GameObject> registeredRoots = new();
    private readonly List<BallInfo> activeBalls = new();

    public void Initialize()
    {
        RefreshSceneBalls();
    }

    public IReadOnlyList<BallInfo> GetActiveBalls()
    {
        RebuildActiveBalls();
        return activeBalls;
    }

    public void Register(GameObject candidate)
    {
        if (!TryGetBallRoot(candidate, out GameObject root))
            return;

        if (!registeredRoots.Contains(root))
            registeredRoots.Add(root);
    }

    public void Unregister(GameObject candidate)
    {
        if (candidate == null)
            return;

        if (TryGetBallRoot(candidate, out GameObject root))
        {
            registeredRoots.Remove(root);
            return;
        }

        registeredRoots.Remove(candidate);
    }

    public void RefreshSceneBalls()
    {
        registeredRoots.Clear();

        Rigidbody[] rigidbodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

        foreach (Rigidbody body in rigidbodies)
        {
            if (body != null)
                Register(body.gameObject);
        }

        RebuildActiveBalls();
    }

    private void RebuildActiveBalls()
    {
        activeBalls.Clear();

        for (int i = registeredRoots.Count - 1; i >= 0; i--)
        {
            GameObject root = registeredRoots[i];

            if (root == null)
            {
                registeredRoots.RemoveAt(i);
                continue;
            }

            if (TryBuildBallInfo(root, out BallInfo info))
                activeBalls.Add(info);
        }
    }

    private static bool TryBuildBallInfo(GameObject root, out BallInfo info)
    {
        info = default;

        if (root == null || !root.activeInHierarchy)
            return false;

        if (!int.TryParse(root.tag, out _))
            return false;

        Collider collider = GetEnabledCollider(root);

        if (collider == null)
            return false;

        Rigidbody body = root.GetComponentInChildren<Rigidbody>();
        info = new BallInfo(root, body, collider);
        return true;
    }

    private static bool TryGetBallRoot(GameObject candidate, out GameObject root)
    {
        root = null;

        if (candidate == null)
            return false;

        Transform current = candidate.transform;

        while (current != null)
        {
            if (int.TryParse(current.tag, out _))
            {
                root = current.gameObject;
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Collider GetEnabledCollider(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col.enabled && col.gameObject.activeInHierarchy)
                return col;
        }

        return null;
    }
}
