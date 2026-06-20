using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class JsonSaveSystem : ISaveSystem
{
    [System.Serializable]
    private sealed class SaveData
    {
        public List<Entry> entries = new();
    }

    [System.Serializable]
    private sealed class Entry
    {
        public string key;
        public int value;
    }

    private readonly Dictionary<string, int> values = new();
    private readonly string path;
    private bool loaded;

    public JsonSaveSystem()
    {
        path = Path.Combine(Application.persistentDataPath, "save-data.json");
    }

    public bool HasKey(string key)
    {
        EnsureLoaded();
        return values.ContainsKey(key);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        EnsureLoaded();
        return values.TryGetValue(key, out int value) ? value : defaultValue;
    }

    public void SetInt(string key, int value)
    {
        EnsureLoaded();
        values[key] = value;
    }

    public void DeleteKey(string key)
    {
        EnsureLoaded();
        values.Remove(key);
    }

    public void Save()
    {
        EnsureLoaded();

        SaveData data = new();

        foreach (KeyValuePair<string, int> pair in values)
            data.entries.Add(new Entry { key = pair.Key, value = pair.Value });

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }

    private void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;

        if (!File.Exists(path))
            return;

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

        if (data?.entries == null)
            return;

        foreach (Entry entry in data.entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.key))
                values[entry.key] = entry.value;
        }
    }
}
