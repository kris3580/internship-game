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
    [Header("Looping Audio")]
    [SerializeField] private AudioClip fireLoopClip;
    [SerializeField] private AudioClip earthLoopClip;
    [SerializeField] private AudioClip waterLoopClip;
    [SerializeField] private AudioClip windLoopClip;
    [SerializeField] private float fireLoopVolume = 0.45f;
    [SerializeField] private float earthLoopVolume = 0.35f;
    [SerializeField] private float waterLoopVolume = 0.4f;
    [SerializeField] private float windLoopVolume = 0.35f;

    private IslandManager islandManager;
    private Rigidbody body;
    private AudioSource loopSource;
    private float stoppedTimer;
    private bool consumed;

    private void Awake()
    {
        islandManager = FindFirstObjectByType<IslandManager>();
        body = GetComponent<Rigidbody>();
        EnsureLoopSource();

        if (CompareTag(FireTag))
            SetCollidersAsTriggers();
    }

    private void OnEnable()
    {
        consumed = false;
        stoppedTimer = 0f;

        if (body == null)
            body = GetComponent<Rigidbody>();

        StartLoopSound();

        if (CompareTag(FireTag))
            SetCollidersAsTriggers();
    }

    private void OnDisable()
    {
        if (loopSource != null)
            loopSource.Stop();
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
        if (loopSource != null)
            loopSource.Stop();
        islandManager?.DestroyBall(gameObject);
    }

    private void SetCollidersAsTriggers()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
            col.isTrigger = true;
    }

    private void EnsureLoopSource()
    {
        if (loopSource != null)
            return;

        loopSource = GetComponent<AudioSource>();

        if (loopSource == null)
            loopSource = gameObject.AddComponent<AudioSource>();

        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0.35f;
    }

    private void StartLoopSound()
    {
        EnsureLoopSource();

        AudioClip clip = null;
        float volume = 1f;

        if (CompareTag(FireTag))
        {
            clip = fireLoopClip;
            volume = fireLoopVolume;
        }
        else if (CompareTag(EarthTag))
        {
            clip = earthLoopClip;
            volume = earthLoopVolume;
        }
        else if (CompareTag(WaterTag))
        {
            clip = waterLoopClip;
            volume = waterLoopVolume;
        }
        else if (CompareTag(WindTag))
        {
            clip = windLoopClip;
            volume = windLoopVolume;
        }

        if (clip == null || !AudioPreferences.SoundEnabled)
            return;

        loopSource.clip = clip;
        loopSource.volume = Mathf.Max(0f, volume);
        loopSource.Play();
    }

    private void OnValidate()
    {
        stoppedSpeed = Mathf.Max(0f, stoppedSpeed);
        stoppedDuration = Mathf.Max(0f, stoppedDuration);
        earthSpawnCount = Mathf.Max(0, earthSpawnCount);
        earthSpawnRadius = Mathf.Max(0f, earthSpawnRadius);
        fireLoopVolume = Mathf.Max(0f, fireLoopVolume);
        earthLoopVolume = Mathf.Max(0f, earthLoopVolume);
        waterLoopVolume = Mathf.Max(0f, waterLoopVolume);
        windLoopVolume = Mathf.Max(0f, windLoopVolume);
    }
}
