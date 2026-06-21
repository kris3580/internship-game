using UnityEngine;

public static class AudioPreferences
{
    private const string MusicEnabledKey = "MusicEnabled";
    private const string SoundEnabledKey = "SoundEnabled";

    public static bool MusicEnabled
    {
        get => PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        set => SaveBool(MusicEnabledKey, value);
    }

    public static bool SoundEnabled
    {
        get => PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        set => SaveBool(SoundEnabledKey, value);
    }

    private static void SaveBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
