using System.Collections;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using TMPro;
using Zenject;

public class BallPlacer : MonoBehaviour
{
    private const int MousePointerId = -1;

    [SerializeField] private GameObject[] ballPrefabs;
    [SerializeField] private Vector3 spawnPosition = new(0f, 11.07f, -1.7f);
    [SerializeField] private Vector3 spawnEulerAngles = Vector3.zero;
    [SerializeField] private Transform spawnedBallsParent;
    [SerializeField] private Transform poolStick;
    [SerializeField] private Vector3 poolStickOffset = new(-0.15f, 0.73f, -0.03f);
    [SerializeField] private Animator poolAnimator;
    [SerializeField] private string hitAnimationStateName = "Hit";
    [SerializeField] private float dropDelay = 0.15f;
    [SerializeField] private Vector3 dropImpulse = new(0f, -1.5f, 0f);
    [SerializeField] private ForceMode dropImpulseMode = ForceMode.Impulse;
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private GameAudioManager audioManager;
    [SerializeField] private float minZ = -4.5f;
    [SerializeField] private float maxZ = 4.5f;
    [FormerlySerializedAs("nextBallDelay")]
    [SerializeField] private float nextHitDelay = 0.35f;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool blockPlacementOverUi;
    [SerializeField] private TMP_Text nextBallText;
    [SerializeField] private ParticleSystem hitParticle;

    private GameObject pendingBall;
    private string queuedPowerUpTag;
    private string nextNormalTag;
    private bool pendingBallIsPowerUp;
    private Action queuedPowerUpReleased;
    private Action pendingPowerUpReleased;
    private Rigidbody pendingRigidbody;
    private Collider[] pendingColliders = new Collider[0];
    private Coroutine spawnRoutine;
    private Coroutine dropRoutine;
    private float currentSpawnZ;
    private bool dropQueued;
    private bool mouseStartedOverUi;
    private bool suppressDropUntilPointerReleased;
    private int uiBlockedFingerId = -1;
    private IAudioService audioService;
    [InjectOptional] private IAudioService injectedAudioService;
    [InjectOptional] private IBallRegistry ballRegistry;
    [InjectOptional] private IBallFactory ballFactory;
    [InjectOptional] private IGameStateMachine gameStateMachine;
    [InjectOptional] private IslandManager injectedIslandManager;
    [Inject(Optional = true, Id = GameSceneInstaller.SpawnedBallsParentId)]
    private Transform injectedSpawnedBallsParent;

    private void Awake()
    {
        if (islandManager == null)
            islandManager = injectedIslandManager;

        if (spawnedBallsParent == null)
            spawnedBallsParent = injectedSpawnedBallsParent;

        audioService = injectedAudioService ?? audioManager;

        currentSpawnZ = Mathf.Clamp(spawnPosition.z, minZ, maxZ);
        ballFactory?.RegisterPrefabs(ballPrefabs);

        EnsureEventSystem();
    }

    private void Start()
    {
        if (spawnOnStart)
            SpawnNextBall();
    }

    private void Update()
    {
        if (pendingBall == null || dropQueued || (gameStateMachine != null && !gameStateMachine.IsPlaying))
            return;

        bool shouldDrop = false;

        if (TryReadPointer(out Vector2 screenPosition, out shouldDrop))
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

        string ballTag = GetNextTag();

        if (string.IsNullOrWhiteSpace(ballTag))
        {
            Debug.LogWarning("BallPlacer tried to spawn an empty prefab slot.", this);
            return;
        }

        pendingBall = ballFactory != null
            ? ballFactory.Spawn(ballTag, GetSpawnPosition(currentSpawnZ), Quaternion.Euler(spawnEulerAngles), spawnedBallsParent)
            : Instantiate(GetPrefabForTag(ballTag), GetSpawnPosition(currentSpawnZ), Quaternion.Euler(spawnEulerAngles));

        ParentPendingBallForOrganization();
        pendingRigidbody = pendingBall.GetComponent<Rigidbody>();
        pendingColliders = pendingBall.GetComponentsInChildren<Collider>();
        SetPendingColliders(false);

        if (pendingRigidbody == null)
        {
            Debug.LogWarning($"Pending ball '{pendingBall.name}' needs a Rigidbody to be placed.", pendingBall);
            return;
        }

        pendingRigidbody.isKinematic = false;
        pendingRigidbody.useGravity = false;
        pendingRigidbody.detectCollisions = false;
        pendingRigidbody.linearVelocity = Vector3.zero;
        pendingRigidbody.angularVelocity = Vector3.zero;
        pendingRigidbody.isKinematic = true;
        MovePoolStickWithPendingBall();
        audioService?.PlayBallPlace(pendingBall.transform.position);
        RefreshNextBallText();
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
        audioService?.PlayPoolStickHit(pendingBall.transform.position);

        if (dropDelay <= 0f)
        {
            ReleasePendingBall();
            return;
        }

        dropRoutine = StartCoroutine(DropAfterDelay());
    }

