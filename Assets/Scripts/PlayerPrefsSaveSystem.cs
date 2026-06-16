using UnityEngine;

public sealed class PlayerPrefsSaveSystem : ISaveSystem
{
    public bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    public void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    public void Save()
    {
        PlayerPrefs.Save();
    }
}
