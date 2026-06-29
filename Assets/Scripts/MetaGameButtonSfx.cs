using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MetaGameButtonSfx : MonoBehaviour
{
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
#if UNITY_EDITOR
        tapClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Game/Common/ui_tap.wav");
        buyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Game/Common/buy.wav");
#endif
    }
}
