using System.Collections.Generic;
using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    private struct BallInfo
    {
        public GameObject Root;
        public Rigidbody Rigidbody;
        public Collider Collider;
    }

    private struct PairState
    {
        public bool Touching;
        public float LastPlayedTime;
    }

    private static readonly int[] NoteOffsets = { 0, 2, 4, 7, 12 };

    public static GameAudioManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip ballCollisionClip;
    [SerializeField] private AudioClip poolStickHitClip;
    [SerializeField] private AudioClip ballPlaceClip;
    [SerializeField] private AudioClip ballDisappearClip;
    [SerializeField] private AudioClip comboClip;

    [Header("Random Pitch")]
    [SerializeField] private Vector2 ballCollisionPitchRange = new(0.92f, 1.08f);
    [SerializeField] private Vector2 poolStickHitPitchRange = new(0.95f, 1.05f);
    [SerializeField] private Vector2 ballPlacePitchRange = new(0.96f, 1.08f);

    [Header("Note Pitch")]
    [SerializeField] private int disappearStartingOctave = 4;
    [SerializeField] private int comboStartingOctave = 4;

    [Header("Volumes")]
    [SerializeField] private float ballCollisionVolume = 0.65f;
    [SerializeField] private float poolStickHitVolume = 0.8f;
    [SerializeField] private float ballPlaceVolume = 0.7f;
    [SerializeField] private float ballDisappearVolume = 0.75f;
    [SerializeField] private float comboVolume = 0.85f;

    [Header("Ball Collision Detection")]
    [SerializeField] private LayerMask ballLayer;
    [SerializeField] private float collisionTouchTolerance = 0.03f;
    [SerializeField] private float minimumCollisionRelativeSpeed = 0.8f;
    [SerializeField] private float collisionCooldown = 0.08f;
    [SerializeField] private int maxCollisionSoundsPerStep = 4;

    [Header("Audio Sources")]
    [SerializeField] private int sourcePoolSize = 8;
    [SerializeField] private float spatialBlend = 0.35f;

    private readonly List<AudioSource> sources = new();
    private readonly List<BallInfo> balls = new();
    private readonly Dictionary<long, PairState> pairStates = new();
    private int nextSourceIndex;

    private void Awake()
    {
        Instance = this;
        EnsureSources();
    }

    private void FixedUpdate()
    {
        PlayBallCollisionSounds();
    }

    public void PlayPoolStickHit(Vector3 position)
    {
        PlayAt(position, poolStickHitClip, poolStickHitVolume, RandomPitch(poolStickHitPitchRange));
    }

    public void PlayBallPlace(Vector3 position)
    {
        PlayAt(position, ballPlaceClip, ballPlaceVolume, RandomPitch(ballPlacePitchRange));
    }

    public void PlayBallDisappear(Vector3 position, int sequenceIndex)
    {
        PlayAt(position, ballDisappearClip, ballDisappearVolume, GetNotePitch(disappearStartingOctave, sequenceIndex));
    }

    public void PlayCombo(Vector3 position, int comboCount)
    {
        int sequenceIndex = Mathf.Max(0, comboCount - 2);
        PlayAt(position, comboClip, comboVolume, GetNotePitch(comboStartingOctave, sequenceIndex));
    }

    private void PlayBallCollisionSounds()
    {
        if (ballCollisionClip == null)
            return;

        CollectBalls();

        int soundsPlayed = 0;

        for (int i = 0; i < balls.Count; i++)
        {
            for (int j = i + 1; j < balls.Count; j++)
            {
                BallInfo first = balls[i];
                BallInfo second = balls[j];
                long key = GetPairKey(first.Root, second.Root);
                pairStates.TryGetValue(key, out PairState state);

                bool touching = AreTouching(first.Collider, second.Collider);

                if (touching && !state.Touching)
                {
                    float relativeSpeed = (first.Rigidbody.linearVelocity - second.Rigidbody.linearVelocity).magnitude;

                    if (relativeSpeed >= minimumCollisionRelativeSpeed &&
                        Time.time >= state.LastPlayedTime + collisionCooldown &&
                        soundsPlayed < maxCollisionSoundsPerStep)
                    {
                        Vector3 soundPosition = (first.Root.transform.position + second.Root.transform.position) * 0.5f;
                        PlayAt(soundPosition, ballCollisionClip, ballCollisionVolume, RandomPitch(ballCollisionPitchRange));
                        state.LastPlayedTime = Time.time;
                        soundsPlayed++;
                    }
                }

                state.Touching = touching;
                pairStates[key] = state;
            }
        }
    }

    private void CollectBalls()
    {
        balls.Clear();

        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

        foreach (Rigidbody body in rigidbodies)
        {
            if (body == null || !body.gameObject.activeInHierarchy)
                continue;

            if (!TryGetBallRoot(body.transform, out GameObject root))
                continue;

            Collider ballCollider = GetEnabledCollider(root);

            if (ballCollider == null)
                continue;

            balls.Add(new BallInfo
            {
                Root = root,
                Rigidbody = body,
                Collider = ballCollider
            });
        }
    }

    private bool TryGetBallRoot(Transform start, out GameObject root)
    {
        root = null;
        Transform current = start;

        while (current != null)
        {
            if (int.TryParse(current.tag, out _))
            {
                root = current.gameObject;
                break;
            }

            current = current.parent;
        }

        if (root == null)
            return false;

        return ballLayer.value == 0 || IsInMask(root.layer, ballLayer.value);
    }

    private Collider GetEnabledCollider(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col.enabled && col.gameObject.activeInHierarchy)
                return col;
        }

        return null;
    }

    private bool AreTouching(Collider first, Collider second)
    {
        Bounds expanded = first.bounds;
        expanded.Expand(collisionTouchTolerance * 2f);

        if (!expanded.Intersects(second.bounds))
            return false;

        Vector3 pointOnFirst = first.ClosestPoint(second.bounds.center);
        Vector3 pointOnSecond = second.ClosestPoint(pointOnFirst);

        return (pointOnFirst - pointOnSecond).sqrMagnitude <=
            collisionTouchTolerance * collisionTouchTolerance;
    }

    private void PlayAt(Vector3 position, AudioClip clip, float volume, float pitch)
    {
        if (clip == null)
            return;

        AudioSource source = GetSource();
        source.transform.position = position;
        source.pitch = pitch;
        source.spatialBlend = spatialBlend;
        source.PlayOneShot(clip, volume);
    }

    private AudioSource GetSource()
    {
        EnsureSources();

        for (int i = 0; i < sources.Count; i++)
        {
            nextSourceIndex = (nextSourceIndex + 1) % sources.Count;

            if (!sources[nextSourceIndex].isPlaying)
                return sources[nextSourceIndex];
        }

        nextSourceIndex = (nextSourceIndex + 1) % sources.Count;
        return sources[nextSourceIndex];
    }

    private void EnsureSources()
    {
        sourcePoolSize = Mathf.Max(1, sourcePoolSize);

        while (sources.Count < sourcePoolSize)
        {
            GameObject sourceObject = new($"SfxSource {sources.Count + 1}");
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            sources.Add(source);
        }
    }

    private float RandomPitch(Vector2 range)
    {
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max);
    }

    private float GetNotePitch(int startingOctave, int sequenceIndex)
    {
        int octaveOffset = startingOctave - 4;
        int noteIndex = Mathf.Max(0, sequenceIndex);
        int semitones = octaveOffset * 12 +
            NoteOffsets[noteIndex % NoteOffsets.Length] +
            (noteIndex / NoteOffsets.Length) * 12;

        return Mathf.Pow(2f, semitones / 12f);
    }

    private static long GetPairKey(GameObject first, GameObject second)
    {
        int firstId = first.GetInstanceID();
        int secondId = second.GetInstanceID();

        if (firstId > secondId)
        {
            int temp = firstId;
            firstId = secondId;
            secondId = temp;
        }

        return ((long)firstId << 32) ^ (uint)secondId;
    }

    private bool IsInMask(int layer, int mask)
    {
        return (mask & (1 << layer)) != 0;
    }

    private void OnValidate()
    {
        sourcePoolSize = Mathf.Max(1, sourcePoolSize);
        collisionTouchTolerance = Mathf.Max(0f, collisionTouchTolerance);
        minimumCollisionRelativeSpeed = Mathf.Max(0f, minimumCollisionRelativeSpeed);
        collisionCooldown = Mathf.Max(0f, collisionCooldown);
        maxCollisionSoundsPerStep = Mathf.Max(1, maxCollisionSoundsPerStep);
        spatialBlend = Mathf.Clamp01(spatialBlend);
        ballCollisionVolume = Mathf.Max(0f, ballCollisionVolume);
        poolStickHitVolume = Mathf.Max(0f, poolStickHitVolume);
        ballPlaceVolume = Mathf.Max(0f, ballPlaceVolume);
        ballDisappearVolume = Mathf.Max(0f, ballDisappearVolume);
        comboVolume = Mathf.Max(0f, comboVolume);
    }
}