    private void ReleasePendingBall()
    {
        GameObject releasedBall = pendingBall;
        Rigidbody releasedRigidbody = pendingRigidbody;
        bool releasedBallIsPowerUp = pendingBallIsPowerUp;
        Action releasedPowerUpCallback = pendingPowerUpReleased;

        PlayHitParticle();
        SetPendingColliders(true);

        if (releasedRigidbody != null)
        {
            releasedRigidbody.isKinematic = false;
            releasedRigidbody.useGravity = true;
            releasedRigidbody.detectCollisions = true;
            releasedRigidbody.linearVelocity = Vector3.zero;
            releasedRigidbody.angularVelocity = Vector3.zero;
            ballRegistry?.Register(releasedBall);
            islandManager?.RegisterBallShot(releasedBall);
            releasedRigidbody.AddForce(dropImpulse, dropImpulseMode);
        }

        if (releasedBallIsPowerUp)
            releasedPowerUpCallback?.Invoke();

        pendingBall = null;
        pendingRigidbody = null;
        pendingColliders = new Collider[0];
        pendingBallIsPowerUp = false;
        pendingPowerUpReleased = null;
        dropRoutine = null;
        dropQueued = false;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(SpawnAfterDelay());
    }

    private bool TryReadPointer(out Vector2 screenPosition, out bool shouldDrop)
    {
        shouldDrop = false;

        if (suppressDropUntilPointerReleased)
        {
            screenPosition = Input.mousePosition;

            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
                suppressDropUntilPointerReleased = false;

            return false;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            screenPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
                uiBlockedFingerId = blockPlacementOverUi && IsPointerOverUi(touch.fingerId) ? touch.fingerId : -1;

            bool touchIsBlocked = blockPlacementOverUi
                && (uiBlockedFingerId == touch.fingerId || IsPointerOverUi(touch.fingerId));

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
            mouseStartedOverUi = blockPlacementOverUi && IsPointerOverUi(MousePointerId);

        bool mouseIsBlocked = blockPlacementOverUi
            && (mouseStartedOverUi || IsPointerOverUi(MousePointerId));
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

        currentSpawnZ = targetPosition.z;
        pendingRigidbody.position = targetPosition;

        if (pendingBall != null)
            pendingBall.transform.position = targetPosition;

        MovePoolStickTo(targetPosition);
    }

    private void MovePoolStickWithPendingBall()
    {
        if (poolStick == null || pendingBall == null)
            return;

        MovePoolStickTo(pendingBall.transform.position);
    }

    private void ParentPendingBallForOrganization()
    {
        if (spawnedBallsParent == null || pendingBall == null)
            return;

        pendingBall.transform.SetParent(spawnedBallsParent, true);
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

    private void PlayHitParticle()
    {
        if (hitParticle == null)
            return;

        if (!hitParticle.gameObject.activeSelf)
            hitParticle.gameObject.SetActive(true);

        hitParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        hitParticle.Play(true);
    }

    public bool QueuePowerUp(string powerUpTag, Action onReleased = null)
    {
        if (string.IsNullOrWhiteSpace(powerUpTag) || dropQueued || !string.IsNullOrWhiteSpace(queuedPowerUpTag))
            return false;

        if (pendingBall != null)
        {
            ballRegistry?.Unregister(pendingBall);
            DespawnBall(pendingBall);
            pendingBall = null;
            pendingRigidbody = null;
            pendingColliders = new Collider[0];
        }

        queuedPowerUpTag = powerUpTag;
        queuedPowerUpReleased = onReleased;
        suppressDropUntilPointerReleased = true;
        SpawnNextBall();
        return true;
    }

    public bool TryGetPrefabForTag(string ballTag, out GameObject prefab)
    {
        prefab = null;

        if (ballPrefabs == null)
            return false;

        foreach (GameObject candidate in ballPrefabs)
        {
            if (candidate != null && candidate.CompareTag(ballTag))
            {
                prefab = candidate;
                return true;
            }
        }

        return false;
    }

    private string GetNextTag()
    {
        if (!string.IsNullOrWhiteSpace(queuedPowerUpTag))
        {
            string powerUp = queuedPowerUpTag;
            queuedPowerUpTag = null;
            pendingBallIsPowerUp = true;
            pendingPowerUpReleased = queuedPowerUpReleased;
            queuedPowerUpReleased = null;
            return powerUp;
        }

        pendingBallIsPowerUp = false;
        pendingPowerUpReleased = null;

        if (string.IsNullOrWhiteSpace(nextNormalTag))
            nextNormalTag = PickNormalTag();

        string tag = nextNormalTag;
        nextNormalTag = PickNormalTag();
        return tag;
    }

    private string PickNormalTag()
    {
        if (ballFactory != null)
            return ballFactory.GetRandomNormalTag();

        if (ballPrefabs == null || ballPrefabs.Length == 0)
            return null;

        GameObject prefab = ballPrefabs[UnityEngine.Random.Range(0, ballPrefabs.Length)];
        return prefab != null ? prefab.tag : null;
    }

    private void RefreshNextBallText()
    {
        if (nextBallText == null)
            nextBallText = FindTextByName("NextBallText");

        if (nextBallText == null)
            return;

        if (string.IsNullOrWhiteSpace(nextNormalTag))
            nextNormalTag = PickNormalTag();

        nextBallText.text = nextNormalTag ?? string.Empty;
    }

    private GameObject GetPrefabForTag(string ballTag)
    {
        TryGetPrefabForTag(ballTag, out GameObject prefab);
        return prefab;
    }

    private void DespawnBall(GameObject ball)
    {
        if (ballFactory != null)
            ballFactory.Despawn(ball);
        else
            Destroy(ball);
    }

    private TMP_Text FindTextByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private float GetAxisFromScreen(Vector2 screenPosition)
    {
        if (Screen.width <= 0)
            return currentSpawnZ;

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
        yield return new WaitForSeconds(nextHitDelay);
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

        nextHitDelay = Mathf.Max(0f, nextHitDelay);
        dropDelay = Mathf.Max(0f, dropDelay);
        spawnPosition.x = 0f;
    }
}
