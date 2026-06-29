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

    public long GetLong(string key, long defaultValue = 0L)
    {
        string value = PlayerPrefs.GetString(key, defaultValue.ToString());
        return long.TryParse(value, out long parsed) ? parsed : defaultValue;
    }

    public void SetLong(string key, long value)
    {
        PlayerPrefs.SetString(key, value.ToString());
    }

    public string GetString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    public void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value ?? string.Empty);
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
