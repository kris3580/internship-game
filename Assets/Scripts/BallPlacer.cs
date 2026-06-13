using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BallPlacer : MonoBehaviour
{
    private const int MousePointerId = -1;

    [SerializeField] private GameObject[] ballPrefabs;
    [SerializeField] private Vector3 spawnPosition = new(0f, 11.07f, -1.7f);
    [SerializeField] private Vector3 spawnEulerAngles = Vector3.zero;
    [SerializeField] private Transform poolStick;
    [SerializeField] private Vector3 poolStickOffset = new(-0.15f, 0.73f, -0.03f);
    [SerializeField] private Animator poolAnimator;
    [SerializeField] private string hitAnimationStateName = "Hit";
    [SerializeField] private float dropDelay = 0.15f;
    [SerializeField] private Vector3 dropImpulse = new(0f, -1.5f, 0f);
    [SerializeField] private ForceMode dropImpulseMode = ForceMode.Impulse;
    [SerializeField] private float minZ = -4.5f;
    [SerializeField] private float maxZ = 4.5f;
    [SerializeField] private float nextBallDelay = 0.35f;
    [SerializeField] private bool spawnOnStart = true;

    private GameObject pendingBall;
    private Rigidbody pendingRigidbody;
    private Collider[] pendingColliders = new Collider[0];
    private Coroutine spawnRoutine;
    private Coroutine dropRoutine;
    private bool dropQueued;
    private bool mouseStartedOverUi;
    private int uiBlockedFingerId = -1;

    private void Awake()
    {
        EnsureEventSystem();
    }

    private void Start()
    {
        if (spawnOnStart)
            SpawnNextBall();
    }

    private void Update()
    {
        if (pendingBall == null || dropQueued)
            return;

        if (TryReadPointer(out Vector2 screenPosition, out bool shouldDrop))
            MovePendingBall(screenPosition);

        if (shouldDrop || Input.GetKeyDown(KeyCode.Space))
            DropPendingBall();
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

        spawnRoutine = null;
        dropRoutine = null;
        dropQueued = false;
    }

    /// <summary>
    /// Spawns the next pending ball at the configured height with its X coordinate locked to zero.
    /// </summary>
    public void SpawnNextBall()
    {
        if (pendingBall != null)
            return;

        if (ballPrefabs == null || ballPrefabs.Length == 0)
        {
            Debug.LogWarning("BallPlacer has no ball prefabs assigned.", this);
            return;
        }

        GameObject prefab = ballPrefabs[UnityEngine.Random.Range(0, ballPrefabs.Length)];

        if (prefab == null)
        {
            Debug.LogWarning("BallPlacer tried to spawn an empty prefab slot.", this);
            return;
        }

        pendingBall = Instantiate(prefab, GetSpawnPosition(spawnPosition.z), Quaternion.Euler(spawnEulerAngles));
        pendingRigidbody = pendingBall.GetComponent<Rigidbody>();
        pendingColliders = pendingBall.GetComponentsInChildren<Collider>();
        SetPendingColliders(false);

        if (pendingRigidbody == null)
        {
            Debug.LogWarning($"Pending ball '{pendingBall.name}' needs a Rigidbody to be placed.", pendingBall);
            return;
        }

        pendingRigidbody.linearVelocity = Vector3.zero;
        pendingRigidbody.angularVelocity = Vector3.zero;
        pendingRigidbody.useGravity = false;
        pendingRigidbody.isKinematic = true;
        pendingRigidbody.detectCollisions = false;
        MovePoolStickWithPendingBall();
    }

    /// <summary>
    /// Releases the pending ball into physics and schedules the next pending ball.
    /// </summary>
    public void DropPendingBall()
    {
        if (pendingBall == null || dropQueued)
            return;

        dropQueued = true;
        PlayHitAnimation();

        if (dropDelay <= 0f)
        {
            ReleasePendingBall();
            return;
        }

        dropRoutine = StartCoroutine(DropAfterDelay());
    }

    private void ReleasePendingBall()
    {
        SetPendingColliders(true);

        if (pendingRigidbody != null)
        {
            pendingRigidbody.isKinematic = false;
            pendingRigidbody.useGravity = true;
            pendingRigidbody.detectCollisions = true;
            pendingRigidbody.linearVelocity = Vector3.zero;
            pendingRigidbody.angularVelocity = Vector3.zero;
            pendingRigidbody.AddForce(dropImpulse, dropImpulseMode);
        }

        pendingBall = null;
        pendingRigidbody = null;
        pendingColliders = new Collider[0];
        dropRoutine = null;
        dropQueued = false;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnAfterDelay());
    }

    private bool TryReadPointer(out Vector2 screenPosition, out bool shouldDrop)
    {
        shouldDrop = false;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
                uiBlockedFingerId = IsPointerOverUi(touch.fingerId) ? touch.fingerId : -1;

            bool touchIsBlocked = uiBlockedFingerId == touch.fingerId || IsPointerOverUi(touch.fingerId);

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                if (uiBlockedFingerId == touch.fingerId)
                    uiBlockedFingerId = -1;
            }

            if (touchIsBlocked)
                return false;

            shouldDrop = touch.phase == TouchPhase.Ended;
            return touch.phase != TouchPhase.Canceled;
        }

        screenPosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
            mouseStartedOverUi = IsPointerOverUi(MousePointerId);

        bool mouseIsBlocked = mouseStartedOverUi || IsPointerOverUi(MousePointerId);
        shouldDrop = !mouseIsBlocked && Input.GetMouseButtonUp(0);
        bool hasPointer = !mouseIsBlocked && (Input.GetMouseButton(0) || shouldDrop);

        if (Input.GetMouseButtonUp(0))
            mouseStartedOverUi = false;

        return hasPointer;
    }

    private void MovePendingBall(Vector2 screenPosition)
    {
        if (pendingRigidbody == null)
            return;

        Vector3 targetPosition = GetSpawnPosition(GetAxisFromScreen(screenPosition));
        pendingRigidbody.MovePosition(targetPosition);
        MovePoolStickTo(targetPosition);
    }

    private void MovePoolStickWithPendingBall()
    {
        if (poolStick == null || pendingBall == null)
            return;

        MovePoolStickTo(pendingBall.transform.position);
    }

    private void MovePoolStickTo(Vector3 ballPosition)
    {
        if (poolStick == null)
            return;

        poolStick.position = ballPosition + poolStickOffset;
    }

    private void PlayHitAnimation()
    {
        if (poolAnimator == null || string.IsNullOrWhiteSpace(hitAnimationStateName))
            return;

        poolAnimator.Play(hitAnimationStateName, 0, 0f);
    }

    private float GetAxisFromScreen(Vector2 screenPosition)
    {
        if (Screen.width <= 0)
            return spawnPosition.z;

        float screenPercent = Mathf.Clamp01(screenPosition.x / Screen.width);
        return Mathf.Lerp(minZ, maxZ, screenPercent);
    }

    private Vector3 GetSpawnPosition(float z)
    {
        return new Vector3(0f, spawnPosition.y, Mathf.Clamp(z, minZ, maxZ));
    }

    private void SetPendingColliders(bool enabled)
    {
        foreach (Collider pendingCollider in pendingColliders)
            pendingCollider.enabled = enabled;
    }

    private bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId == MousePointerId)
            return EventSystem.current.IsPointerOverGameObject();

        return EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(nextBallDelay);
        spawnRoutine = null;
        SpawnNextBall();
    }

    private IEnumerator DropAfterDelay()
    {
        yield return new WaitForSeconds(dropDelay);
        ReleasePendingBall();
    }

    private void OnValidate()
    {
        if (minZ > maxZ)
        {
            float oldMin = minZ;
            minZ = maxZ;
            maxZ = oldMin;
        }

        nextBallDelay = Mathf.Max(0f, nextBallDelay);
        dropDelay = Mathf.Max(0f, dropDelay);
        spawnPosition.x = 0f;
    }
}
