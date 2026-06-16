using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlaylistPlayer : MonoBehaviour
{
    public static MusicPlaylistPlayer Instance { get; private set; }

    [SerializeField] private AudioClip[] playlist;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool shuffle;
    [SerializeField] private float volume = 0.45f;
    [SerializeField] private float endTolerance = 0.15f;

    private AudioSource source;
    private int currentIndex = -1;
    private bool currentClipStarted;
    private bool applicationSuspended;
    private bool resumeAfterSuspension;
    private int suspendedTimeSamples;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.loop = false;
        source.playOnAwake = false;
        source.volume = volume;

        if (playOnAwake)
            PlayNext();
    }

    private void Update()
    {
        if (playlist == null || playlist.Length == 0)
            return;

        if (source == null || applicationSuspended)
            return;

        if (source.isPlaying)
        {
            currentClipStarted = true;
            return;
        }

        if (source.clip == null || HasCurrentClipFinished())
        {
            PlayNext();
            return;
        }

        ResumeCurrentClip(source.timeSamples);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        SetApplicationSuspended(pauseStatus);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetApplicationSuspended(!hasFocus);
    }

    private void PlayNext()
    {
        if (playlist == null || playlist.Length == 0)
            return;

        currentIndex = shuffle
            ? Random.Range(0, playlist.Length)
            : (currentIndex + 1) % playlist.Length;

        source.clip = playlist[currentIndex];
        source.volume = volume;
        source.Play();
        currentClipStarted = source.clip != null;
    }

    private bool HasCurrentClipFinished()
    {
        if (source.clip == null)
            return true;

        if (!currentClipStarted)
            return false;

        int sampleTolerance = Mathf.CeilToInt(source.clip.frequency * endTolerance);
        return source.time >= source.clip.length - endTolerance
            || source.timeSamples >= source.clip.samples - sampleTolerance;
    }

    private void SetApplicationSuspended(bool suspended)
    {
        if (source == null || applicationSuspended == suspended)
            return;

        applicationSuspended = suspended;

        if (suspended)
        {
            resumeAfterSuspension = source.clip != null && source.isPlaying;
            suspendedTimeSamples = source.clip != null ? source.timeSamples : 0;
            source.Pause();
            return;
        }

        bool shouldResume = resumeAfterSuspension;
        resumeAfterSuspension = false;

        if (!shouldResume || source.clip == null || HasCurrentClipFinished())
            return;

        ResumeCurrentClip(suspendedTimeSamples);
    }

    private void ResumeCurrentClip(int timeSamples)
    {
        if (source.clip == null)
            return;

        int clampedSamples = Mathf.Clamp(timeSamples, 0, Mathf.Max(0, source.clip.samples - 1));
        source.timeSamples = clampedSamples;
        source.UnPause();

        if (!source.isPlaying)
        {
            source.Play();
            source.timeSamples = clampedSamples;
        }
    }

    private void OnValidate()
    {
        volume = Mathf.Max(0f, volume);
        endTolerance = Mathf.Max(0.01f, endTolerance);
    }
}
