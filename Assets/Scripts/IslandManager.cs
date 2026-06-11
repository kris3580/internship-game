using System;
using System.Collections.Generic;
using UnityEngine;

public class IslandManager : MonoBehaviour
{
    [SerializeField] private LayerMask ballLayer;
    [SerializeField] private float touchTolerance = 0.05f;
    [SerializeField] private bool checkAutomatically = true;
    [SerializeField] private float automaticCheckInterval = 0.25f;

    private float nextAutomaticCheckTime;
    private bool warnedMissingLayer;

    public event Action<IReadOnlyList<GameObject>> IslandCleared;

    private void FixedUpdate()
    {
        if (!checkAutomatically || Time.time < nextAutomaticCheckTime)
            return;

        nextAutomaticCheckTime = Time.time + automaticCheckInterval;
        CheckIslands();
    }

    /// <summary>
    /// Finds all connected same-tag ball islands and clears islands whose size is at least their numeric tag.
    /// </summary>
    [ContextMenu("Check Islands")]
    public void CheckIslands()
    {
        int activeBallMask = GetActiveBallMask();
        Dictionary<GameObject, Collider> balls = CollectBalls(activeBallMask);

        ResolveIslands(balls, activeBallMask);
    }

    private Dictionary<GameObject, Collider> CollectBalls(int activeBallMask)
    {
        Collider[] allColliders = FindObjectsByType<Collider>(
            FindObjectsSortMode.None
        );

        Dictionary<GameObject, Collider> balls = new();

        foreach (Collider col in allColliders)
        {
            if (!TryGetBall(col, activeBallMask, out GameObject ball))
                continue;

            if (!balls.ContainsKey(ball))
                balls.Add(ball, col);
        }

        return balls;
    }

    private void ResolveIslands(
        Dictionary<GameObject, Collider> balls,
        int activeBallMask)
    {
        HashSet<GameObject> visited = new();

        foreach (KeyValuePair<GameObject, Collider> pair in balls)
        {
            GameObject ball = pair.Key;

            if (visited.Contains(ball))
                continue;

            List<GameObject> island = FindIsland(
                ball,
                balls,
                activeBallMask,
                visited
            );

            if (!TryGetRequiredIslandSize(ball, out int requiredIslandSize))
                continue;

            if (island.Count < requiredIslandSize)
                continue;

            ClearIsland(island, ball.tag, requiredIslandSize);
        }
    }

    private List<GameObject> FindIsland(
        GameObject start,
        Dictionary<GameObject, Collider> balls,
        int activeBallMask,
        HashSet<GameObject> visited)
    {
        List<GameObject> island = new();
        Queue<GameObject> queue = new();

        string targetTag = start.tag;

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            GameObject current = queue.Dequeue();
            island.Add(current);

            Collider currentCollider = balls[current];
            Bounds searchBounds = currentCollider.bounds;
            searchBounds.Expand(touchTolerance * 2f);

            Collider[] neighbors = Physics.OverlapBox(
                searchBounds.center,
                searchBounds.extents,
                Quaternion.identity,
                activeBallMask,
                QueryTriggerInteraction.Collide
            );

            foreach (Collider neighbor in neighbors)
            {
                if (!TryGetBall(neighbor, activeBallMask, out GameObject other))
                    continue;

                if (other == current)
                    continue;

                if (!other.CompareTag(targetTag))
                    continue;

                if (visited.Contains(other))
                    continue;

                if (!AreTouching(currentCollider, neighbor))
                    continue;

                visited.Add(other);
                queue.Enqueue(other);
            }
        }

        return island;
    }

    private void ClearIsland(
        IReadOnlyList<GameObject> island,
        string islandTag,
        int requiredIslandSize)
    {
        Debug.Log($"Island of {islandTag}: {island.Count}/{requiredIslandSize}");
        IslandCleared?.Invoke(island);

        foreach (GameObject member in island)
            Destroy(member);
    }

    private bool TryGetBall(Collider col, int activeBallMask, out GameObject ball)
    {
        ball = null;

        if (col == null)
            return false;

        if (!col.enabled || !col.gameObject.activeInHierarchy)
            return false;

        Transform current = col.transform;

        while (current != null)
        {
            if (!current.CompareTag("Untagged"))
            {
                ball = current.gameObject;
                break;
            }

            current = current.parent;
        }

        if (ball == null)
            return false;

        bool colliderIsOnBallLayer = IsInMask(col.gameObject.layer, activeBallMask);
        bool ballIsOnBallLayer = IsInMask(ball.layer, activeBallMask);

        return colliderIsOnBallLayer || ballIsOnBallLayer;
    }

    private bool AreTouching(Collider first, Collider second)
    {
        Bounds expandedFirst = first.bounds;
        expandedFirst.Expand(touchTolerance * 2f);

        if (!expandedFirst.Intersects(second.bounds))
            return false;

        Vector3 pointOnFirst = first.ClosestPoint(second.bounds.center);
        Vector3 pointOnSecond = second.ClosestPoint(pointOnFirst);

        return (pointOnFirst - pointOnSecond).sqrMagnitude <=
            touchTolerance * touchTolerance;
    }

    private int GetActiveBallMask()
    {
        if (ballLayer.value != 0)
            return ballLayer.value;

        if (!warnedMissingLayer)
        {
            warnedMissingLayer = true;
            Debug.LogWarning(
                "IslandManager has no ball layer mask assigned. " +
                "It will check all layers and filter by numeric tag.",
                this
            );
        }

        return Physics.AllLayers;
    }

    private bool IsInMask(int layer, int mask)
    {
        return (mask & (1 << layer)) != 0;
    }

    private bool TryGetRequiredIslandSize(GameObject ball, out int requiredIslandSize)
    {
        if (int.TryParse(ball.tag, out requiredIslandSize) && requiredIslandSize > 0)
            return true;

        Debug.LogWarning(
            $"Ball '{ball.name}' has tag '{ball.tag}', but island tags must be numbers like 3, 4, or 5.",
            ball
        );

        return false;
    }

    private void OnValidate()
    {
        touchTolerance = Mathf.Max(0f, touchTolerance);
        automaticCheckInterval = Mathf.Max(0.02f, automaticCheckInterval);
    }
}
