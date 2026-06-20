using UnityEngine;

public class PowerUpBall : MonoBehaviour
{
    private const string FireTag = "fire";
    private const string EarthTag = "earth";
    private const string WaterTag = "water";
    private const string WindTag = "wind";

    [SerializeField] private float stoppedSpeed = 0.08f;
    [SerializeField] private float stoppedDuration = 0.35f;
    [SerializeField] private int earthSpawnCount = 5;
    [SerializeField] private float earthSpawnRadius = 0.75f;

    private IslandManager islandManager;
    private Rigidbody body;
    private float stoppedTimer;
    private bool consumed;

    private void Awake()
    {
        islandManager = FindFirstObjectByType<IslandManager>();
        body = GetComponent<Rigidbody>();

        if (CompareTag(FireTag))
            SetCollidersAsTriggers();
    }

    private void OnEnable()
    {
        consumed = false;
        stoppedTimer = 0f;

        if (body == null)
            body = GetComponent<Rigidbody>();

        if (CompareTag(FireTag))
            SetCollidersAsTriggers();
    }

    private void FixedUpdate()
    {
        if (!CompareTag(FireTag) || consumed || body == null)
            return;

        if (body.isKinematic || !body.useGravity)
        {
            stoppedTimer = 0f;
            return;
        }

        if (body.linearVelocity.sqrMagnitude <= stoppedSpeed * stoppedSpeed)
            stoppedTimer += Time.fixedDeltaTime;
        else
            stoppedTimer = 0f;

        if (stoppedTimer >= stoppedDuration)
            ConsumeSelf();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyTo(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryApplyTo(other);
    }

    private void TryApplyTo(Collider other)
    {
        if (consumed || other == null)
            return;

        if (IsBottomBorder(other.transform))
        {
            ConsumeSelf();
            return;
        }

        GameObject numericBall = FindNumericBall(other.transform);

        if (numericBall == null || numericBall == gameObject)
            return;

        if (islandManager == null)
            islandManager = FindFirstObjectByType<IslandManager>();

        if (CompareTag(FireTag))
        {
            islandManager?.DestroyBall(numericBall);
            return;
        }

        consumed = true;

        if (CompareTag(EarthTag))
        {
            islandManager?.SpawnCopiesAround(numericBall, earthSpawnCount, earthSpawnRadius);
            ConsumeSelf();
            return;
        }

        if (CompareTag(WaterTag))
        {
            islandManager?.ClearIslandContaining(numericBall);
            ConsumeSelf();
            return;
        }

        if (CompareTag(WindTag))
        {
            islandManager?.ClearAllBallsWithTag(numericBall.tag);
            ConsumeSelf();
        }
    }

    private GameObject FindNumericBall(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (int.TryParse(current.tag, out _))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private bool IsBottomBorder(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (current.name == "Bottom")
                return true;

            current = current.parent;
        }

        return false;
    }

    private void ConsumeSelf()
    {
        consumed = true;
        islandManager?.DestroyBall(gameObject);
    }

    private void SetCollidersAsTriggers()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
            col.isTrigger = true;
    }
}
