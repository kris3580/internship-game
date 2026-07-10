using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MetaGameButtonSfx : MonoBehaviour
{
    private const string TapClipResourcePath = "Audio/Game/Common/ui_tap";
    private const string BuyClipResourcePath = "Audio/Game/Common/buy";

    private readonly HashSet<Button> hookedButtons = new();
    private AudioSource source;
    private AudioClip tapClip;
    private AudioClip buyClip;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        LoadClips();
    }

    private void Update()
    {
        HookButtons();
    }

    public static void PlayBuy()
    {
        MetaGameButtonSfx player = FindFirstObjectByType<MetaGameButtonSfx>();
        player?.Play(player.buyClip, 0.9f);
    }

    private void HookButtons()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button == null || hookedButtons.Contains(button))
                continue;

            hookedButtons.Add(button);
            button.onClick.AddListener(() => Play(tapClip, 0.75f));
        }
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || source == null || !AudioPreferences.SoundEnabled)
            return;

        source.pitch = Random.Range(0.94f, 1.06f);
        source.PlayOneShot(clip, volume);
    }

    private void LoadClips()
    {
        tapClip = Resources.Load<AudioClip>(TapClipResourcePath);
        buyClip = Resources.Load<AudioClip>(BuyClipResourcePath);

        if (tapClip == null)
            Debug.LogError($"Missing UI tap clip at Resources/{TapClipResourcePath}.", this);

        if (buyClip == null)
            Debug.LogError($"Missing UI buy clip at Resources/{BuyClipResourcePath}.", this);
    }
}
