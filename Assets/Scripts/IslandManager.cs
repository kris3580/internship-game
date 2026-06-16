using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Zenject;

public class IslandManager : MonoBehaviour
{
    [SerializeField] private LayerMask ballLayer;
    [SerializeField] private float touchTolerance = 0.05f;
    [SerializeField] private bool checkAutomatically = true;
    [SerializeField] private float automaticCheckInterval = 0.25f;
    [SerializeField] private GameObject comboTextContainer;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Animator comboAnimator;
    [SerializeField] private string comboAnimationStateName = "Combo";
    [SerializeField] private GameAudioManager audioManager;
    [SerializeField] private CameraShake comboCameraShake;
    [SerializeField] private float islandWaveDelay = 0.06f;
    [SerializeField] private float islandBallDelay = 0.025f;

    private float nextAutomaticCheckTime;
    private bool warnedMissingLayer;
    private int comboCount;
    private bool shotActive;
    private bool shotClearedIsland;
    private GameObject lastShotBall;
    private Vector3 lastImpactPosition;
    private bool hasImpactPosition;
    private readonly HashSet<GameObject> clearingBalls = new();
    private IAudioService audioService;
    [InjectOptional] private IAudioService injectedAudioService;
    [InjectOptional] private IBallRegistry ballRegistry;
    [InjectOptional] private CameraShake injectedComboCameraShake;

    public event Action<IReadOnlyList<GameObject>> IslandCleared;

    private void Awake()
    {
        if (comboCameraShake == null)
            comboCameraShake = injectedComboCameraShake;

        audioService = injectedAudioService ?? audioManager;
    }

    public void RegisterBallShot(GameObject shotBall)
    {
        if (shotActive && !shotClearedIsland)
            ResetCombo();

        shotActive = true;
        shotClearedIsland = false;
        lastShotBall = shotBall;

        if (shotBall != null)
        {
            lastImpactPosition = shotBall.transform.position;
            hasImpactPosition = true;
        }
    }

    public void RegisterBallShot()
    {
        RegisterBallShot(null);
    }

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
        Dictionary<GameObject, Collider> balls = new();

        if (ballRegistry != null)
        {
            foreach (BallInfo info in ballRegistry.GetActiveBalls())
                AddBallIfValid(info.Collider, activeBallMask, balls);

            return balls;
        }

        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (Collider col in allColliders)
            AddBallIfValid(col, activeBallMask, balls);

        return balls;
    }

    private void AddBallIfValid(
        Collider col,
        int activeBallMask,
        Dictionary<GameObject, Collider> balls)
    {
        if (!TryGetBall(col, activeBallMask, out GameObject ball))
            return;

        if (!balls.ContainsKey(ball))
            balls.Add(ball, col);
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

            ClearIsland(island, ball.tag, requiredIslandSize, balls, activeBallMask);
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

                if (!balls.ContainsKey(other))
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
        int requiredIslandSize,
        Dictionary<GameObject, Collider> balls,
        int activeBallMask)
    {
        Debug.Log($"Island of {islandTag}: {island.Count}/{requiredIslandSize}");
        RegisterIslandClear();
        IslandCleared?.Invoke(island);

        foreach (GameObject member in island)
            clearingBalls.Add(member);

        StartCoroutine(ClearIslandSpread(
            new List<GameObject>(island),
            BuildSpreadWaves(island, balls, activeBallMask)
        ));
    }

    private void RegisterIslandClear()
    {
        if (!shotActive)
            shotActive = true;

        shotClearedIsland = true;
        comboCount++;

        if (comboCount >= 2)
            ShowCombo(comboCount);
    }

    private void ShowCombo(int count)
    {
        if (comboTextContainer != null && !comboTextContainer.activeSelf)
            comboTextContainer.SetActive(true);

        if (comboText != null)
        {
            if (!comboText.gameObject.activeSelf)
                comboText.gameObject.SetActive(true);

            comboText.text = $"Combo!! x{count}";
            comboText.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.85f, 1f);
        }

        if (comboAnimator != null && !string.IsNullOrWhiteSpace(comboAnimationStateName))
            comboAnimator.Play(comboAnimationStateName, 0, 0f);

        Vector3 feedbackPosition = comboTextContainer != null
            ? comboTextContainer.transform.position
            : transform.position;

        audioService?.PlayCombo(feedbackPosition, count);
        comboCameraShake?.Shake();
    }

    private void ResetCombo()
    {
        comboCount = 0;
        shotActive = false;
        shotClearedIsland = false;
        lastShotBall = null;
        hasImpactPosition = false;
    }

    private List<List<GameObject>> BuildSpreadWaves(
        IReadOnlyList<GameObject> island,
        Dictionary<GameObject, Collider> balls,
        int activeBallMask)
    {
        GameObject origin = GetSpreadOrigin(island);
        Dictionary<GameObject, int> distances = new();
        Queue<GameObject> queue = new();

        distances[origin] = 0;
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            GameObject current = queue.Dequeue();

            if (!balls.TryGetValue(current, out Collider currentCollider))
                continue;

            foreach (GameObject candidate in island)
            {
                if (distances.ContainsKey(candidate))
                    continue;

                if (!balls.TryGetValue(candidate, out Collider candidateCollider))
                    continue;

                if (!AreTouching(currentCollider, candidateCollider))
                    continue;

                distances[candidate] = distances[current] + 1;
                queue.Enqueue(candidate);
            }
        }

        List<List<GameObject>> waves = new();

        foreach (GameObject member in island)
        {
            if (!distances.TryGetValue(member, out int distance))
                distance = 0;

            while (waves.Count <= distance)
                waves.Add(new List<GameObject>());

            waves[distance].Add(member);
        }

        foreach (List<GameObject> wave in waves)
            wave.Sort((first, second) => GetSortPosition(first).z.CompareTo(GetSortPosition(second).z));

        return waves;
    }

    private GameObject GetSpreadOrigin(IReadOnlyList<GameObject> island)
    {
        if (lastShotBall != null)
        {
            foreach (GameObject member in island)
            {
                if (member == lastShotBall)
                    return member;
            }
        }

        if (!hasImpactPosition)
            return island[0];

        GameObject closest = island[0];
        float closestDistance = float.MaxValue;

        foreach (GameObject member in island)
        {
            if (member == null)
                continue;

            float distance = (member.transform.position - lastImpactPosition).sqrMagnitude;

            if (distance < closestDistance)
            {
                closest = member;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private Vector3 GetSortPosition(GameObject ball)
    {
        return ball != null ? ball.transform.position : Vector3.zero;
    }

    private IEnumerator ClearIslandSpread(List<GameObject> island, List<List<GameObject>> waves)
    {
        int noteIndex = 0;

        foreach (List<GameObject> wave in waves)
        {
            foreach (GameObject member in wave)
            {
                if (member == null)
                    continue;

                audioService?.PlayBallDisappear(member.transform.position, noteIndex);
                clearingBalls.Remove(member);
                ballRegistry?.Unregister(member);
                Destroy(member);
                noteIndex++;

                if (islandBallDelay > 0f)
                    yield return new WaitForSeconds(islandBallDelay);
            }

            if (islandWaveDelay > 0f)
                yield return new WaitForSeconds(islandWaveDelay);
        }

        foreach (GameObject member in island)
            clearingBalls.Remove(member);
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

        if (clearingBalls.Contains(ball))
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
        islandWaveDelay = Mathf.Max(0f, islandWaveDelay);
        islandBallDelay = Mathf.Max(0f, islandBallDelay);
    }
}
